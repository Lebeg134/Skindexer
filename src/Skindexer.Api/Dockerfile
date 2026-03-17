FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
 
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
 
# Copy csproj files first for layer caching
COPY ["src/Skindexer.Api/Skindexer.Api.csproj", "src/Skindexer.Api/"]
COPY ["src/Skindexer.Fetchers/Skindexer.Fetchers.csproj", "src/Skindexer.Fetchers/"]
COPY ["src/Skindexer.Contracts/Skindexer.Contracts.csproj", "src/Skindexer.Contracts/"]
COPY ["src/Skindexer.Scheduler/Skindexer.Scheduler.csproj", "src/Skindexer.Scheduler/"]
 
RUN dotnet restore "src/Skindexer.Api/Skindexer.Api.csproj"
 
# Copy everything else and publish
COPY . .
WORKDIR "/src/src/Skindexer.Api"
RUN dotnet publish "Skindexer.Api.csproj" -c Release -o /app/publish
 
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Skindexer.Api.dll"]
 