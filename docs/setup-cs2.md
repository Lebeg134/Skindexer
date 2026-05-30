# CS2 Setup Guide

This guide covers the required first-run steps for a CS2 deployment of Skindexer.
These steps are only needed once per fresh database.

## Automated setup (recommended)

A setup script handles everything below automatically. You don't need to clone the repo.

**Step 1** — Configure and start your stack:

```bash
# edit docker-compose.yml with your fetcher config, then:
docker compose up -d
```

**Step 2** — Run the CS2 setup script:

```bash
curl -fsSL https://raw.githubusercontent.com/Lebeg134/Skindexer/main/scripts/setup-cs2.sh | bash
```

The script will handle the ByMykel fetch, apply the rarity seed, and leave the stack ready.
If ByMykel has already run before, it skips the fetch and just applies the seed.

**Step 3** — Start your stack permanently with your real fetcher config:

```bash
docker compose up -d
```

That's it. If the script fails or you'd prefer to run the steps manually, follow the guide below.

---

## Why this is needed

CS2 item metadata (names, rarities, variants) is seeded by the **ByMykel fetcher**, which pulls
from the [bymykel/CSGO-API](https://github.com/bymykel/CSGO-API) dataset. Rarity display ordering
is a separate one-time seed that must run after the item data exists.

---

## Step 1 — Start the stack

```bash
docker compose up -d
```

Wait for the API to be healthy. Migrations are applied automatically on startup.

---

## Step 2 — Enable the ByMykel fetcher

In `docker-compose.yml`, uncomment and set the `Fetchers__Enabled` line:

```yaml
Fetchers__Enabled: "cs2-bymykel"
```

If you want additional price fetchers (e.g. Skinport, cs2.sh), add them here too.
They can wait — item metadata must exist before prices are meaningful.

---

## Step 3 — Run ByMykel on startup

ByMykel runs on a cron schedule and has no manual trigger endpoint. The easiest way to
run it immediately on first boot is to enable `FetchOnStartup`:

```yaml
Fetchers__FetchOnStartup: "true"
```

Then bring up (or restart) the stack:

```bash
docker compose up -d
```

Once ByMykel has run, **turn `FetchOnStartup` back off** to avoid triggering all fetchers
on every future restart:

```yaml
Fetchers__FetchOnStartup: "false"
```

Monitor progress in the `fetch_runs` table:

```bash
docker compose exec db psql -U skindexer -d skindexer -c \
  "SELECT fetcher_id, triggered_by, started_at, finished_at, status FROM fetch_runs ORDER BY started_at DESC LIMIT 5;"
```

The run is done when `finished_at` is populated and `status` is `success`.

---

## Step 4 — Run the rarity order seed

Once ByMykel has finished, apply the rarity display order:

```bash
docker compose exec -T db psql -U skindexer -d skindexer < sql/seeds/cs_rarity_order.sql
```

This script is idempotent — safe to re-run at any time.

### Verify

```bash
docker compose exec db psql -U skindexer -d skindexer -c \
  "SELECT rg.type, r.slug, r.\"order\" FROM rarities r JOIN rarity_groups rg ON rg.id = r.rarity_group_id WHERE rg.game_id = 'cs2' ORDER BY rg.type, r.\"order\";"
```

You should see ordered rows for `weapon_skin`, `agent`, `collectible`, and other types.

---

## Step 5 — Enable your remaining fetchers

Now that item data exists, uncomment and expand the `Fetchers__Enabled` line in `docker-compose.yml`:

```yaml
Fetchers__Enabled: "cs2-bymykel,cs2-skinport,cs2-cs2sh"
```

Also uncomment any required API keys for the fetchers you added. Restart the stack to pick up the change:

```bash
docker compose up -d
```

Price fetchers will run on their configured cron schedule, or immediately if `FetchOnStartup` is `true`.

---

## Summary

| Step | What | When |
|---|---|---|
| 1 | `docker compose up` | Once |
| 2 | Enable `cs2-bymykel` in config | Once |
| 3 | Set `FetchOnStartup=true`, restart, then turn it off | Once |
| 4 | Run `cs_rarity_order.sql` seed | Once |
| 5 | Enable price fetchers | Once |
