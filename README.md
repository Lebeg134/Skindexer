# Skindexer
A self-hostable REST API for game skin prices and item catalogs.
Supports CS2, Rust, and any game you add via the fetcher plugin system.

## Features

- Price history for tradeable skins (CS2, Rust, more via community fetchers)
- Item catalog with game-specific metadata
- REST API with OpenAPI docs
- Designed for extensibility — add any game without touching core code

## Getting started

Coming soon!

## Adding a new game

See [docs/adding-a-fetcher.md](docs/adding-a-fetcher.md) for a full walkthrough.

## Roadmap

- [ ] First-class CS2 fetcher via Steam Market API
- [ ] Rust fetcher via Steam Market API
- [ ] Price statistics endpoint (24h change, 7d high/low)
- [ ] Multi-source price comparison
- [ ] Contributor-hosted catalogs (non-tradeable games)

## License

MIT
