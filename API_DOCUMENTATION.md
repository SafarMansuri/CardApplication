# CardApplication API Documentation

## Overview

This API is built with ASP.NET Core and provides authentication, user management, card information and payment summaries, stock and stock payment tracking, cash ledger tracking, and bank account transaction tracking.

The application follows a layered architecture:

- Controller layer
- Service layer
- Repository layer
- EF Core DbContext
- SQL Server database

All protected endpoints require a valid JWT bearer token.

---

## Base URL

Use the project base URL from your local/hosted environment.

Examples:

- Local development: https://localhost:7154
- Local HTTP: http://localhost:5156

Swagger UI is available at `/swagger` when running the API locally.

---

## Authentication

### POST /api/Auth/login

Authenticate a user and receive a JWT token pair.

Request body:

```json
{
  "username": "master",
  "password": "Master@123"
}
```

Response:

```json
{
  "accessToken": "jwt_token_here",
  "refreshToken": "refresh_token_here",
  "user": {
    "id": 1,
    "username": "master",
    "role": "Master",
    "fullName": "System Master"
  }
}
```

### POST /api/Auth/refresh

Refresh an expired or near-expired access token.

Rotation revokes the old refresh token and creates its replacement atomically. Concurrent refresh requests using the same old token permit only one successful rotation; the other request returns HTTP 401. Save the returned replacement token and use it for later refresh/logout requests.

Request body:

```json
{
  "refreshToken": "your_refresh_token"
}
```

### POST /api/Auth/logout

Revoke a refresh token.

Use the latest refresh token after rotation. Logout does not invalidate an already-issued access JWT before its expiry.

Request body:

```json
{
  "refreshToken": "your_refresh_token"
}
```

### POST /api/Auth/seed

Create the initial master account when the database is empty.

Response:

```json
{
  "message": "Master user created successfully.",
  "username": "master",
  "defaultPassword": "Master@123"
}
```

> This endpoint should only be used for initial setup and should be restricted in production.

---

## Users

Base route: /api/Users

### GET /api/Users

Get a paginated list of users. Passwords and password hashes are not returned.

Required role: Admin or Master

Query parameters:

- `pageNumber` (optional, default `1`, minimum `1`)
- `pageSize` (optional, default `10`, range `1`-`100`)
- `search` (optional, case-insensitive contains match on `fullName`, `username`, `nickName`)
- `role` (optional, `Master`, `Admin`, or `User`; invalid values return HTTP 400)
- `sortBy` (optional, one of `id`, `username`, `fullName`, `nickName`, `phoneNumber`, `role`, `createdat`, `updatedat`; invalid values return HTTP 400)
- `sortDir` (optional, `asc` or `desc`)

Example: `/api/Users?pageNumber=1&pageSize=25&search=john&role=User&sortBy=username&sortDir=asc`

The response `resultObject` contains `items` and `pagination` metadata (`totalCount`, `pageSize`, `currentPage`, `totalPages`, `hasNext`, `hasPrevious`).

### GET /api/Users/summary

Get user counts grouped by role: `{ total, masters, admins, users }`.

Required role: Admin or Master

### GET /api/Users/{id}

Get a user by ID.

Required role: Admin or Master

Password values are never included in User API responses.

### POST /api/Users

Create a new user.

Required role: Admin or Master

Request body:

```json
{
  "username": "john",
  "password": "Secret123",
  "phoneNumber": "9876543210",
  "fullName": "John Doe",
  "nickName": "JD",
  "additionalInfo": "New user",
  "role": "User"
}
```

Backend validation requires a username of 3-100 characters, a password of 8-128 characters, and a role of `Master`, `Admin`, or `User`. Duplicate usernames are rejected.

### PUT /api/Users/{id}

Update an existing user.

Required role: Admin or Master

The route ID must match the body `id`; username, password, and role validation also applies. A duplicate username or missing user is rejected.

### DELETE /api/Users/{id}

Delete a user.

Required role: Admin or Master

Deleting an unknown ID returns not found.

---

## Card Info

Base route: /api/CardInfo

### GET /api/CardInfo

Get a paginated list of card records.

