namespace Skindexer.Api.Options;

public class FetcherOptions
{
    public const string SectionName = "Fetchers";

    private string _enabled = string.Empty;

    public string Enabled
    {
        get => _enabled;
        set => _enabled = value ?? string.Empty;
    }

    public IReadOnlyList<string> EnabledFetchers =>
        _enabled.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}