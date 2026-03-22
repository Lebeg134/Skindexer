using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Skindexer.Api.Data;
using Skindexer.Api.Data.Repositories;
using Skindexer.Api.Features.Collections;
using Skindexer.Api.Features.Grades;
using Skindexer.Api.Features.Items;
using Skindexer.Api.Features.Prices;
using Skindexer.Fetchers;
using Skindexer.Scheduler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSkindexerFetchers();
builder.Services.AddHostedService<FetchScheduler>();


builder.Services.AddDbContext<SkindexerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IPriceRepository, PriceRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<IGradeRepository, GradeRepository>();

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

var registry = app.Services.GetRequiredService<FetcherRegistry>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation(
    "Skindexer started. Scheduled: [{Scheduled}] | Manual: [{Manual}] | File: [{File}]",
    string.Join(", ", registry.Scheduled.Select(f => f.GameId)),
    string.Join(", ", registry.Manual.Select(f => f.GameId)),
    string.Join(", ", registry.FileBased.Select(f => f.GameId)));

app.Run();