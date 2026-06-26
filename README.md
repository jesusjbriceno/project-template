# Project Template

Full-stack monorepo template: .NET 10 API + React TypeScript frontend, Docker-ready.

**Current phase**: Domain, Application, Infrastructure EF Core, MigrationService initial seed, and API auth endpoints (JWT login/refresh/logout) are complete. The next backend area is API hardening/foundation: ProblemDetails/Result mapping, OpenAPI metadata for auth, `.http` smoke docs, and auth rate limiting. Frontend is still a static placeholder — React/Vite app with TanStack Router arrives in a later phase.

## Quick Start

```bash
# Create your local env file (never commit .env)
cp .env.example .env

# Start the full stack
docker compose up --build

# Verify API health
curl http://localhost:8080/health
# → Healthy

# Frontend placeholder (static HTML until Phase 9)
# Default is port 3000; if WEB_PORT is set in .env, use that value instead.
curl http://localhost:3000
```

## Architecture

| Area | Decision |
|------|----------|
| Repository shape | Monorepo |
| Backend | .NET 10, ASP.NET Core |
| Backend architecture | Clean Architecture + CQRS + Result pattern |
| Persistence | EF Core + PostgreSQL, repositories |
| Auth | JWT HS256 access tokens, refresh token rotation, HttpOnly/Secure/SameSite cookies |
| Authorization | Roles + granular permissions (RBAC) |
| Frontend | React + TypeScript + Tailwind + pnpm + Zustand + Zod |
| Frontend router | TanStack Router (type-safe routes, typed search params, nested layouts) |
| Delivery | Docker Compose |

### Layer Rules

```
Domain → Application → Infrastructure → API / MigrationService
```

- **Domain**: Entities, value objects, domain rules. No EF Core, HTTP, or UI concerns.
- **Application**: Use cases, CQRS handlers, validators (FluentValidation), interfaces.
- **Infrastructure**: EF Core, repositories, migrations, external services.
- **API (Controllers/Endpoints)**: HTTP layer, auth middleware, request/response mapping.
- **MigrationService**: Applies EF Core migrations and seeds system roles, permissions, and the initial Superadmin at startup (runs and exits).

### Constraints

- **No AutoMapper**. Mapping via `explicit`/`implicit` operators.
- **Controllers preferred** by default. Minimal API endpoints live under `Endpoints/`, never in `Program.cs`.
- **Program.cs is composition root only**: configuration, DI, middleware, route registration.
- **TDD required**. Tests are first-class citizens.
- **OWASP Top Ten** compliance and static analysis quality gates are mandatory.

## Project Structure

```
├── apps/
│   ├── api/
│   │   ├── src/
│   │   │   ├── Project.Api.Controllers/    # ASP.NET Core Web API (controllers)
│   │   │   ├── Project.Api.Endpoints/      # Minimal API endpoint groups (optional)
│   │   │   ├── Project.Application/        # Use cases, CQRS, validators
│   │   │   ├── Project.Domain/             # Entities, value objects, domain rules
│   │   │   ├── Project.Infrastructure/     # EF Core, repositories, external services
│   │   │   └── Project.MigrationService/   # Schema migration & seed runner
│   │   └── tests/
│   │       ├── Project.UnitTests/
│   │       ├── Project.ApplicationTests/
│   │       └── Project.IntegrationTests/
│   └── web/                                # React frontend (Phase 9)
├── packages/contracts/                     # Shared API contracts (future)
├── docs/                                   # Implementation docs
├── infra/docker/                           # Dockerfiles
├── scripts/                                # Developer automation
├── docker-compose.yml                      # Full stack orchestration
└── README.md
```

## Development

```bash
# Build backend
dotnet build apps/api

# Run tests
dotnet test apps/api/tests/Project.IntegrationTests

# Run API locally (without Docker)
dotnet run --project apps/api/src/Project.Api.Controllers
```

## Environment Variables

Copy `.env.example` to `.env` before starting:

```bash
cp .env.example .env
```

`.env` is gitignored — never commit secrets. Compose uses `${VAR:-default}` fallbacks so the stack starts even without a local `.env`, but you should still create one for real development.

| Variable | Purpose |
|----------|---------|
| `POSTGRES_DB` | Database name (default: `projecttemplate`) |
| `POSTGRES_USER` | Database user (default: `postgres`) |
| `POSTGRES_PASSWORD` | Database password (default: `dev_password_change_me` — change for production) |
| `ConnectionStrings__DefaultConnection` | EF Core connection string |
| `SUPERADMIN_EMAIL` | Initial superadmin email — used by MigrationService seed |
| `SUPERADMIN_PASSWORD` | Initial superadmin password — used by MigrationService seed |
| `Jwt__Secret` | HS256 signing key (≥32 chars, generate with `openssl rand -base64 32`) |
| `Jwt__Issuer` | Token issuer (default: `project-template-api`) |
| `Jwt__Audience` | Token audience (default: `project-template-client`) |
| `Jwt__RefreshTokenDays` | Refresh token lifetime in days (default: `7`) |

> **Next backend area:** API hardening/foundation (ProblemDetails/Result mapping, OpenAPI metadata for auth, `.http` smoke docs, auth rate limiting), then Users/Roles/Permissions management APIs + authorization policies.
