#!/usr/bin/env bash
# =============================================================================
# Skindexer — CS2 First-Run Setup
# =============================================================================
# Run this once before your first permanent docker compose up.
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/Lebeg134/Skindexer/main/scripts/setup-cs2.sh | bash
#
# What this script does:
#   1. Checks if ByMykel has already run — skips fetch if so
#   2. If not: starts the stack, waits for ByMykel to complete, brings it down
#   3. Applies the CS2 rarity order seed (idempotent — safe to re-run)
#
# After this script finishes your DB is seeded and ready.
# Do your final: docker compose up -d
# =============================================================================

set -euo pipefail

# --- Config ------------------------------------------------------------------
DB_SERVICE="db"
DB_NAME="skindexer"
DB_USER="skindexer"
FETCHER_ID="cs2-bymykel"
SEED_URL="https://raw.githubusercontent.com/Lebeg134/Skindexer/main/sql/seeds/cs_rarity_order.sql"
POLL_INTERVAL=10
TIMEOUT=600

# --- Colors ------------------------------------------------------------------
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

info()  { echo -e "${GREEN}[setup-cs2]${NC} $*"; }
warn()  { echo -e "${YELLOW}[setup-cs2]${NC} $*"; }
error() { echo -e "${RED}[setup-cs2]${NC} $*" >&2; }

# --- Trap: catch unexpected exits --------------------------------------------
trap 'error "Script exited unexpectedly at line $LINENO. Check output above for clues."' ERR

# --- Preflight ---------------------------------------------------------------
info "Running preflight checks..."

if ! command -v docker &>/dev/null; then
    error "Docker not found. Please install Docker and try again."
    exit 1
fi

if ! command -v curl &>/dev/null; then
    error "curl not found. Please install curl and try again."
    exit 1
fi

if [ ! -f "docker-compose.yml" ]; then
    error "docker-compose.yml not found. Run this script from the directory containing it."
    exit 1
fi

info "Preflight OK."

# --- Helper: wait for DB -----------------------------------------------------
wait_for_db() {
    info "Waiting for DB to be ready..."
    local waited=0
    until docker compose exec -T "$DB_SERVICE" pg_isready -U "$DB_USER" -d "$DB_NAME" &>/dev/null; do
        sleep 2
        waited=$((waited + 2))
        if [ "$waited" -ge 30 ]; then
            error "DB did not become ready within 30 seconds."
            return 1
        fi
    done
    info "DB is ready."
}

# --- Helper: is DB reachable -------------------------------------------------
db_is_reachable() {
    docker compose exec -T "$DB_SERVICE" pg_isready -U "$DB_USER" -d "$DB_NAME" &>/dev/null
}

# --- Check if ByMykel already ran --------------------------------------------
STACK_WAS_RUNNING=false
NEED_FETCH=true
STARTED_STACK=false

info "Checking if ByMykel has already run..."

if docker compose ps --status running 2>/dev/null | grep -q "$DB_SERVICE"; then
    STACK_WAS_RUNNING=true
    wait_for_db

    EXISTING=$(docker compose exec -T "$DB_SERVICE" psql -U "$DB_USER" -d "$DB_NAME" -t -c \
        "SELECT COUNT(*) FROM fetch_runs WHERE fetcher_id = '$FETCHER_ID' AND status = 'success';" \
        2>/dev/null | tr -d '[:space:]')

    if [ "${EXISTING:-0}" -gt 0 ]; then
        info "ByMykel has already run successfully — skipping fetch."
        NEED_FETCH=false
    else
        info "No successful ByMykel run found — fetch needed."
    fi
else
    info "Stack is not running — fetch needed."
fi

# --- Run ByMykel if needed ---------------------------------------------------
if [ "$NEED_FETCH" = true ]; then
    info "Starting stack with ByMykel fetch enabled..."

    Fetchers__Enabled="$FETCHER_ID" \
    Fetchers__FetchOnStartup="true" \
    docker compose up -d

    STARTED_STACK=true
    wait_for_db || { error "DB failed to become ready. Bringing stack down."; docker compose down; exit 1; }

    info "Waiting for ByMykel fetch to complete (timeout: ${TIMEOUT}s)..."

    ELAPSED=0
    while true; do
        STATUS=$(docker compose exec -T "$DB_SERVICE" psql -U "$DB_USER" -d "$DB_NAME" -t -c \
            "SELECT status FROM fetch_runs WHERE fetcher_id = '$FETCHER_ID' ORDER BY started_at DESC LIMIT 1;" \
            2>/dev/null | tr -d '[:space:]')

        if [ "$STATUS" = "success" ]; then
            info "ByMykel fetch completed successfully."
            break
        elif [ "$STATUS" = "failed" ]; then
            error "ByMykel fetch failed. Check logs with: docker compose logs api"
            docker compose down
            exit 1
        else
            if [ "$ELAPSED" -ge "$TIMEOUT" ]; then
                error "Timed out waiting for ByMykel fetch after ${TIMEOUT}s."
                error "Check logs with: docker compose logs api"
                docker compose down
                exit 1
            fi
            warn "Status: '${STATUS:-pending}' — waiting ${POLL_INTERVAL}s... (${ELAPSED}s elapsed)"
            sleep "$POLL_INTERVAL"
            ELAPSED=$((ELAPSED + POLL_INTERVAL))
        fi
    done
fi

# --- Ensure DB is reachable for seed step ------------------------------------
if ! db_is_reachable; then
    info "Starting stack for seed step..."
    docker compose up -d
    STARTED_STACK=true
    wait_for_db || { error "DB failed to become ready for seed step."; docker compose down; exit 1; }
fi

# --- Apply rarity seed -------------------------------------------------------
info "Fetching CS2 rarity order seed..."
SEED_SQL=$(curl -fsSL "$SEED_URL") || {
    error "Failed to download seed file from GitHub."
    [ "$STARTED_STACK" = true ] && [ "$STACK_WAS_RUNNING" = false ] && docker compose down
    exit 1
}

info "Applying CS2 rarity order seed..."
echo "$SEED_SQL" | docker compose exec -T "$DB_SERVICE" psql -U "$DB_USER" -d "$DB_NAME"
info "Rarity seed applied."

# --- Verify ------------------------------------------------------------------
info "Verifying rarity seed..."
RARITY_COUNT=$(docker compose exec -T "$DB_SERVICE" psql -U "$DB_USER" -d "$DB_NAME" -t -c \
    "SELECT COUNT(*) FROM rarities r JOIN rarity_groups rg ON rg.id = r.rarity_group_id WHERE rg.game_id = 'cs2' AND r.\"order\" IS NOT NULL;" \
    2>/dev/null | tr -d '[:space:]')

if [ -z "$RARITY_COUNT" ] || [ "$RARITY_COUNT" -eq 0 ]; then
    error "Rarity seed verification failed — no ordered rarity rows found. Check manually."
    exit 1
else
    info "Verified: $RARITY_COUNT CS2 rarity rows have ordering set."
fi

# --- Bring down if we started it ---------------------------------------------
if [ "$STARTED_STACK" = true ] && [ "$STACK_WAS_RUNNING" = false ]; then
    info "Bringing stack down..."
    docker compose down
fi

# --- Done --------------------------------------------------------------------
echo ""
echo -e "${GREEN}============================================${NC}"
echo -e "${GREEN}  CS2 setup complete!${NC}"
echo -e "${GREEN}============================================${NC}"
echo ""
echo "  Your DB is seeded and ready."
echo "  Configure your fetchers in docker-compose.yml and run:"
echo ""
echo "    docker compose up -d"
echo ""
