using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;
using Skindexer.Api.Data;
using Skindexer.Api.Data.Repositories;
using Skindexer.Api.Features;
using Skindexer.Api.Features.Collections;
using Skindexer.Api.Features.Grades;
using Skindexer.Api.Features.Import;
using Skindexer.Api.Features.Items;
using Skindexer.Api.Features.Prices;
using Skindexer.Fetchers;
using Skindexer.Fetchers.Interfaces;
using Skindexer.Scheduler;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddOpenApi();
services.AddSkindexerFetchers(configuration);
services.AddHostedService<FetchScheduler>();


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
services.AddScoped<ICollectionRepository, CollectionRepository>();
services.AddScoped<IGradeRepository, GradeRepository>();

services.AddScoped<IFetchResultPersister, FetchResultPersister>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

GetItemsEndpoint.MapEndpoint(app);
GetPricesEndpoint.MapEndpoint(app);
GetCollectionsEndpoint.MapEndpoint(app);
GetGradesEndpoint.MapEndpoint(app);

CS2KaggleImportEndpoints.MapEndpoint(app);

var registry = app.Services.GetRequiredService<FetcherRegistry>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation(
    "Skindexer started. Scheduled: [{Scheduled}] | Manual: [{Manual}] | File: [{File}]",
    string.Join(", ", registry.Scheduled.Select(f => f.FetcherId)),
    string.Join(", ", registry.Manual.Select(f => f.FetcherId)),
    string.Join(", ", registry.FileBased.Select(f => f.FetcherId)));

app.Run();