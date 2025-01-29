FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Instalacja narzędzia sqlcmd
RUN apt-get update && \
    apt-get install -y curl software-properties-common apt-transport-https && \
    curl -sSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /usr/share/keyrings/mssql-release.gpg && \
    echo "deb [arch=amd64 signed-by=/usr/share/keyrings/mssql-release.gpg] https://packages.microsoft.com/ubuntu/20.04/prod focal main" > /etc/apt/sources.list.d/mssql-release.list && \
    apt-get update && \
    ACCEPT_EULA=Y apt-get install -y mssql-tools unixodbc-dev && \
    echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

ENV PATH=$PATH:/opt/mssql-tools/bin

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Kopiuj plik projektu z katalogu CryptoDbDockerService
COPY CryptoDbDockerService/CryptoDbDockerService.csproj .
RUN dotnet restore "CryptoDbDockerService.csproj"

# Kopiuj resztę plików z katalogu CryptoDbDockerService
COPY CryptoDbDockerService/ ./
WORKDIR "/src"
RUN dotnet build "CryptoDbDockerService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CryptoDbDockerService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_EnableDiagnostics=0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CryptoDbDockerService.dll"]
STOPSIGNAL SIGINT
