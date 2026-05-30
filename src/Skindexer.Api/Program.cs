using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;
using Skindexer.Api.Data;
using Skindexer.Api.Data.Repositories;
using Skindexer.Api.Extensions;
using Skindexer.Api.Features;
using Skindexer.Api.Features.Collections;
using Skindexer.Api.Features.Enrichment;
using Skindexer.Api.Features.FetchRuns;
using Skindexer.Api.Features.Import;
using Skindexer.Api.Features.Items;
using Skindexer.Api.Features.Prices;
using Skindexer.Api.Features.PriceSources;
using Skindexer.Api.Features.Rarity;
using Skindexer.Api.Features.Variants;
using Skindexer.Fetchers;
using Skindexer.Fetchers.Interfaces;
using Skindexer.Scheduler;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddOpenApi();
services.AddSkindexerFetchers(configuration);
services.AddHostedService<FetchScheduler>();

services.Configure<SchedulerOptions>(configuration.GetSection(SchedulerOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("Default")!;

var dataSource = new NpgsqlDataSourceBuilder(connectionString)
    .EnableDynamicJson()
    .Build();

services.AddSingleton(dataSource);

services.AddDbContext<SkindexerDbContext>(options =>
    options.UseNpgsql(dataSource)
        .UseSnakeCaseNamingConvention());

services.AddScoped<IItemRepository, ItemRepository>();
services.AddScoped<IPriceRepository, PriceRepository>();
services.AddScoped<IPriceSourceRepository, PriceSourceRepository>();
services.AddScoped<ICollectionRepository, CollectionRepository>();
services.AddScoped<IRarityRepository, RarityRepository>();
services.AddScoped<IRarityGroupRepository, RarityGroupRepository>();
services.AddScoped<IVariantRepository, VariantRepository>();
services.AddScoped<IFetchRunRepository, FetchRunRepository>();

services.AddScoped<IFetchResultPersister, FetchResultPersister>();
services.AddScoped<IItemEnricher, CS2ItemEnricher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

EnrichEndpoints.MapEndpoint(app);
GetItemsEndpoint.MapEndpoint(app);
GetPricesEndpoint.MapEndpoint(app);
GetPriceSourcesEndpoint.MapEndpoint(app);
GetCollectionsEndpoint.MapEndpoint(app);
GetRaritiesEndpoint.MapEndpoint(app);
GetRarityGroupsEndpoint.MapEndpoint(app);
GetVariantsEndpoint.MapEndpoint(app);

CS2KaggleImportEndpoint.MapEndpoint(app);

// Migrate before anything else starts — failure is fatal.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SkindexerDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        startupLogger.LogCritical(ex, "Database migration failed. Shutting down.");
        throw;
    }
}

var registry = app.Services.GetRequiredService<FetcherRegistry>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation(
    "Skindexer started. Scheduled: [{Scheduled}] | Manual: [{Manual}] | File: [{File}]",
    string.Join(", ", registry.Scheduled.Select(f => f.FetcherId)),
    string.Join(", ", registry.Manual.Select(f => f.FetcherId)),
    string.Join(", ", registry.FileBased.Select(f => f.FetcherId)));

app.Run();