Query parameters:

- `pageNumber` (optional, default `1`, minimum `1`)
- `pageSize` (optional, default `10`, range `1`-`100`)
- `search` (optional, case-insensitive contains match on `partyName`, `cardNumber`, `bankName`, `cardName`)
- `isSettled` (optional, `true` or `false`)
- `limitUsed` (optional, `true` or `false`)
- `sortBy` (optional, one of `id`, `date`, `partyName`, `cardName`, `cardNumber`, `bankName`, `deduction`, `profit`, `pos`, `model`, `swipePerson`, `merchant`, `limitUsed`, `isSettled`, `settledAt`, `createdat`, `updatedat`)
- `sortDir` (optional, `asc` or `desc`, default `desc`)

Example: `/api/CardInfo?pageNumber=1&pageSize=20&search=hdfc&isSettled=false&sortBy=date&sortDir=desc`

The response `resultObject` contains `items` and `pagination` metadata (`totalCount`, `pageSize`, `currentPage`, `totalPages`, `hasNext`, `hasPrevious`).

### GET /api/CardInfo/{id}

Get one card record by ID.

### POST /api/CardInfo

Create card info record.

`bankName` is required and limited to 50 characters, matching the database constraint. Missing, blank, or oversized values return HTTP 400 rather than a database error.

Request body example:

```json
{
  "cardName": "Visa Card",
  "cardNumber": "1234 5678 9012 3456",
  "imei": "76867hasd8",
  "partyName": "Vendor A",
  "deduction": 250.00,
  "profit": 25.00,
  "date": "2026-09-24T00:00:00",
  "pos": "POS-01",
  "model": "Phone model",
  "merchant": "Merchant name",
  "swipePerson": "Person name",
  "remarks": "Card issued",
  "limitUsed": true,
  "additionalInfo": "Priority customer",
  "bankName": "HDFC"
}
```

`cardName`, `cardNumber`, and `partyName` are required. `imei` is text so it can contain letters and digits. `deduction` and `profit` cannot be negative. Each PartyName's CardInfo entries are expected to share the same deduction and profit for summary calculations.

### PUT /api/CardInfo/{id}

Update card info record.

### DELETE /api/CardInfo/{id}

Delete card info record.

Creating, updating, or deleting CardInfo recalculates the corresponding party's PaymentInfo summary in the same database transaction. If a CardInfo record moves to another PartyName, both the old and new party summaries are recalculated.

---

## Payment Info

Base route: /api/PaymentInfo

PaymentInfo is a lifetime party-level summary maintained automatically from CardInfo. There is one row per PartyName. New swipes begin pending; an Admin or Master can settle all currently pending swipes. Settled CardInfo rows cannot be updated or deleted.

### GET /api/PaymentInfo

Get paginated party summaries. Accepts `pageNumber`, `pageSize`, optional exact `partyName`, and optional `status=Pending|Paid`. Unsupported status values return HTTP 400.

Example: `/api/PaymentInfo?pageNumber=1&pageSize=20&partyName=Vendor%20A&status=Pending`

Each item includes the existing party/display fields plus `paidAmount`, `pendingAmount`, `paymentStatus`, `lastPaidAt`, `lastPaidByUserId`, and `lastPaidByUsername`.

Summary calculations are:

- `swipeCount` = number of CardInfo rows for the party
- `totalAmount` = sum of every row's `deduction`
- `totalProfit` = sum of every row's `profit`
- `paidAmount` = sum represented by settled CardInfo rows
- `pendingAmount` = `totalAmount - paidAmount`
- `paymentStatus` = `Paid` when pending amount is zero, otherwise `Pending`
- latest CardInfo supplies the representative date/card/merchant/swipe-person/remarks fields

### PATCH /api/PaymentInfo/{id}/mark-paid

Required role: `Admin` or `Master`.

Atomically settles every currently pending CardInfo row in the selected party summary. The API sets pending amount to zero, updates paid amount to the lifetime total, and records the UTC payment time and authenticated JWT user. No request body is required. A missing summary returns 404; a summary with no pending amount returns 409. A later CardInfo swipe preserves the previous paid amount and reopens only that new value as pending.

