namespace Skindexer.Api.Options;

public class FetcherOptions
{
    public const string SectionName = "Fetchers";

    public List<string> Enabled { get; set; } = [];
}