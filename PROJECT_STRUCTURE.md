# CardApplication Project Structure

Last reviewed: 2026-09-15

This document is the quick-reference guide for the CardApplication codebase. It describes the current source layout and the main runtime relationships. Generated files and machine-specific settings are intentionally excluded.

## Repository Layout

```text
CardApplication/
|-- .github/                         CI or repository automation
|-- CardApplicationAPI/
|   |-- CardApplicationAPI.sln       Visual Studio solution
|   |-- CardApplicationAPI.csproj    ASP.NET Core project
|   |-- Program.cs                   Application startup and dependency injection
|   |-- WeatherForecast.cs           Template/sample response model
|   |-- Authentication/              Custom authentication handlers
|   |-- Controllers/                 HTTP API endpoints
|   |-- Data/
|   |   |-- DBContext/               Entity Framework database context
|   |   `-- Entities/                Database entity classes
|   |-- Helpers/                     Shared helpers such as password hashing
|   |-- Middleware/                 Request pipeline middleware
|   |-- Migrations/                  Entity Framework migration history
|   |-- Models/                      API/domain models and response wrappers
|   |-- Repositories/
|   |   |-- Interfaces/              Repository contracts
|   |   `-- Implementations/         Entity Framework data access
|   |-- Services/
|   |   |-- Interfaces/              Service contracts
|   |   `-- Implementations/         Business logic
|   |-- appsettings*.json            Runtime configuration; local files are ignored
|   |-- CardApplicationAPI.http      Example HTTP requests
|   `-- Properties/                  Launch profiles
|-- PROJECT_STRUCTURE.md              Codebase structure reference
`-- API_DOCUMENTATION.md             Frontend API consumption guide
```

The following folders are generated or local-only and are not part of the source architecture: `.vs/`, `bin/`, `obj/`, and user-specific settings files.

## Where To Start

Use these files as the first stop for common questions:

| Question | Start here |
|---|---|
| How does the application start? | `CardApplicationAPI/Program.cs` |
| Which endpoint handles a request? | `CardApplicationAPI/Controllers/` |
| Where is business logic? | `CardApplicationAPI/Services/Implementations/` |
| Where is database access? | `CardApplicationAPI/Repositories/Implementations/` |
| Which tables are available? | `CardApplicationAPI/Data/DBContext/AppDbContext.cs` and `Data/Entities/` |
| What does the frontend send or receive? | `API_DOCUMENTATION.md` and `CardApplicationAPI/Models/` |
| How has the schema changed? | `CardApplicationAPI/Migrations/` |
| What settings are required? | `CardApplicationAPI/appsettings.Development.json.example` |

For future code questions, identify the controller first, follow its injected service, then follow the repository and entity used by that service.

## Technology

- ASP.NET Core Web API using `Microsoft.NET.Sdk.Web`
- Target framework: `net10.0`
- Nullable reference types and implicit usings enabled
- SQL Server through Entity Framework Core
- JWT bearer authentication
- Swagger/OpenAPI through Swashbuckle
- Password hashing through ASP.NET Core Identity primitives

## Application Startup

`CardApplicationAPI/Program.cs` is the composition root. It configures:

- Controllers, endpoint discovery, and Swagger
- JWT bearer authentication and authorization
- A fallback authorization policy requiring authenticated users
- SQL Server `AppDbContext`
- CORS policy named `all`
- Repository and service dependency injection
- Password hashing as a singleton helper
- HTTPS redirection and the HTTP request pipeline

The default development launch profiles use HTTP port `5156` and HTTPS port `7154`. Swagger is the launch page.

### Request pipeline order

Requests pass through the following important stages in `Program.cs`:

1. Swagger middleware exposes the OpenAPI UI.
2. HTTPS redirection redirects HTTP requests when applicable.
3. The permissive `all` CORS policy allows any origin, header, and method.
4. Authentication validates the JWT bearer token.
5. Authorization evaluates the fallback policy and controller role attributes.
6. Global exception middleware converts unhandled exceptions into JSON responses.
7. Controller routing dispatches the request with `MapControllers()`.

The fallback authorization policy requires authentication by default. Endpoints that must be public explicitly use `[AllowAnonymous]`, as `AuthController` does.

## Request and Data Flow

The normal application flow is:

```text
HTTP request
    -> Controller
    -> Service interface and implementation
    -> Repository interface and implementation
    -> AppDbContext
    -> SQL Server
