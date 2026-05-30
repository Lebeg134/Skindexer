FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY skindexer.sln ./
COPY src/Skindexer.Api/Skindexer.Api.csproj src/Skindexer.Api/
COPY src/Skindexer.Contracts/Skindexer.Contracts.csproj src/Skindexer.Contracts/
COPY src/Skindexer.Fetchers/Skindexer.Fetchers.csproj src/Skindexer.Fetchers/
COPY src/Skindexer.Scheduler/Skindexer.Scheduler.csproj src/Skindexer.Scheduler/

RUN dotnet restore src/Skindexer.Api/Skindexer.Api.csproj

COPY src/ src/
RUN dotnet publish src/Skindexer.Api/Skindexer.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Skindexer.Api.dll"]
