# ── Build stage ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only source project files (tests excluded — they are not needed for publish)
COPY apps/api/src/Project.Domain/*.csproj       src/Project.Domain/
COPY apps/api/src/Project.Application/*.csproj  src/Project.Application/
COPY apps/api/src/Project.Infrastructure/*.csproj src/Project.Infrastructure/
COPY apps/api/src/Project.Api.Controllers/*.csproj src/Project.Api.Controllers/

# Restore only the project being published (avoids test-project dependencies)
RUN dotnet restore src/Project.Api.Controllers/Project.Api.Controllers.csproj

# Copy remaining source and publish
COPY apps/api/ ./
RUN dotnet publish src/Project.Api.Controllers/Project.Api.Controllers.csproj \
    -c Release -o /app --no-restore

# ── Runtime stage ────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Project.Api.Controllers.dll"]
