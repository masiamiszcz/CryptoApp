# Budowanie projektu
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Kopiowanie pliku `.csproj` — ścieżka względem kontekstu budowania
COPY CoinGeckoDockerService/*.csproj ./CoinGeckoDockerService/

# Przywrócenie zależności (restore)
RUN dotnet restore ./CoinGeckoDockerService/CoinGeckoDockerService.csproj

# Kopiowanie reszty plików projektu
COPY CoinGeckoDockerService/ ./CoinGeckoDockerService/

# Budowanie projektu
WORKDIR /app/CoinGeckoDockerService
RUN dotnet publish -c Release -o /out

# Przygotowanie środowiska produkcyjnego
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "CoinGeckoDockerService.dll"]