using Scalar.AspNetCore;
using Skindexer.Fetchers;
using Skindexer.Scheduler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSkindexerFetchers();
builder.Services.AddHostedService<FetchScheduler>();

// TODO: add DbContext registration once EF migrations are set up

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Feature endpoints are registered here as we build them
// e.g. app.MapPriceEndpoints();

// Log full registry overview at startup — one place to see everything registered
var registry = app.Services.GetRequiredService<FetcherRegistry>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation(
    "Skindexer started. Scheduled: [{Scheduled}] | Manual: [{Manual}] | File: [{File}]",
    string.Join(", ", registry.Scheduled.Select(f => f.GameId)),
    string.Join(", ", registry.Manual.Select(f => f.GameId)),
    string.Join(", ", registry.FileBased.Select(f => f.GameId)));

app.Run();