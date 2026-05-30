# Skindexer

A self-hostable REST API for game skin prices and item catalogs.
Supports CS2 out of the box, with a fetcher plugin system for adding any game.

## Features

- Price history for tradeable skins (CS2 and more via community fetchers)
- Item catalog with game-specific metadata (names, rarities, wear variants)
- Multiple price sources per game — lowest listing, buy order, last sold, and more
- REST API with OpenAPI docs
- Designed for extensibility — add any game without touching core code
- Docker-based deployment — runs anywhere with a single `docker compose up`

## Getting Started

### Prerequisites

- Docker and Docker Compose

### Run

1. Download the `docker-compose.yml` from this repo.

2. Open it and configure which fetchers you want active by uncommenting the relevant lines:

```yaml
environment:
  # Uncomment and list the fetchers you want active:
  # Fetchers__Enabled: "cs2-bymykel,cs2-steamanalyst,cs2-cs2sh"

  # API keys (only needed for fetchers you enabled above):
  # CS2Sh__ApiKey: YOUR_KEY_HERE
  # Pricempire__ApiKey: YOUR_KEY_HERE
  # SteamWebApi__ApiKey: YOUR_KEY_HERE
```

3. Start the stack:

```bash
docker compose up -d
```

The API will be available at `http://localhost:8080`. In development, OpenAPI docs are at `/scalar/v1`.

> **CS2 users:** after first boot, a few manual steps are required to populate item data and rarity ordering.
> See [docs/setup-cs2.md](docs/setup-cs2.md).

## Configuration

All configuration is done via environment variables in `docker-compose.yml`.

| Variable | Description |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string (pre-configured in the compose file) |
| `Fetchers__Enabled` | Comma-separated list of fetcher IDs to enable (e.g. `cs2-bymykel,cs2-skinport`) |
| `Fetchers__FetchOnStartup` | Set to `true` to trigger all enabled fetchers immediately on boot |
| `Fetchers__Schedules__<fetcher-id>` | Override the default cron schedule for a specific fetcher (e.g. `Fetchers__Schedules__cs2-bymykel`) |

API keys are only needed for fetchers that require them — the compose file lists them all as commented-out examples.

A full list of available fetcher IDs is in [docs/fetchers.md](docs/fetchers.md) *(coming soon)*.

## Adding a New Game

See [docs/adding-a-fetcher.md](docs/adding-a-fetcher.md) for a full walkthrough.
The guide includes an annotated reference implementation and a prompt template designed for AI-assisted development.

## Roadmap

- [ ] Price statistics endpoint (24h change, 7d high/low)
- [ ] Multi-source price comparison
- [ ] Health and observability endpoint
- [ ] CSFloat fetcher
- [ ] Contributor-hosted catalogs (non-tradeable games)

## License

MIT
