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

Request body:

```json
{
  "refreshToken": "your_refresh_token"
}
```

### POST /api/Auth/logout

Revoke a refresh token.

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

Example: `/api/Users?pageNumber=2&pageSize=25`

The response `resultObject` contains `items` and `pagination` metadata (`totalCount`, `pageSize`, `currentPage`, `totalPages`, `hasNext`, `hasPrevious`).

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

Example: `/api/CardInfo?pageNumber=1&pageSize=20`

The response `resultObject` contains `items` and `pagination` metadata (`totalCount`, `pageSize`, `currentPage`, `totalPages`, `hasNext`, `hasPrevious`).

### GET /api/CardInfo/{id}

Get one card record by ID.

### POST /api/CardInfo

Create card info record.

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

PaymentInfo is a read-only, party-level summary. Do not POST, PUT, or DELETE PaymentInfo directly; its rows are maintained automatically from CardInfo changes. There is one summary row per PartyName, and different CardInfo entries for the same party contribute to that row.

### GET /api/PaymentInfo

Get the paginated summaries used by the payment grid.

Query parameters:

- `pageNumber` (optional, default `1`, minimum `1`)
- `pageSize` (optional, default `10`, range `1`-`100`)
- `partyName` (optional, exact PartyName filter)

Example: `/api/PaymentInfo?pageNumber=1&pageSize=20&partyName=Vendor%20A`

The response `resultObject` contains `items` and `pagination` metadata (`totalCount`, `pageSize`, `currentPage`, `totalPages`, `hasNext`, `hasPrevious`). Each item has `id`, `date`, `partyName`, `swipeCount`, `swipeAmount`, `totalAmount`, `profit`, `totalProfit`, `limitUsed`, `cardName`, `merchant`, `swipePerson`, and `remarks`.

Summary calculations use the shared per-entry CardInfo values for that party:

- `swipeCount` = number of CardInfo entries for the PartyName
- `swipeAmount` = the per-entry `deduction` (not a sum)
- `totalAmount` = `deduction * swipeCount`
- `profit` = per-entry CardInfo `profit`
- `totalProfit` = `profit * swipeCount`
- `date`, `limitUsed`, `cardName`, `merchant`, `swipePerson`, and `remarks` come from the latest CardInfo entry for that party

---

## Stock Item

Base route: /api/StockItem

### GET /api/StockItem

Get a paginated list of stock items. Accepts `pageNumber` (default `1`) and `pageSize` (default `10`, maximum `100`). The response uses the common `items` and `pagination` structure.

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

Stock payment summaries are derived from the stock item itself. A record is created, updated, or deleted automatically when the matching StockItem is created, updated, or removed. This keeps the payment sheet aligned with the stock source of truth and avoids direct manual edits.

### GET /api/StockPaymentInfo

Get a paginated list of stock payment summary records. Accepts `pageNumber` (default `1`), `pageSize` (default `10`), optional `partyName`, and optional `inOut` filter. The response uses the common `items` and `pagination` structure.

### GET /api/StockPaymentInfo/{id}

Get a single stock payment summary by ID.

### GET /api/StockPaymentInfo/stock/{stockItemId}

Get the payment summary for one stock item. Accepts `pageNumber` and `pageSize` as query parameters.

Business rules:

- `In` payment means the stock came in / the business is paying out to the party.
- `Out` payment means the stock went out / the business is receiving from the party.
- `amount` is the stock item `amount` for that record.
- `date` is the stock item `inDate`, `outDate`, or created date.
- `partyName`, `bank`, `inOut`, and `remarks` are synchronized from the linked stock item.

Example summary response shape:

```json
{
  "id": 12,
  "no": "ST-001",
  "accountName": "ABC Mobile",
  "accountNo": null,
  "ifsc": null,
  "amount": 72000.00,
  "date": "2026-09-24T00:00:00",
  "partyName": "ABC Mobile",
  "bank": "Cash",
  "inOut": "IN",
  "remarks": "Purchased from supplier",
  "stockItemId": 7
}
```

---

## Cash Ledger

Base route: /api/CashLedger

### GET /api/CashLedger

Get all cash ledger entries.

### GET /api/CashLedger/{id}

Get single cash ledger entry.

### GET /api/CashLedger/type/{transactionType}

Get entries by transaction type.

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

List all bank accounts, including inactive accounts. The response is not paginated.

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

Delete an account. Its own bank transactions are configured to be deleted with it. A referenced related account can prevent deletion while a transaction still refers to it.

### GET /api/BankAccount/summary/total

Return one decimal value containing the total current balance across all bank accounts.

### GET /api/BankAccount/summary/accounts

Return each account's opening, incoming, outgoing, transfer-in, transfer-out, and current balances. Balances are calculated from transaction rows; no balance columns are maintained on the account.

```text
CurrentBalance = OpeningBalance + TotalIn + TransferIn - TotalOut - TransferOut
```

The aggregate assumes all accounts use the same currency. There is no currency conversion.

---

## Bank Transactions

Base route: `/api/BankTransaction`

Bank transactions record all account movements. Valid `transactionType` values are `OPENING`, `IN`, `OUT`, `TRANSFER_IN`, and `TRANSFER_OUT`; `amount` must be greater than zero. `accountId` is required. `relatedAccountId`, `referenceNo`, and `remarks` are optional.

### GET /api/BankTransaction

List all transactions, newest transaction date first. The response is not paginated.

### GET /api/BankTransaction/{id}

Get one transaction by ID, including its account and optional related account details.

### GET /api/BankTransaction/account/{accountId}

List transactions belonging to the specified account, newest transaction date first.

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

For an outgoing payment use `OUT`; for opening funds use `OPENING`. The API does not expose a single atomic transfer operation: record a transfer as a `TRANSFER_OUT` for the source account and a matching `TRANSFER_IN` for the destination account, setting each entry's `relatedAccountId` to the other account. Submit both entries and use the same reference number to keep them associated. If the second request fails, the first entry is not automatically rolled back.

### PUT /api/BankTransaction/{id}

Update a transaction. The route `id` must match the body `id`. Use the same request fields as create, with the transaction `id` included.

### DELETE /api/BankTransaction/{id}

Delete one transaction. The account balance summaries are recalculated from the remaining entries on the next read.

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

For paginated list endpoints, invalid paging values return HTTP 400. The maximum supported `pageSize` is 100. The User and CardInfo endpoints return HTTP status codes corresponding to their result status (including 400 validation failures and 404 missing records); User creation returns HTTP 201 when successful.
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