```

Controllers are responsible for routing, authorization attributes, request validation, and HTTP responses. Services coordinate business operations and response wrappers. Repositories perform persistence and map between API models and database entities.

### Dependency injection registrations

| Abstraction | Implementation | Lifetime |
|---|---|---|
| `IUserRepository` | `UserRepository` | Scoped |
| `ICardInfoRepository` | `CardInfoRepository` | Scoped |
| `IRefreshTokenRepository` | `RefreshTokenRepository` | Scoped |
| `IUserService` | `UserService` | Scoped |
| `ICardInfoService` | `CardInfoService` | Scoped |
| `IJwtTokenService` | `JwtTokenService` | Scoped |
| `IRefreshTokenService` | `RefreshTokenService` | Scoped |
| `PasswordHasherHelper` | Concrete helper | Singleton |
| `AppDbContext` | EF Core context | Scoped by `AddDbContext` |

## API Controllers

All controller routes use the `api/[controller]` convention unless noted otherwise.

| Controller | Route | Authorization | Responsibility |
|---|---|---|---|
| `AuthController` | `api/Auth` | Anonymous | Login, refresh token rotation, logout, and initial master-user seed |
| `UsersController` | `api/Users` | `Admin` or `Master` | User CRUD operations |
| `CardInfoController` | `api/CardInfo` | Authenticated users | Card information CRUD operations |
| `WeatherForecastController` | `/WeatherForecast` | Authenticated users | Template/sample endpoint |

Authentication endpoints include `POST login`, `POST refresh`, `POST logout`, and `POST seed`. User and card controllers expose the expected `GET`, `GET {id}`, `POST`, `PUT {id}`, and `DELETE {id}` operations.

The complete frontend request and response reference is maintained separately in [API_DOCUMENTATION.md](API_DOCUMENTATION.md).

## Source File Catalog

### Controllers

- `AuthController.cs`: login, access-token refresh, logout, and initial-user seeding.
- `UsersController.cs`: role-restricted user CRUD operations.
- `CardInfoController.cs`: authenticated card-information CRUD operations.
- `WeatherForecastController.cs`: default template endpoint; not business functionality.

### Authentication and helpers

- `Authentication/BasicAuthenticationHandler.cs`: custom Basic authentication handler that looks up users and verifies passwords.
- `Helpers/PasswordHasherHelper.cs`: hashes and verifies passwords using ASP.NET Identity's `PasswordHasher<Users>`.
- `Models/Settings/JwtSettings.cs`: strongly typed JWT configuration.

### Middleware

- `Middleware/GlobalExceptionMiddleware.cs`: catches unhandled exceptions and writes a JSON error response.
- `Middleware/GlobalExceptionMiddlewareExtensions.cs`: registers the exception middleware with `UseGlobalExceptionHandler()`.

### Models and response types

- `Models/Users.cs`: user API/domain model.
- `Models/CardInfo.cs`: card-information API/domain model.
- `Models/ResponseModel/ApplicationActionResult.cs`: response wrapper, response status enum, pagination types, and response factories.
- `Models/Settings/JwtSettings.cs`: JWT issuer, audience, key, and token lifetime settings.

The API models and database entities are separate types. Repositories perform the mapping between them. This separation allows database storage details to differ from the public API contract, although the current user API model still contains a password property.

## Data Layer

`Data/DBContext/AppDbContext.cs` defines the EF Core context and currently exposes:

- `Users` mapped from `UserEntity`
- `RefreshTokens` mapped from `RefreshTokenEntity`
- `CardInfo` mapped from `CardInfoEntity`
- `PaymentInfo` mapped from `PaymentInfoEntity`

Database entities live in `Data/Entities/`. API/domain models live in `Models/`. Repositories explicitly map between these representations.

The `Migrations/` folder contains the database history, including the initial schema, CardInfo changes, and refresh-token support. Migration designer files and the model snapshot are generated by EF Core but should remain committed because they are required for migration tracking.

### Current schema timeline

| Migration | Change |
|---|---|
| `20251113030509_initialcreate` | Creates `CardInfo`, `PaymentInfo`, and `Users` tables |
| `20251117132418_newcolumnincardinfo` | Adds `BankName` to `CardInfo` |
| `20251117132725_datavaluechange` | Empty migration; no schema operation |
| `20251117132956_datavaluechange1` | Changes `CardInfo.BankName` to required `nvarchar(50)` |
| `20260815124900_AddRefreshTokens` | Creates the `RefreshTokens` table |

`AppDbContextModelSnapshot.cs` represents the latest EF Core model used to compare future migrations.

## Services and Repositories

Current service areas:

- User management: `IUserService` and `UserService`
- Card information: `ICardInfoService` and `CardInfoService`
- JWT access tokens: `IJwtTokenService` and `JwtTokenService`
- Refresh tokens: `IRefreshTokenService` and `RefreshTokenService`

Current repository areas:

- Users
- Card information
- Refresh tokens

The service layer returns `InternalActionResult<T>` or token-specific results. Common response types are defined in `Models/ResponseModel/`.

### Business areas

The current business areas are:

1. **Authentication**: credentials, JWT access tokens, refresh-token rotation, and logout.
2. **Users**: user records, roles, password hashing, and user administration.
3. **Card information**: card record CRUD and model/entity mapping.
4. **Payment information**: one lifetime summary per party, derived from CardInfo, with paid/pending balances and an Admin/Master settlement action.
5. **Stock and stock payments**: stock CRUD plus automatically synchronized read-only payment summaries.
6. **Cash and bank ledgers**: cash movements, dynamic bank accounts, account transactions, and atomic transfers.
7. **Dashboard**: combined financial/inventory totals including pending party payments.

## Authentication and Security

- `AuthController` issues access and refresh tokens after validating credentials.
- `JwtTokenService` creates access tokens using the configured issuer, audience, signing key, and expiry.
- `RefreshTokenService` creates, rotates, revokes, and expires refresh tokens.
- `UsersController` restricts access to the `Admin` and `Master` roles.
- `CardInfoController` requires an authenticated user.
- `BasicAuthenticationHandler` contains a custom Basic authentication implementation used by authentication-related code when registered.
- `PasswordHasherHelper` supports ASP.NET Identity password hashes and currently retains a legacy plain-text fallback in authentication paths.

Never commit real keys, passwords, or connection strings. Local `appsettings.json` and `appsettings.Development.json` files are ignored; use the example settings file as the configuration template.

### Security-sensitive implementation notes

- JWT validation checks issuer, audience, lifetime, signing key, and uses zero clock skew.
- Access tokens include user ID, username, and role claims.
- Refresh tokens are rotated on refresh and can be revoked on logout.
- The current authentication paths retain a plain-text password fallback for legacy data. This should be removed after legacy records are migrated.
- The seed endpoint returns a default password and should be restricted or removed outside initial setup.
- CORS currently allows any origin, header, and method. Restrict this policy before production deployment.
- User responses currently use the `Users` model, which includes `Password`. A response-specific DTO should be introduced before exposing this API beyond a trusted environment.

## Configuration

Important configuration sections:

- `ConnectionStrings:DefaultConnection` for SQL Server access
- `Jwt:Key` for signing tokens; it must be at least 32 characters
- `Jwt:Issuer` and `Jwt:Audience` for token validation
- `Jwt:AccessExpiryMinutes` and `Jwt:RefreshExpiryDays` for token lifetimes

The example configuration is in `CardApplicationAPI/appsettings.Development.json.example`.

## Common Commands

Run from the repository root:

```powershell
dotnet restore CardApplicationAPI/CardApplicationAPI.sln
dotnet build CardApplicationAPI/CardApplicationAPI.sln
dotnet run --project CardApplicationAPI/CardApplicationAPI.csproj
```

When the database model changes, create and apply an EF Core migration using the repository's configured connection string, then commit the generated migration files and model snapshot.

## Development Workflow

1. Copy `CardApplicationAPI/appsettings.Development.json.example` to the local development settings file.
2. Add a valid SQL Server connection string and a JWT key of at least 32 characters.
3. Restore packages and build the solution.
4. Run the API and open Swagger at `/swagger`.
5. Seed the initial user only when the Users table is empty.
6. Use the returned login tokens to call protected endpoints.
7. When changing persistence models, create a migration and review the generated SQL/schema change before applying it.
8. Update both this document and `API_DOCUMENTATION.md` when public behavior changes.

There are currently no test projects visible in the repository tree. Until automated tests are added, validate endpoint changes through Swagger or the `.http` request file and verify the affected database migration.

## Maintenance Rules

Update this document when any of the following changes:

- A project, source folder, controller, endpoint, service, repository, entity, or migration is added or removed
- Authentication, authorization, dependency injection, or middleware behavior changes
- Configuration keys, ports, packages, or startup commands change
- The API response or database mapping conventions change

Update `API_DOCUMENTATION.md` at the same time when a public endpoint, request payload, response shape, authentication rule, or frontend integration behavior changes.

Keep the directory tree and summaries concise. Do not add secrets, generated build output, full migration contents, or every class member here. For a specific question, use this document to locate the owning area and then verify details in the current source files.

## Current Limitations and Follow-up Areas

- No automated test project is currently present.
- Party identity is currently based on normalized `PartyName` text rather than a dedicated Party table.
- PaymentInfo retains only the latest settlement audit on its lifetime summary; it is not a separate payment-history ledger.
- Several response and error behaviors are implemented directly in controllers and services rather than through dedicated DTOs and centralized validation.
- User and card update methods accept broad domain models, including server-managed fields.
- The API has no explicit version prefix such as `/api/v1`.
- `BasicAuthenticationHandler` exists but is not registered in the current `Program.cs` authentication configuration.