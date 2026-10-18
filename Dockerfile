# syntax=docker/dockerfile:1

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached while only source files change.
COPY global.json Directory.Build.props BankingApp.sln ./
COPY Banking.Domain/Banking.Domain.csproj Banking.Domain/
COPY Banking.Application/Banking.Application.csproj Banking.Application/
COPY Banking.Infrastructure/Banking.Infrastructure.csproj Banking.Infrastructure/
COPY Banking.API/Banking.API.csproj Banking.API/
RUN dotnet restore Banking.API/Banking.API.csproj

COPY . .
RUN dotnet publish Banking.API/Banking.API.csproj --configuration Release --no-restore --output /app/publish

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl only serves the HEALTHCHECK below. /data holds the SQLite file (docker-compose mounts a volume there)
# and belongs to the image's non-root "app" user so the API can create the database on first start.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data && chown app:app /data

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/data/banking.db"

COPY --from=build --chown=app:app /app/publish .

USER app
EXPOSE 8080
VOLUME ["/data"]

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Banking.API.dll"]
