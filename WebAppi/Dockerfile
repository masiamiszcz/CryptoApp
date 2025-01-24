FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebAppi/WebAppi.csproj", "WebAppi/"]
RUN dotnet restore "WebAppi/WebAppi.csproj"
COPY . .
WORKDIR "/src/WebAppi"
RUN dotnet build "WebAppi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# Diagnostyka katalogów
RUN ls -la /src/WebAppi
RUN ls -la /app/build
# Wymuszona wartość BUILD_CONFIGURATION
RUN dotnet publish "WebAppi.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:TreatWarningsAsErrors=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAppi.dll"]