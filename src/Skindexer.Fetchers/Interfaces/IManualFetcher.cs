namespace Skindexer.Fetchers.Interfaces;
 
/// <summary>
/// Implement this for sources that are triggered manually via the API.
/// The scheduler ignores these entirely.
/// TODO: The API exposes POST /admin/fetch/{gameId} for any registered IManualFetcher.
///
/// Use this when:
/// - There is no public API to query automatically
/// - Items have fixed developer-set prices rather than a player-driven market
/// - Data changes infrequently and human oversight makes sense
/// - You want to import catalog or price data on demand
///
/// Examples:
///   - Valorant (Riot provides no public API, fixed Valorant Points store)
///   - Apex Legends (EA provides no public API, fixed Apex Coins store)
///   - Arc Raiders (no trading system, Raider Tokens store only)
///   - Marvel Rivals (fixed in-game store, no player trading)
/// </summary>
public interface IManualFetcher : IGameFetcher
{
}