---

## Stock Item

Base route: /api/StockItem

### GET /api/StockItem

Get a paginated list of stock items. Accepts `pageNumber` (default `1`), `pageSize` (default `10`, maximum `100`), plus optional filter and sorting parameters:

- `search` (case-insensitive contains match on `no`, `model`, `imei`, `partyName`, `soldTo`)
- `status` (`Available`, `Sold`, or `Returned`; invalid values return HTTP 400)
- `inOut` (`IN` or `OUT`; invalid values return HTTP 400)
- `sortBy` (`id`, `no`, `inDate`, `outDate`, `imei`, `colour`, `model`, `gstMrp`, `amount`, `partyName`, `payment`, `inOut`, `status`, `soldTo`, `remarks`, `paymentStatus`, `createdAt`, `updatedAt`)
- `sortDir` (`asc` or `desc`, default `desc`)

Every `StockItem` response includes `paymentStatus` (`Pending` or `Completed`) synchronized from its `StockPaymentInfo` obligation.

### GET /api/StockItem/{id}

Get a stock item by ID.

### GET /api/StockItem/by-party/{partyName}

Filter stock items by party name. Accepts `pageNumber` and `pageSize` as query parameters.

### GET /api/StockItem/by-status/{status}

Filter stock items by status. Accepts `pageNumber` and `pageSize` as query parameters.

### GET /api/StockItem/by-inout/{inOut}

Filter stock items by in/out value. Accepts `pageNumber` and `pageSize` as query parameters.

### POST /api/StockItem

Create a stock item record.

Request body example:

```json
{
  "no": "ST-001",
  "inDate": "2026-09-24T00:00:00",
  "outDate": null,
  "imei": "355123456789012",
  "colour": "Black",
  "model": "iPhone 15",
  "gstMrp": 79999.00,
  "amount": 72000.00,
  "partyName": "ABC Mobile",
  "payment": "Cash",
  "inOut": "IN",
  "status": "Available",
  "soldTo": "",
  "remarks": "Purchased from supplier"
}
```

### PUT /api/StockItem/{id}

Update stock item.

### DELETE /api/StockItem/{id}

Delete stock item.

---

## Stock Payment Info

Base route: /api/StockPaymentInfo

Each StockItem automatically owns one StockPaymentInfo obligation. `IN` creates a supplier `Payable` using `PartyName`; `OUT` creates a buyer `Receivable` using `SoldTo`. New obligations are `Pending`, and Admin/Master users settle them per item.

StockItem validation requires `IN` or `OUT`, a positive amount, PartyName for IN, and SoldTo for OUT. Available inventory is defined by `status=Available`.

### GET /api/StockPaymentInfo

Returns paginated obligations. Filters: `partyName`, `inOut=IN|OUT`, `paymentType=Payable|Receivable`, and `status=Pending|Completed`.

### GET /api/StockPaymentInfo/{id}

Get one obligation, including amount, outstanding amount, classification, status, and completion audit.

### GET /api/StockPaymentInfo/stock/{stockItemId}

Get the obligation linked to a StockItem.

### PATCH /api/StockPaymentInfo/{id}/settle

Admin/Master only. Completes the full outstanding amount atomically and creates an immutable Settlement history event. Partial settlement is not supported.

```json
{
  "amount": 72000.00,
  "transactionDate": "2026-10-10T13:00:00Z",
  "paymentMethod": "Bank Transfer",
  "referenceNo": "PAY-001",
  "remarks": "Supplier paid"
}
```

### PATCH /api/StockPaymentInfo/{id}/reset

Admin/Master only. Returns a Completed obligation to Pending and records an immutable Reset event. Body: `{ "reason": "Correction reason" }`.

### GET /api/StockPaymentInfo/{id}/history

Returns paginated Settlement and Reset audit events, including amount, date, method/reference, remarks, authenticated admin, and creation time.

### GET /api/StockPaymentInfo/summary/parties

Returns party-wise totals grouped separately by Payable and Receivable: item count, total amount, completed amount, and outstanding amount. Supports party, type, status, and pagination filters.

