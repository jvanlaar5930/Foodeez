# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Foodeez.sln ./
# Central package management decides every version and global.json pins the SDK band -
# restore below fails without them, since no PackageReference carries its own Version.
COPY Directory.Packages.props global.json ./
COPY src/Foodeez.Domain/Foodeez.Domain.csproj src/Foodeez.Domain/
COPY src/Foodeez.Application/Foodeez.Application.csproj src/Foodeez.Application/
COPY src/Foodeez.Infrastructure/Foodeez.Infrastructure.csproj src/Foodeez.Infrastructure/
COPY src/Foodeez.API/Foodeez.API.csproj src/Foodeez.API/
RUN dotnet restore src/Foodeez.API/Foodeez.API.csproj

COPY src/ src/
RUN dotnet publish src/Foodeez.API/Foodeez.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://0.0.0.0:5000
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "Foodeez.API.dll"]
