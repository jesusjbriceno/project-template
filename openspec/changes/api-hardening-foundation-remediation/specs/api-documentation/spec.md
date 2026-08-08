# Delta for api-documentation

## Purpose

Provide runtime evidence for the three documentation scenarios that lacked covering tests
in the prior verification pass: enum serialization, XML schema description propagation, and
the `.http` smoke document workflow.

## MODIFIED Requirements

### Requirement: Enum Serialization

API responses SHALL serialize enum values as strings using `JsonStringEnumConverter`,
configured for both controllers (`AddJsonOptions`) and minimal APIs (`ConfigureHttpJsonOptions`).
A runtime integration test SHALL execute an endpoint that returns an enum-typed DTO and assert
the serialized JSON contains the string name and never the integer form.
(Previously: Configuration defined but no endpoint exercised enum serialization at runtime.)

#### Scenario: Enum serialized as string
- GIVEN a response DTO containing an enum property
- WHEN serialized to JSON
- THEN the value appears as its string name, not its integer

#### Scenario: Enum value emitted end-to-end
- GIVEN an integration test sends a request that triggers a JSON response with an enum property
- WHEN the response body is read as raw JSON
- THEN the enum field appears as its string name
- AND the raw JSON body does not contain the integer form of the value
- AND the field deserializes back to the typed enum value

### Requirement: XML Doc Generation

The API controllers `.csproj` MUST enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
so existing `<summary>` XML doc comments on DTOs flow into the OpenAPI schema automatically.
A runtime OpenAPI test SHALL parse `/openapi/v1.json`, locate a known DTO schema, and assert
its `description` contains the expected summary text.
(Previously: XML generation enabled but the OpenAPI test never asserted schema descriptions.)

#### Scenario: XML comments appear in OpenAPI schema
- GIVEN a `TokenResponse` record with `<summary>` XML doc
- WHEN the OpenAPI document is generated
- THEN the schema includes the summary description text

#### Scenario: XML description is asserted at runtime
- GIVEN the OpenAPI document is fetched from `/openapi/v1.json`
- WHEN the test reads the `TokenResponse` schema
- THEN the schema `description` is non-empty
- AND the description contains the expected `<summary>` substring

### Requirement: .http Smoke Documentation

The repository root MUST contain `api-smoke.http` using `@baseUrl`. It SHALL cover every auth
endpoint with at least one success and one error scenario per endpoint, and SHALL include
commented alternatives for Docker Compose and local `dotnet run` base URLs. A runtime test
SHALL read `api-smoke.http`, parse it as plain text, and assert the document references each
auth endpoint, declares `@baseUrl`, and contains Docker/local commented variants.
(Previously: File existed but no test exercised the document workflow.)

#### Scenario: Smoke file covers auth endpoints
- GIVEN `api-smoke.http` opened in a compatible editor
- WHEN sending requests
- THEN `POST {{baseUrl}}/auth/login` covers valid + invalid credentials
- AND `POST {{baseUrl}}/auth/refresh` covers success + missing cookie
- AND `POST {{baseUrl}}/auth/logout` covers success path
- AND commented `@baseUrl` examples show Docker Compose and local execution options

#### Scenario: Smoke document is asserted at runtime
- GIVEN the test reads `api-smoke.http` from the repository root
- WHEN the document is parsed
- THEN `POST {{baseUrl}}/auth/login`, `POST {{baseUrl}}/auth/refresh`, and
  `POST {{baseUrl}}/auth/logout` are each referenced
- AND `@baseUrl` is declared
- AND commented Docker Compose and `dotnet run` variants are present