### GET /api/StockPaymentInfo/summary/totals

Returns `pendingPayable` and `pendingReceivable` across all stock obligations.

Completed obligations protect StockItem amount, direction, PartyName, SoldTo, and deletion until an admin resets the payment. Descriptive fields remain editable.

---

## Cash Ledger

Base route: /api/CashLedger

### GET /api/CashLedger

Get a paginated list of cash ledger entries. Accepts `pageNumber` (default `1`), `pageSize` (default `10`, range `1`-`100`), plus optional filter and sort parameters:

- `search` (case-insensitive contains match on `name`, `remarks`)
- `transactionType` (`OPENING`, `IN`, or `OUT`; invalid values return HTTP 400)
- `sortBy` (`id`, `transactionDate`, `transactionType`, `name`, `amount`, `remarks`, `createdOn`, `createdBy`)
- `sortDir` (`asc` or `desc`, default `desc` by transaction date and ID)

The `resultObject` contains `items` and `pagination` metadata.

### GET /api/CashLedger/{id}

Get single cash ledger entry.

### GET /api/CashLedger/type/{transactionType}

Get a paginated list of entries by transaction type, ordered by transaction date and ID ascending. Accepts `pageNumber` and `pageSize` with the same defaults and limits as `GET /api/CashLedger`.

Valid values:

- OPENING
- IN
- OUT

### GET /api/CashLedger/summary/opening

Returns opening balance total.

### GET /api/CashLedger/summary/in

Returns total cash-in amount.

### GET /api/CashLedger/summary/out

Returns total cash-out amount.

### GET /api/CashLedger/summary/current

Returns current balance using:

```
Current Balance = Opening + IN - OUT
```

### POST /api/CashLedger

Create a new cash ledger record.

Example request:

```json
{
  "transactionDate": "2026-09-24T00:00:00",
  "transactionType": "IN",
  "name": "Customer payment",
  "amount": 25000.00,
  "remarks": "Cash received from sale",
  "createdBy": "admin"
}
```

### PUT /api/CashLedger/{id}

Update an existing cash ledger record.

### DELETE /api/CashLedger/{id}

Delete a cash ledger record.

---

## Bank Accounts

Base route: `/api/BankAccount`

Bank accounts are stored as rows, so adding another account does not require a database schema change. The account name must be unique. These endpoints are separate from `CashLedger`; this module does not modify or combine cash ledger records.

### GET /api/BankAccount

List bank accounts, including inactive accounts. Accepts `pageNumber` (default `1`), `pageSize` (default `10`, range `1`-`100`), plus optional filter and sort parameters:

- `search` (case-insensitive contains match on `bankName`, `accountName`, `accountNo`)
- `isActive` (`true` or `false`)
- `sortBy` (`id`, `accountName`, `bankName`, `accountNo`, `isActive`, `createdAt`, `updatedAt`)
- `sortDir` (`asc` or `desc`)

The `resultObject` contains `items` and `pagination` metadata.

### GET /api/BankAccount/summary/status

Return bank account counts by active state: `{ total, active, inactive }`.

### GET /api/BankAccount/{id}

Get one account by ID.

### POST /api/BankAccount

Create a bank account. `accountName` and `bankName` are required; `accountNo` is optional.

```json
{
  "accountName": "Main Current Account",
  "bankName": "State Bank",
  "accountNo": "1000000001",
  "isActive": true
}
```

### PUT /api/BankAccount/{id}

Update the account details and active status. The route `id` must match the body `id`.

```json
{
  "id": 1,
  "accountName": "Main Current Account",
  "bankName": "State Bank",
  "accountNo": "1000000001",
  "isActive": true
}
```

### DELETE /api/BankAccount/{id}

Delete an account. Its own bank transactions are deleted, and references to it from other accounts' transactions are set to null in the same transaction. This retains the current deletion policy; it is not an accounting reversal.

### GET /api/BankAccount/summary/total

Return one decimal value containing the total current balance across all bank accounts.

### GET /api/BankAccount/summary/accounts

