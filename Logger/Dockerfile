# Etap 1: Budowanie projektu Logger
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
EXPOSE 8500

# 1. Kopiowanie pliku Logger.csproj — w tym miejscu zaczynamy od `.csproj`
COPY ["Logger/Logger.csproj", "Logger/"]

# 2. Przywracanie zależności tylko dla Logger
RUN dotnet restore "Logger/Logger.csproj"

# 3. Kopiowanie pełnej zawartości projektu Logger
COPY Logger/ ./Logger/

# 4. Budowa projektu Logger
WORKDIR /src/Logger
RUN dotnet build "Logger.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Etap 2: Publikowanie projektu Logger
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Logger.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Etap 3: Stworzenie gotowego obrazu opartego na ASP.NET Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Kopiowanie opublikowanych plików z poprzedniego etapu
COPY --from=publish /app/publish .

# EntryPoint dla aplikacji Logger
ENTRYPOINT ["dotnet", "Logger.dll"]