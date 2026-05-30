namespace Skindexer.Scheduler;

public class SchedulerOptions
{
    public const string SectionName = "Fetchers";

    /// <summary>
    /// Override cron schedule per fetcher. Key is FetcherId, value is cron expression.
    /// Example: Fetchers__Schedules__cs2-bymykel = "0 1 * * *"
    /// </summary>
    public Dictionary<string, string> Schedules { get; set; } = new();

    /// <summary>
    /// Run all scheduled fetchers once immediately on startup before handing off to cron.
    /// Useful for seeding a fresh database. Defaults to true.
    /// </summary>
    public bool FetchOnStartup { get; set; } = true;
}