Return a paginated list of accounts with opening, incoming, outgoing, transfer-in, transfer-out, and current balances. Accepts `pageNumber` (optional, default `1`) and `pageSize` (optional, default `10`, range `1`-`100`). Balances are calculated from transaction rows; no balance columns are maintained on the account.

```text
CurrentBalance = OpeningBalance + TotalIn + TransferIn - TotalOut - TransferOut
```

The aggregate assumes all accounts use the same currency. There is no currency conversion.

---

## Bank Transactions

Base route: `/api/BankTransaction`

Bank transactions record all account movements. `amount` must be greater than zero. The standard transaction endpoint accepts `OPENING`, `IN`, `OUT`, `TRANSFER_IN`, or `TRANSFER_OUT`; for a managed account-to-account transfer, use the atomic transfer endpoint below. `accountId` is required for standard transactions. `relatedAccountId`, `referenceNo`, and `remarks` are optional.

### GET /api/BankTransaction

List transactions, newest transaction date and ID first by default. Accepts `pageNumber` (default `1`), `pageSize` (default `10`, range `1`-`100`), plus optional filter and sort parameters:

- `search` (case-insensitive contains match on `remarks`, `referenceNo`, and the account's or related account's `bankName` / `accountName`)
- `transactionType` (one or more comma-separated values from `OPENING, IN, OUT, TRANSFER_IN, TRANSFER_OUT`; invalid values return HTTP 400)
- `sortBy` (`id`, `accountId`, `relatedAccountId`, `transactionDate`, `transactionType`, `amount`, `referenceNo`, `remarks`, `createdAt`, `updatedAt`, `accountName`, `bankName`)
- `sortDir` (`asc` or `desc`, default `desc`)

The `resultObject` contains `items` and `pagination` metadata.

### GET /api/BankTransaction/{id}

Get one transaction by ID, including its account and optional related account details.

### GET /api/BankTransaction/account/{accountId}

List transactions belonging to the specified account. Supports the same `search`, comma-separated `transactionType`, `sortBy`, `sortDir`, `pageNumber`, and `pageSize` parameters as `GET /api/BankTransaction`.

### POST /api/BankTransaction

Create a movement for one account.

```json
{
  "accountId": 1,
  "relatedAccountId": null,
  "transactionDate": "2026-09-29T12:30:00",
  "transactionType": "IN",
  "amount": 25000.00,
  "referenceNo": "IN-001",
  "remarks": "Customer payment received"
}
```

For an outgoing payment use `OUT`; for opening funds use `OPENING`. Use the dedicated transfer endpoint for movements between managed bank accounts so both sides are recorded atomically.

### POST /api/BankTransaction/transfer

Transfer money between two managed bank accounts in one request. The API verifies both accounts exist, rejects transfers to the same account, and rejects amounts greater than the source account's available balance. It creates a `TRANSFER_OUT` entry for the source and a matching `TRANSFER_IN` entry for the destination, with reciprocal `relatedAccountId` values. Both entries share the amount, date, reference, and remarks and are saved in one database transaction.

```json
{
  "fromAccountId": 1,
  "toAccountId": 2,
  "transactionDate": "2026-09-29T14:00:00",
  "amount": 1000.00,
  "referenceNo": "TRF-001",
  "remarks": "Move funds to savings"
}
```

The response `resultObject` contains `sourceTransaction` and `destinationTransaction`. Invalid account IDs or a same-account transfer return HTTP 400; missing accounts return HTTP 404; insufficient balance returns HTTP 400. If either ledger row fails to save, neither row is committed.

### PUT /api/BankTransaction/{id}

Update a transaction. The route `id` must match the body `id`. Use the same request fields as create, with the transaction `id` included.

### DELETE /api/BankTransaction/{id}

Delete one transaction. The account balance summaries are recalculated from the remaining entries on the next read.

---

## Dashboard

Base route: `/api/Dashboard` (Restricted to `Admin` and `Master` roles)

### GET /api/Dashboard/summary

Returns consolidated financial and inventory metrics:

