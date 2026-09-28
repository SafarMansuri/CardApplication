# CardApplication API Documentation

## Overview

This API is built with ASP.NET Core and provides authentication, user management, card information management, stock tracking, stock payment tracking, and cash ledger tracking.

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

Swagger UI is available in development for API exploration.

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

### GET /api/StockPaymentInfo

Get a paginated list of stock payment records. Accepts `pageNumber` (default `1`) and `pageSize` (default `10`, maximum `100`). The response uses the common `items` and `pagination` structure.

### GET /api/StockPaymentInfo/{id}

Get a single payment record by ID.

### GET /api/StockPaymentInfo/stock/{stockItemId}

Get a paginated list of payment records for a specific stock item. Accepts `pageNumber` and `pageSize` as query parameters.

### POST /api/StockPaymentInfo

Create a stock payment record.

Request body example:

```json
{
  "no": "SP-001",
  "accountName": "ABC Traders",
  "accountNo": "1234567890",
  "ifsc": "HDFC0001234",
  "amount": 50000.00,
  "date": "2026-09-24T00:00:00",
  "partyName": "ABC Mobile",
  "bank": "HDFC",
  "remarks": "Advance payment",
  "stockItemId": 1
}
```

### PUT /api/StockPaymentInfo/{id}

Update stock payment detail.

### DELETE /api/StockPaymentInfo/{id}

Delete stock payment detail.

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

## Common response format

All responses follow the same format:

```json
{
  "statusCode": 200,
  "statusText": "Success",
  "statusMessage": "Operation completed successfully.",
  "resultObject": {}
}
```

Common status values:

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

This application supports the following business flow:

1. User authenticates and receives a JWT token.
2. User creates or manages stock items.
3. Stock payment records link to the relevant stock item.
4. Cash ledger tracks opening, incoming, and outgoing cash.
5. Business reports can be derived from item movement + payment + cash ledger.

This is useful for:

- stock buying and selling records
- party-wise stock tracking
- payment history against stock items
- opening/in/out cash balance reporting
- overall daily cash management

---

## Notes for production

The current project has a few things to review before production use:

- CORS is open for all origins
- seed user endpoint is public and should be restricted
- password handling should be reviewed for stricter production security
- response DTOs should be separated from entity models in some scenarios

---

## Quick summary

This API has 5 major modules:

- Auth
- Users
- Card info
- Stock items and stock payment
- Cash ledger

Together, these modules cover the business flow of tracking stock movement, stock payments, and cash movement for daily operations.
