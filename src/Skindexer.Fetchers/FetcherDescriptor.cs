using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Skindexer.Fetchers;

public class FetcherDescriptor
{
    public string FetcherId { get; init; } = default!;
    public Action<IServiceCollection, IConfiguration> Register { get; init; } = default!;
}