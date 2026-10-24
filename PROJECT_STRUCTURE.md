# CardApplication Project Structure

Last reviewed against source: 2026-10-23

This is the primary project reference for future development and assistant sessions. Read it first to locate the relevant module, then verify behavior in the current source before changing code. It describes implemented behavior, not proposed features or the migration state of any deployed database. Generated files and machine-specific settings are intentionally excluded.

## How To Use This Reference

1. Read the module map, business rules, and limitations below before planning a change.
2. Inspect the relevant controller, service, repository, models/entities, and their interfaces. Source code takes precedence if documentation differs.
3. Check `git status` and preserve unrelated working changes.
4. Follow the change-impact checklist below; verify only the affected areas, without applying migrations or sending mutating requests to a hosted environment automatically.
5. Update this document when architecture, behavior, configuration, or verification steps change. Update [API_DOCUMENTATION.md](API_DOCUMENTATION.md) for public contract changes.

This file is a maintained entry point, not an automatically loaded tool rule. Future sessions should be directed to `PROJECT_STRUCTURE.md`; no separate `project.md` is maintained.

## Repository Layout

```text
CardApplication/
|-- .github/                         CI or repository automation
|-- CardApplication.Tests.sln        Root solution containing API and automated tests
|-- CardApplicationAPI.Tests/        Sibling .NET 10 xUnit project (not inside API)
|   |-- Infrastructure/             Owned LocalDB databases, HTTP factory, fixtures
|   |-- Unit/                       Validation, authentication helpers, middleware
|   `-- Integration/                HTTP, SQL schema, financial flows, concurrency
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
|   |-- DatabaseScripts/             Standalone SQL scripts; review against migrations
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
|   |-- CardInfoSettlement.http      Manual card/party settlement workflow
|   |-- StockPaymentSettlement.http  Manual stock financial workflow
|   |-- FullApiTest.http             Local full-API request collection
|   `-- Properties/                  Launch profiles
|-- PROJECT_STRUCTURE.md              Codebase structure reference
`-- API_DOCUMENTATION.md             Frontend API consumption guide
```

The following folders are generated or local-only and are not part of the source architecture: `.vs/`, `bin/`, `obj/`, and user-specific settings files. The old nested `CardApplicationAPI/CardApplicationAPI.Tests/obj/` directory is leftover output and remains excluded by the API project. The active tests are in the sibling root-level `CardApplicationAPI.Tests/` directory and the root test solution; the existing API-only solution is retained. `FullApiTest.http` is present locally but untracked at this review.

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
| `IPaymentInfoRepository` | `PaymentInfoRepository` | Scoped |
| `IStockItemRepository` | `StockItemRepository` | Scoped |
| `IStockPaymentInfoRepository` | `StockPaymentInfoRepository` | Scoped |
| `ICashLedgerRepository` | `CashLedgerRepository` | Scoped |
| `IBankAccountRepository` | `BankAccountRepository` | Scoped |
| `IBankTransactionRepository` | `BankTransactionRepository` | Scoped |
| `IDashboardRepository` | `DashboardRepository` | Scoped |
| `IRefreshTokenRepository` | `RefreshTokenRepository` | Scoped |
| `IUserService` | `UserService` | Scoped |
| `ICardInfoService` | `CardInfoService` | Scoped |
| `IPaymentInfoService` | `PaymentInfoService` | Scoped |
| `IStockItemService` | `StockItemService` | Scoped |
| `IStockPaymentInfoService` | `StockPaymentInfoService` | Scoped |
| `ICashLedgerService` | `CashLedgerService` | Scoped |
| `IBankAccountService` | `BankAccountService` | Scoped |
| `IBankTransactionService` | `BankTransactionService` | Scoped |
| `IDashboardService` | `DashboardService` | Scoped |
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
| `PaymentInfoController` | `api/PaymentInfo` | Authenticated reads; Admin/Master mark-paid | Party summaries and full pending settlement |
| `StockItemController` | `api/StockItem` | Authenticated users | Stock CRUD; party/status/direction filters |
| `StockPaymentInfoController` | `api/StockPaymentInfo` | Authenticated reads; Admin/Master settle/reset | Obligations, history, party and overall summaries |
| `CashLedgerController` | `api/CashLedger` | Authenticated users | Cash CRUD, type filter, opening/in/out/current summaries |
| `BankAccountController` | `api/BankAccount` | Authenticated users | Account CRUD, total and per-account balances |
| `BankTransactionController` | `api/BankTransaction` | Authenticated users | Transaction CRUD, account filter, transfers |
| `DashboardController` | `api/Dashboard` | Authenticated users | `GET summary` aggregate view |
| `WeatherForecastController` | `/WeatherForecast` | Authenticated users | Template/sample endpoint |

Authentication endpoints include `POST login`, `POST refresh`, `POST logout`, and `POST seed`. User and card controllers expose the expected `GET`, `GET {id}`, `POST`, `PUT {id}`, and `DELETE {id}` operations.

The complete frontend request and response reference is maintained separately in [API_DOCUMENTATION.md](API_DOCUMENTATION.md).

## Source File Catalog

### Controllers

- `AuthController.cs`: login, access-token refresh, logout, and initial-user seeding.
- `UsersController.cs`: role-restricted user CRUD operations.
- `CardInfoController.cs`: authenticated card-information CRUD operations.
- `PaymentInfoController.cs`: filtered lifetime summaries and `PATCH {id}/mark-paid` (no request body).
- `StockItemController.cs`: CRUD and `by-party/{partyName}`, `by-status/{status}`, `by-inout/{inOut}` reads.
- `StockPaymentInfoController.cs`: list/get, `stock/{stockItemId}`, `{id}/history`, `summary/parties`, `summary/totals`, and `PATCH {id}/settle` / `{id}/reset`.
- `CashLedgerController.cs`: CRUD, `type/{transactionType}`, and `summary/opening|in|out|current`.
- `BankAccountController.cs`: CRUD and `summary/total` / `summary/accounts`.
- `BankTransactionController.cs`: CRUD, `account/{accountId}`, and `POST transfer`.
- `DashboardController.cs`: `GET summary`.
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
- `Models/PaymentInfo.cs`: lifetime party summary and latest settlement audit.
- `Models/StockItem.cs` and `Models/StockPaymentInfo.cs`: stock source and synchronized obligation.
- `Models/StockPaymentSettlementModels.cs`: settlement/reset inputs, history response, party summary, and overall totals.
- `Models/CashLedger.cs`, `Models/BankAccount.cs`, `Models/BankTransaction.cs`: cash/bank API models.
- `Models/BankTransferRequest.cs` and `Models/BankTransferResult.cs`: paired-transfer contract.
- `Models/DashboardSummary.cs`: aggregate dashboard response.
- `Models/ResponseModel/ApplicationActionResult.cs`: response wrapper, response status enum, pagination types, and response factories.
- `Models/Settings/JwtSettings.cs`: JWT issuer, audience, key, and token lifetime settings.

The API models and database entities are separate types. Repositories perform the mapping between them. This separation allows database storage details to differ from the public API contract, although the current user API model still contains a password property.

## Data Layer

`Data/DBContext/AppDbContext.cs` defines the EF Core context and currently exposes:

- `Users` mapped from `UserEntity`
- `RefreshTokens` mapped from `RefreshTokenEntity`
- `CardInfo` mapped from `CardInfoEntity`
- `PaymentInfo` mapped from `PaymentInfoEntity`
- `StockItems` mapped from `StockItemEntity`
- `StockPaymentInfo` mapped from `StockPaymentInfoEntity`
- `StockPaymentHistory` mapped from `StockPaymentHistoryEntity`
- `CashLedger` mapped from `CashLedgerEntity`
- `BankAccounts` mapped from `BankAccountEntity`
- `BankTransactions` mapped from `BankTransactionEntity`

Database entities live in `Data/Entities/`. API/domain models live in `Models/`. Repositories explicitly map between these representations.

Important relationships and constraints in `AppDbContext`:

- Unique `Users.Username`, `PaymentInfo.PartyName`, and `BankAccounts.AccountName`.
- Unique `StockPaymentInfo.StockItemId` enforces at most one obligation per stock row, despite collection navigation properties.
- StockItem deletion cascades to StockPaymentInfo, which cascades to StockPaymentHistory.
- BankTransaction's primary account relationship cascades on deletion; its related-account relationship uses `NoAction`.
- Check constraints enforce payment classifications/statuses, nonnegative outstanding/party balances, and positive bank/cash amounts with supported transaction types.
- Entity attributes also define table/column names and types. Some stock columns contain spaces or `/`; consult entities before writing SQL.

The `Migrations/` folder contains the database history, including the initial schema, CardInfo changes, and refresh-token support. Migration designer files and the model snapshot are generated by EF Core but should remain committed because they are required for migration tracking.

### Current schema timeline

| Migration | Change |
|---|---|
| `20251113030509_initialcreate` | Creates `CardInfo`, `PaymentInfo`, and `Users` tables |
| `20251117132418_newcolumnincardinfo` | Adds `BankName` to `CardInfo` |
| `20251117132725_datavaluechange` | Empty migration; no schema operation |
| `20251117132956_datavaluechange1` | Changes `CardInfo.BankName` to required `nvarchar(50)` |
| `20260815124900_AddRefreshTokens` | Creates the `RefreshTokens` table |
| `20260928120000_AddPartyPaymentSummaries` | Adds swipe metadata; drops/recreates PaymentInfo as party summaries; adds unique indexes |
| `20261010141315_AddPartyPaymentSettlement` | Adds card settlement flags, paid/pending summary fields and audit, corrected sum backfill, and constraints |
| `20261010160000_AddStockPaymentClassificationAndHistory` | Adds stock classification/outstanding/completion fields, legacy backfill, history table, constraints, and index |

`AppDbContextModelSnapshot.cs` represents the latest EF Core model used to compare future migrations.

`DatabaseScripts/` contains `AddPartyPaymentSummaries.sql` (ignored by Git) and `AddStockPaymentClassificationAndHistory.sql`. These are not a guaranteed full database bootstrap. Review script prerequisites, batch boundaries, backfill behavior, and migration-history bookkeeping before execution. Do not assume a script and an EF migration are interchangeable or safely repeatable without comparison. Verify the target database schema and migration history independently; file presence does not mean a migration has been applied.

### Fresh database installation

`CardApplicationAPI/DatabaseScripts/CreateCardApplicationDatabase.sql` creates the complete current schema from the EF model: ten application tables, primary/foreign keys, seven secondary indexes, check constraints, and `dbo.__EFMigrationsHistory`.

- Target SQL Server or Azure SQL, not MySQL/PostgreSQL/SQLite. Create/select the desired application database in SSMS first, then execute the entire file. There is no hard-coded database name or `CREATE DATABASE` operation.
- Use a database without existing CardApplication objects or EF migration history. The script refuses collisions (including repeat execution), rejects system databases and ambient transactions, and wraps creation plus baseline registration in one transaction. It is a fresh-install script, not an upgrade/repair script.
- Tables are explicitly created under `dbo`. Ensure the application's database principal resolves these tables rather than same-named objects in another default schema. The installer needs permission to create tables/indexes and alter the `dbo` schema.
- The script records all eight current migrations through `20261010160000_AddStockPaymentClassificationAndHistory` as a baseline, using EF runtime version `10.0.12`. It creates their resulting current schema directly, rather than replaying historical destructive backfills. Do not run the old incremental SQL scripts afterward, or roll back to pre-baseline migrations.
- No users, passwords, tokens, sample bank accounts, or business rows are inserted. Required application-managed fields (including timestamps/statuses) have no extra SQL defaults beyond the current EF model; direct SQL inserts must supply them.
- Point the API's locally configured connection string at the new database after installation. Bootstrap the initial administrator only in a controlled setup; review the existing seed endpoint's security limitations above.
- When the model changes, regenerate/review schema with `dotnet ef dbcontext script --project CardApplicationAPI/CardApplicationAPI.csproj`, update this bootstrap script and its baseline together, and use reviewed migrations for already-installed databases. Baseline rows must never be copied into an older or partially initialized database.

## Services and Repositories

Current service areas:

- User management: `IUserService` and `UserService`
- Card information: `ICardInfoService` and `CardInfoService`
- Party payments: `IPaymentInfoService` and `PaymentInfoService`
- Stock source: `IStockItemService` and `StockItemService`
- Stock obligations/history/summaries: `IStockPaymentInfoService` and `StockPaymentInfoService`
- Cash ledger: `ICashLedgerService` and `CashLedgerService`
- Bank accounts: `IBankAccountService` and `BankAccountService`
- Bank transactions/transfers: `IBankTransactionService` and `BankTransactionService`
- Dashboard: `IDashboardService` and `DashboardService`
- JWT access tokens: `IJwtTokenService` and `JwtTokenService`
- Refresh tokens: `IRefreshTokenService` and `RefreshTokenService`

Current repository areas:

- Users
- Card information
- Party payment summaries and settlement
- Stock items and obligation synchronization
- Stock payment settlement, reset, history, and aggregate summaries
- Cash ledger
- Bank accounts and balances
- Bank transactions and transfers
- Dashboard aggregates
- Refresh tokens

The service layer returns `InternalActionResult<T>` or token-specific results. Common response types are defined in `Models/ResponseModel/`.

### Business areas

The current business areas are:

1. **Authentication**: credentials, JWT access tokens, refresh-token rotation, and logout.
2. **Users**: user records, roles, password hashing, and user administration.
3. **Card information**: card record CRUD and model/entity mapping.
4. **Payment information**: one lifetime summary per party, derived from CardInfo, with paid/pending balances and an Admin/Master settlement action.
5. **Stock and stock payments**: stock CRUD, synchronized Payable/Receivable obligations, per-item settlement/reset, history events, and party-wise summaries. History has no direct mutation API, but cascade deletion remains possible.
6. **Cash and bank ledgers**: cash movements, dynamic bank accounts, account transactions, and atomic transfers.
7. **Dashboard**: combined financial/inventory totals including pending party payments.

### Card and party-payment rules

- One CardInfo row represents one swipe. `CardInfoRepository.SyncPaymentInfoForPartyAsync` synchronizes the party summary on create/update/delete, including old and new parties when reassigned.
- `TotalAmount` sums each swipe's `Deduction`; `TotalProfit` sums each swipe's `Profit`. Do not restore the historical latest-value-times-count calculation.
- `PendingAmount` sums unsettled swipe deductions; `PaidAmount = TotalAmount - PendingAmount`. Latest-row fields such as `SwipeAmount` are metadata, not aggregate formulas.
- `PaymentInfoRepository.MarkPaidAsync` uses a serializable transaction, settles all pending swipes for the selected summary, and records server UTC time plus authenticated administrator identity.
- Settled swipes cannot be updated or deleted. A new swipe can reopen the same lifetime summary as Pending while preserving prior paid value.
- Party identity is text-based. Preserve trimming and inspect database collation/case behavior before changing grouping.

### Stock financial rules

- `StockItemService.NormalizeAndValidate` requires a stock number, positive amount, and normalized `IN` or `OUT`. `IN` requires supplier `PartyName`; `OUT` requires buyer `SoldTo`.
- Status text is normalized in casing, but currently has no allowed-value whitelist.
- `StockItemRepository.SyncStockPaymentInfoForStockItemAsync` creates/updates one obligation per stock row: IN becomes Payable to PartyName, OUT becomes Receivable from SoldTo.
- A pending obligation's outstanding amount follows the stock amount. Completed obligations retain zero outstanding.
- `StockPaymentInfoRepository.SettleAsync` requires the exact outstanding amount. Settle/reset operations use serializable transactions and record authenticated identity in history; partial settlement is not implemented.
- Stock source updates/deletes lock the obligation before reading protected fields. Settlement/reset take update locks on that same obligation, preventing a financial edit from passing a stale Pending check while settlement completes.
- Reset requires a reason, adds a Reset event, clears completion fields, and restores Pending/outstanding from the source amount. History reads are newest first.
- Completed payment blocks stock amount, direction, supplier, buyer changes and source deletion until reset. Descriptive edits are allowed; PUT replaces supplied model fields, so preserve unchanged values.
- Party summaries group by responsible party plus payment type. Overall totals sum outstanding amounts by type; completed rows contribute zero. Rows with null responsible party are excluded from party grouping.
- One stock row remains one obligation, not a linked receive/sell movement ledger. Do not reinterpret this as a complete inventory lifecycle.
- Settlement changes obligation state only: neither card nor stock settlement automatically posts bank transactions or cash-ledger entries.

### Cash, bank, and dashboard rules

- Cash types: `OPENING`, `IN`, `OUT`; current cash = opening + in - out.
- Bank types: `OPENING`, `IN`, `OUT`, `TRANSFER_IN`, `TRANSFER_OUT`; balance = opening + in + transfer-in - out - transfer-out.
- `BankTransactionRepository.TransferAsync` owns paired source/destination transfers. Use the transfer operation rather than constructing unrelated single rows.
- Transfers acquire account update locks in ascending account-ID order before reading balances, avoiding competing-transfer shared-lock conversion deadlocks.
- `DashboardRepository.GetSummaryAsync` computes values from database rows rather than maintaining a separate dashboard table:

| Response field | Source/formula |
|---|---|
| `totalBankBalance` | Aggregate bank balance formula above |
| `totalAvailableCash` | Aggregate cash formula above |
| `totalSwipes` | CardInfo row count |
| `totalPartyProfit` | Sum of CardInfo.Profit |
| `totalPendingPartyPayment` | Sum of PaymentInfo.PendingAmount |
| `totalStockItems` | Legacy count of IN rows, not available inventory |
| `availableStockCount` | Stock rows with case-insensitive Available status |
| `availableStockValue` | Sum of Amount for those Available rows |
| `pendingStockPayable` | Sum of OutstandingAmount for Payable obligations |
| `pendingStockReceivable` | Sum of OutstandingAmount for Receivable obligations |

### Response and pagination conventions

- Most operations return `InternalActionResult<T>` / `FactoryResponse<T>` with `statusCode`, `statusText`, `statusMessage`, `resultObject`, and `stopProccess`.
- Lists use `resultObject.items` and `resultObject.pagination`; single creates generally expose the ID at `resultObject.id`.
- `PaginationParams` defaults to page 1, size 10. The shared query extension accepts sizes 1–100 and requires a positive page number.
- Auth login/refresh return top-level token fields, not the normal wrapper. Refresh rotates the token; logout must use the current refresh token. Logout does not itself invalidate an already-issued JWT.
- Bank/cash actions now propagate service result status to HTTP responses. Missing records return 404, validation failures return 400, and duplicate bank-account names return 409. Successful creates use HTTP 201 with an internal success code of 200. Framework model-validation and authentication failures can use different response shapes.

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
- Rotation conditionally revokes a still-valid token and inserts its replacement in one database transaction; concurrent use of the same old token cannot issue two replacements.
- The current authentication paths retain a plain-text password fallback for legacy data. This should be removed after legacy records are migrated.
- The seed endpoint returns a default password and should be restricted or removed outside initial setup.
- CORS currently allows any origin, header, and method. Restrict this policy before production deployment.
- `Users.Password` is accepted on input but excluded from JSON output with `JsonIgnore(WhenWriting)`. User-list service results are materialized before clearing passwords so repeated enumeration remains redacted. Malformed stored password hashes are treated as failed verification, not uncaught format exceptions; legacy plaintext fallback remains unchanged.

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

Prerequisites: .NET 10 SDK, SQL Server access for runtime operations, and a compatible `dotnet-ef` tool for migration work. Configuration must be available to startup when EF tooling constructs the application. There is no automatic database migration call in `Program.cs`.

For an approved schema change, create a migration and generate SQL for review:

```powershell
dotnet ef migrations add <MigrationName> --project CardApplicationAPI/CardApplicationAPI.csproj
dotnet ef migrations script <FromMigration> <ToMigration> --idempotent --project CardApplicationAPI/CardApplicationAPI.csproj
```

Replace placeholders with the intended migration range. Compare entity attributes, `AppDbContext`, the snapshot, generated migration operations, and the actual target schema. Review destructive operations and backfills before applying anything. Only after explicit target confirmation and approval:

```powershell
dotnet ef database update --project CardApplicationAPI/CardApplicationAPI.csproj
```

Do not run database updates automatically as a build or documentation verification step.

## Development Workflow

1. Copy `CardApplicationAPI/appsettings.Development.json.example` to the local development settings file.
2. Add a valid SQL Server connection string and a JWT key of at least 32 characters.
3. Restore packages and build the solution.
4. Run the API and open Swagger at `/swagger`.
5. Seed the initial user only when the Users table is empty.
6. Use the returned login tokens to call protected endpoints.
7. When changing persistence models, create a migration and review the generated SQL/schema change; apply it only to an explicitly approved target.
8. Update both this document and `API_DOCUMENTATION.md` when public behavior changes.

### Automated verification

`CardApplication.Tests.sln` contains the API and the root-level `CardApplicationAPI.Tests` xUnit project. The earlier decision to defer automated tests has been superseded.

Prerequisites: .NET 10 SDK and SQL Server LocalDB on Windows. Start the automatic instance with `SqlLocalDB start MSSQLLocalDB` if necessary. The integration runner requires LocalDB to work; it does not silently skip tests or substitute SQLite/InMemory.

```powershell
dotnet restore CardApplication.Tests.sln --disable-parallel
dotnet build CardApplication.Tests.sln --no-restore
dotnet test CardApplication.Tests.sln --no-build --filter "Category=Unit"
dotnet test CardApplication.Tests.sln --no-build --filter "Category=Integration"
dotnet test CardApplication.Tests.sln --no-build --logger trx --results-directory CardApplicationAPI.Tests/bin/TestResults --collect:"XPlat Code Coverage"
```

- Each integration workflow creates a fresh `CardApplication_Test_<GUID>` database on `(localdb)\MSSQLLocalDB` and executes the current complete bootstrap SQL. Tests do not reuse application settings, hosted APIs, or application databases.
- Cleanup drops only the exact database created by that fixture, after validating its generated name and disposing hosts/connections. It clears only that database's connection pool, never all application pools. Failed cleanup is reported rather than scanning/deleting databases from other runs.
- Two unrelated test classes run in parallel to avoid LocalDB connection pressure. Concurrency tests still send simultaneous requests within the same fixture against separate request-scoped DbContexts.
- The HTTP factory overrides test JWT configuration and the database, retains real authentication/authorization, and checks the resolved database name before requests. There are no production test endpoints or auth bypasses.
- Coverage includes all 58 current controller method/route pairs, service validation, password serialization/redaction, card/stock settlement cycles, audit claims, bank/cash arithmetic, dashboard totals, bootstrap guards/schema comparison, and targeted concurrency regressions.
- Use `--filter "FullyQualifiedName~TestName"` for focused iterations. After a production fix, run affected tests and the full suite again on fresh databases. TRX and coverage output live under ignored `bin/`.
- This is Windows/LocalDB verification, not load testing, a hosted deployment test, or proof that historical migrations can bootstrap a database from zero. The standalone script is the tested initialization path.
- NuGet currently reports moderate/high advisories for the existing transitive `System.Security.Cryptography.Pkcs` 9.0.11 dependency. Do not suppress these warnings or treat passing functional tests as security clearance.

Verification on 2026-10-23: 28 unit cases and 32 integration cases passed in two consecutive complete runs (60 passed, zero failed/skipped in each). Build succeeded, `git diff --check` passed, and a read-only LocalDB check found zero remaining test databases. TRX files are `CardApplicationAPI.Tests/bin/TestResults/final-1.trx` and `final-2.trx`; Cobertura coverage is generated under the same output directory. Overall application coverage was 63.66% of lines and 79.86% of branches, including historical migration/legacy code; this is not exhaustive behavioral or security coverage.

### Manual verification resources

| File | Intended use |
|---|---|
| `CardApplicationAPI/CardApplicationAPI.http` | General endpoint examples |
| `CardApplicationAPI/CardInfoSettlement.http` | Card aggregation, mark-paid, settled-row protection, new settlement cycle |
| `CardApplicationAPI/StockPaymentSettlement.http` | Classification, summaries, settlement/reset/history, financial edit protection |
| `CardApplicationAPI/FullApiTest.http` | Locally present full-API request collection; review before execution |

These files are manual examples, not proof that a workflow passed. Check the host first: existing examples can target a hosted service. Use an isolated test database, fresh test names, valid response-capture variables, and test credentials supplied locally. Existing locally edited samples may contain pasted IDs or values inconsistent with their variables. Run dependent requests in order and inspect both HTTP status and response payload. Never execute mutating requests or cleanup against a real environment without approval.

### Change-impact checklist

| Change | Inspect/update together |
|---|---|
| Endpoint or validation | Controller, service interface/implementation, API model, API documentation, affected `.http` example |
| Persistence field or relationship | Entity, AppDbContext, repository mappings, public model if exposed, migration and snapshot |
| Card amounts or party identity | CardInfoRepository synchronization, PaymentInfoRepository settlement, services, DashboardRepository |
| Stock financial fields | StockItemService validation, StockItemRepository synchronization/protections, StockPaymentInfoRepository history/settlement/summaries, dashboard |
| Bank/cash movement | Transaction/account/ledger repositories and services, transfer models, balance summaries, dashboard |
| Authentication or authorization | AuthController, JwtTokenService, RefreshTokenService/repositories, user mapping, Program.cs, role attributes |
| New module | Controller, models/entities, service/repository contracts and implementations, Program.cs registrations, AppDbContext, documentation |

After implementation:

1. Restore/build the solution when code or dependencies changed.
2. Verify the affected behavior and intentional failure paths in an approved test environment; report anything not executed.
3. For documentation-only changes, validate paths, claims, and Markdown links against source; an application build is not required.
4. Run `git diff --check` and review the scoped diff. Preserve unrelated user changes.
5. Update this reference and public API documentation as appropriate; never include credentials or deployment-specific secrets.

## Maintenance Rules

Update this document when any of the following changes:

- A project, source folder, controller, endpoint, service, repository, entity, or migration is added or removed
- Authentication, authorization, dependency injection, or middleware behavior changes
- Configuration keys, ports, packages, or startup commands change
- The API response or database mapping conventions change

Update `API_DOCUMENTATION.md` at the same time when a public endpoint, request payload, response shape, authentication rule, or frontend integration behavior changes.

Keep the directory tree and summaries concise. Do not add secrets, generated build output, full migration contents, or every class member here. For a specific question, use this document to locate the owning area and then verify details in the current source files.

## Current Limitations and Follow-up Areas

- Automated integration tests require Windows LocalDB; Linux/container CI is not configured.
- Stock status has no strict enum validation; legacy rows may contain inconsistent direction/status/party data.
- Stock history is not guaranteed permanent: reset followed by source deletion can cascade-delete past events. There are no direct history edit/delete endpoints.
- Stock source and settlement operations coordinate obligation locks, and bank transfers lock accounts in stable order. Targeted concurrent scenarios are tested, but not every possible cross-operation interleaving or production load pattern.
- Migration files and standalone SQL should be audited against the target schema before bootstrap or upgrade; the model snapshot alone is not evidence that all tables were created or deployed.
- Party identity is currently based on normalized `PartyName` text rather than a dedicated Party table.
- PaymentInfo retains only the latest settlement audit on its lifetime summary; it is not a separate payment-history ledger.
- Several response and error behaviors are implemented directly in controllers and services rather than through dedicated DTOs and centralized validation.
- User and card update methods accept broad domain models, including server-managed fields.
- The API has no explicit version prefix such as `/api/v1`.
- `BasicAuthenticationHandler` exists but is not registered in the current `Program.cs` authentication configuration.