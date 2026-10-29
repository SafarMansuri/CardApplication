CFS · API change request · 27 Oct 2026

# Search and filter parameters for the list endpoints

These six list endpoints only take `PageNumber` / `PageSize`. Today, when a user searches or picks a filter, the frontend downloads the whole list and filters it in the browser. That gets slower as the data grows, and column sorting only works on the page on screen. `/PaymentInfo` and `/StockPaymentInfo` already work the way we need; please follow the same pattern here.

### Rules for every new parameter

- All parameters are optional. Omitted or empty means no filter.
- They combine with each other and with paging: `pagination.totalCount` is the count *after* filtering.
- `search` is a case-insensitive "contains" match across the listed fields (any field matching is enough).
- An invalid value for a fixed list returns 400 with a message, the way `/PaymentInfo` answers "Status must be Pending or Paid."
- Booleans are `true` / `false`.

## GET /api/CardInfoCard Swipes page

| Parameter | Values | Matches |
| --- | --- | --- |
| `search` | text | partyName, cardNumber, bankName, cardName |
| `isSettled` | true / false | Settled / Unsettled filter |
| `limitUsed` | true / false | Card limit used / not used filter |

## GET /api/UsersUsers page

| Parameter | Values | Matches |
| --- | --- | --- |
| `search` | text | fullName, username, nickName |
| `role` | Master / Admin / User | Role filter |

## GET /api/StockItemStock Items page

| Parameter | Values | Matches |
| --- | --- | --- |
| `search` | text | no, model, imei, partyName, soldTo |
| `status` | Available / Sold / Returned | Status filter |
| `inOut` | IN / OUT | IN/OUT filter |

The existing `/by-party`, `/by-status` and `/by-inout` routes take one filter at a time and an exact party name, so the page can't combine them or search. Once these parameters exist those routes can be retired.

## GET /api/CashLedgerCash Ledger page

| Parameter | Values | Matches |
| --- | --- | --- |
| `search` | text | name, remarks |
| `transactionType` | OPENING / IN / OUT | Type filter |

## GET /api/BankAccountBank Accounts page

| Parameter | Values | Matches |
| --- | --- | --- |
| `search` | text | bankName, accountName, accountNo |
| `isActive` | true / false | Active / Inactive filter |

## GET /api/BankTransactionGET /api/BankTransaction/account/{accountId}Transactions page

| Parameter | Values | Matches |
| --- | --- | --- |
| `search` | text | remarks, referenceNo, the account's and the related account's bankName / accountName |
| `transactionType` | one or more, comma-separated: OPENING, IN, OUT, TRANSFER_IN, TRANSFER_OUT | Type filter |

Needs more than one value because the page groups types: "Money in" is `IN,OPENING` and "Transfer" is `TRANSFER_IN,TRANSFER_OUT`.

## Sorting, on all six endpoints

| Parameter | Values | Notes |
| --- | --- | --- |
| `sortBy` | a field name from the response, e.g. `date`, `amount`, `partyName` | 400 for a field that can't be sorted |
| `sortDir` | asc / desc | Default: newest first |

With server paging, clicking a column header can only sort the 25 rows on screen. Sorting on the server makes it sort the whole list.

## Also needed

1. **User counts**: `GET /api/Users/summary` returning `{ total, masters, admins, users }`. The Users page tiles show 0 for Admins and Employees because there is no way to get these counts without downloading every user.
2. **Bank account counts**: `GET /api/BankAccount/summary/status` returning `{ total, active, inactive }`, for the Active / Inactive tiles on Bank Accounts. Same reason.
3. **Payment status on stock items**: add `paymentStatus` (Pending / Completed) to each item in `/api/StockItem` responses. The page could then grey out Delete and lock the money fields straight from the list, instead of calling `/StockPaymentInfo/stock/{id}` for each item first.