- `totalBankBalance`, `totalAvailableCash`, `totalSwipes`, `totalPartyProfit`, `totalStockItems` (count of all `StockItems` rows), `totalPendingPartyPayment`
- `availableStockCount`, `availableStockValue`, `pendingStockPayable`, `pendingStockReceivable`
- `totalCapital` (`totalBankBalance + totalAvailableCash`)
- `totalOwedToYou` (`pendingStockReceivable`)
- `totalYouOwe` (`pendingStockPayable + totalPendingPartyPayment`)
- `netPosition` (`totalOwedToYou - totalYouOwe`)
- `stockAvailablePct` (`round(availableStockCount / totalStockItems * 100, 1)`, or `0` when `totalStockItems` is `0`)
- `accounts`: list of `{ accountId, bankName, accountName, currentBalance }`

### GET /api/Dashboard/monthly-flow?months=12

Returns zero-filled monthly IST (`YYYY-MM`) buckets for the last `months` (`1`-`60`, default `12`):
`[{ "month": "2026-09", "bankIn": 0, "bankOut": 0, "cashIn": 0, "cashOut": 0 }]`.

### GET /api/Dashboard/swipes-daily?days=30

Returns zero-filled daily IST (`YYYY-MM-DD`) buckets for the last `days` (`1`-`365`, default `30`):
`[{ "date": "2026-10-27", "count": 0, "amount": 0, "profit": 0 }]`.

### GET /api/Dashboard/top-parties?limit=5

Returns the top `limit` (`1`-`50`, default `5`) parties owed across card parties (`type: "CardParty"`) and stock suppliers (`type: "Supplier"`), ordered by `pending` descending:
`[{ "partyName": "", "type": "CardParty", "pending": 0 }]`.

---

## Weather Forecast Sample

### GET /WeatherForecast

Return five generated sample forecast records. This is a protected sample endpoint and requires a bearer token; it is not connected to an external weather service.

---

## Common response format

Most business API responses use this envelope:

```json
{
  "statusCode": 200,
  "statusText": "Success",
  "statusMessage": "Operation completed successfully.",
  "resultObject": {}
}
```

Authentication endpoints return their own JSON response shapes (for example, login returns `accessToken`, `refreshToken`, and `user`). Common business response status values are:

- 200 = Success
- 400 = Bad request / validation failure
- 401 = Unauthorized
- 403 = Forbidden
- 404 = Resource not found
- 409 = Conflict, such as a duplicate username

For paginated list endpoints, invalid paging values return HTTP 400. The maximum supported `pageSize` is 100. Business controllers, including bank accounts, bank transactions, and cash ledger, propagate result statuses (including 400 validation failures and 404 missing records). Duplicate bank-account names return 409. Successful creation returns HTTP 201 with `statusCode: 200` in the response envelope. Framework validation and authentication responses may use a different shape.
- 404 = Not found
- 500 = Server error

---

## JWT usage

Add the token to the Authorization header:

```http
Authorization: Bearer <your_access_token>
```

Example:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Business summary

This application supports the following business flows:

1. User authenticates and receives a JWT token.
2. Users with the required role manage users; authenticated users manage card, stock, cash, and bank records.
3. PaymentInfo is recalculated from CardInfo by party; StockPaymentInfo is synchronized from its StockItem.
4. CashLedger tracks opening, incoming, and outgoing cash independently.
5. BankAccounts and BankTransactions support multiple dynamic accounts, with balances calculated from movements.

This is useful for:

- stock buying and selling records
- party-wise stock tracking
- payment history against stock items
- opening/in/out cash balance reporting
- per-account and combined bank balance reporting

---

## Notes for production

The current project has a few things to review before production use:

- CORS is open for all origins
- seed user endpoint is public and should be restricted
- password handling should be reviewed for stricter production security
- response DTOs should be separated from entity models in some scenarios

---

## Quick summary

This API has these modules:

- Auth
- Users
- Card info
- Stock items and stock payment
- Cash ledger
- Bank accounts and bank transactions
- Weather forecast sample

Together, these modules cover stock movement, derived payment summaries, cash movement, and movements across multiple bank accounts. `PaymentInfo` and `StockPaymentInfo` are read-only derived data; `CashLedger` remains a separate legacy cash ledger.
