# api-documentation Specification

## Purpose
Configurable OpenAPI exposure and `.http` smoke documentation conventions for the API surface.
JWT bearer security scheme wiring is deferred in this slice; the built-in OpenAPI document endpoint is the required baseline.

## Requirements

### Requirement: Configurable OpenAPI Availability
The API MUST support OpenAPI document generation via `AddOpenApi()` using .NET 10 built-in support (no Swashbuckle). The `/openapi/v1.json` endpoint SHALL be available only in Development. Production MUST NOT expose OpenAPI unless explicitly configured.

#### Scenario: OpenAPI available in Development
- GIVEN `ASPNETCORE_ENVIRONMENT=Development`
- WHEN `GET /openapi/v1.json`
- THEN 200 with valid OpenAPI 3.x JSON document

#### Scenario: OpenAPI unavailable in Production
- GIVEN `ASPNETCORE_ENVIRONMENT=Production`
- WHEN `GET /openapi/v1.json`
- THEN 404

### Requirement: Enum Serialization
API responses SHALL serialize enum values as strings using `JsonStringEnumConverter`, configured for both controllers (`AddJsonOptions`) and minimal APIs (`ConfigureHttpJsonOptions`).

#### Scenario: Enum serialized as string
- GIVEN a response DTO containing an enum property
- WHEN serialized to JSON
- THEN the value appears as its string name, not its integer

### Requirement: XML Doc Generation
The API controllers `.csproj` MUST enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` so existing `<summary>` XML doc comments on DTOs flow into the OpenAPI schema automatically.

#### Scenario: XML comments appear in OpenAPI schema
- GIVEN a `TokenResponse` record with `<summary>` XML doc
- WHEN the OpenAPI document is generated
- THEN the schema includes the summary description text

### Requirement: .http Smoke Documentation
The repository root MUST contain `api-smoke.http` using `@baseUrl`. It SHALL cover every auth endpoint with at least one success and one error scenario per endpoint, and SHALL include commented alternatives for Docker Compose and local `dotnet run` base URLs.

#### Scenario: Smoke file covers auth endpoints
- GIVEN `api-smoke.http` opened in a compatible editor
- WHEN sending requests
- THEN `POST {{baseUrl}}/auth/login` covers valid + invalid credentials
- AND `POST {{baseUrl}}/auth/refresh` covers success + missing cookie
- AND `POST {{baseUrl}}/auth/logout` covers success path
- AND commented `@baseUrl` examples show Docker Compose and local execution options
