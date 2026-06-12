# AGENTS.md — Project Instructions

Source of truth for AI agents and developers working on this repository.

## Source of truth

- **Obsidian docs**: `D:\_Obsidian\Desarrollo\raw\02-Proyectos\00-ProjectTemplate`
- **README**: this repo's `README.md` for quick-start and current build/run status
- When in doubt, read `04-Architecture/` docs first, then `03-Discovery/`

## Architecture summary

| Area | Decision |
|------|----------|
| Repo shape | Monorepo — `apps/api/`, `apps/web/`, `packages/` |
| Backend | .NET 10, ASP.NET Core |
| Backend architecture | Clean Architecture + CQRS + Result pattern |
| Persistence | EF Core + PostgreSQL — EF Core **only** in Infrastructure layer |
| Auth | JWT access tokens + refresh token rotation |
| Authorization | Roles + granular permissions (RBAC) |
| Frontend | React + TypeScript + Tailwind + pnpm |
| Frontend state | Zustand |
| Frontend validation | Zod |
| Frontend router | TanStack Router (type-safe routes, typed search params, nested layouts) |
| Delivery | Docker Compose |
| Quality | TDD, SOLID, small atomic methods, OWASP Top Ten |

## Layer rules (backend)

```
Domain → Application → Infrastructure → API / MigrationService
```

| Layer | Responsibility | Constraints |
|-------|---------------|-------------|
| **Domain** | Entities, value objects, domain rules | No EF Core, HTTP, or UI concerns |
| **Application** | Use cases, CQRS handlers, validators (FluentValidation), interfaces | No EF Core references; depends only on Domain |
| **Infrastructure** | EF Core DbContext, repositories, migrations, external services | Implements Application interfaces; owns data access |
| **API** | Controllers/Endpoints, auth middleware, request/response mapping | Thin HTTP layer; delegates to Application |
| **MigrationService** | Schema migrations and seed execution | Runs and exits; separate from API |

## Backend constraints

- **No AutoMapper**. Mapping only via `explicit`/`implicit` operators.
- **Result pattern** for operation outcomes — no throwing exceptions for control flow.
- **FluentValidation** for all input validation (commands, queries, requests).
- **EF Core only in Infrastructure** — Domain and Application layers never reference EF Core.
- **Controllers preferred** by default. Minimal API endpoints go under `Endpoints/`, never in `Program.cs`.
- **Program.cs is composition root only**: configuration, DI, middleware, route registration.
- Methods stay small, atomic, and named by responsibility (SOLID).
- Async all the way — never `.Result` or `.Wait()`.
- Use `DateTimeOffset`, never `DateTime`.

## Frontend constraints

- **Clean Architecture folders**: `app/`, `layouts/`, `pages/`, `features/`, `entities/`, `shared/`
- **Tailwind tokens only** — no ad-hoc inline styles. Use CSS variables through Tailwind theme.
- **Mobile-first** responsive classes.
- **Light/dark/system** color modes via Tailwind theme tokens.
- **Accessibility: WCAG AA** — visible focus, semantic HTML, labels, keyboard navigation, contrast.
- **Zod** for runtime validation of external/untrusted data and form schemas.
- **Zustand** with typed stores and selectors for cross-component state.
- **TanStack Router** for type-safe routes, typed search params, nested routes/layouts, and loader coordination.
- Grid for page/layout structure, flex for internal components.
- Use the full viewport workspace — do not center tiny content in huge empty areas.

## Security and quality

- **OWASP Top Ten** compliance is mandatory.
- **Secrets must never be committed**. `.env` is gitignored; use `.env.example` as a template.
- **Dependency scans** (npm audit, `dotnet list package --vulnerable`) before releases.
- **Static analysis**: SonarAnalyzer.CSharp or equivalent must pass with zero warnings.
- **Error messages must not leak internals** — user-facing errors are generic; details are logged server-side.
- JWT tokens: short-lived access (15 min), refresh token in HttpOnly Secure SameSite cookie.

## Testing

- **TDD is mandatory**: write a failing test first, then the minimal code to make it pass.
- **No bugfix without a failing test** that reproduces the bug.
- Backend test projects:
  - `Project.UnitTests` — domain logic, validation, mapping
  - `Project.ApplicationTests` — use case handlers, CQRS
  - `Project.IntegrationTests` — API endpoints with real database (Testcontainers)
- Frontend test strategy: **future phase**. When implemented, follow the same TDD discipline.
- Tests must be independent — no shared mutable state across tests.

## Docker / Infra

- `docker compose up --build` starts the full stack (db, migration, api, web).
- Migration service runs on container start and exits — it does not stay alive.
- API healthcheck: `curl http://localhost:8080/health`.
- PostgreSQL data persists in a named volume (`pgdata`).

## Work rules

- **No architecture improvisation**. Follow `04-Architecture/` docs. If unclear, consult `03-Discovery/` first.
- **Keep README.md updated** when adding/removing features or changing setup steps.
- **Never add AI attribution** (`Co-Authored-By`, AI mentions) to commits.
- Use **Conventional Commits** (`feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`).
- One work unit per commit — tests and docs travel with the behavior they verify.
- All generated artifacts (code, comments, identifiers, UI strings, docs) **must be in English** unless explicitly requested otherwise.
