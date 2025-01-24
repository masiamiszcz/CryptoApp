# Etap 1: budowanie aplikacji
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Ustawiamy katalog roboczy na główny katalog projektu
WORKDIR /source

# Kopiowanie projektu Logger
COPY Logger/ Logger/

# Kopiowanie projektu CoinGeckoDockerService
COPY CoinGeckoDockerService/ CoinGeckoDockerService/

# Przywracanie zależności
WORKDIR /source/CoinGeckoDockerService
RUN dotnet restore

# Budowanie projektu
RUN dotnet publish -c Release -o /output

# Etap 2: obraz runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Kopiowanie opublikowanego buildu z kroków budowy
WORKDIR /app
COPY --from=build /output .

# Punkt wejścia aplikacji
ENTRYPOINT ["dotnet", "CoinGeckoDockerService.dll"]