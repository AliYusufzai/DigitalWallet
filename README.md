# Digital Wallet API

A Digital Wallet REST API learning project built with **ASP.NET Core 10**, **Entity Framework Core**, and **SQL Server**. Supports authentication, profile management, wallet operations, filtered transaction history, and saved beneficiaries.

See [frontend.md](./frontend.md) for request examples, retry handling, and frontend integration.

---

## Tech Stack

| Layer            | Technology              |
| ---------------- | ----------------------- |
| Framework        | ASP.NET Core 10 Web API |
| Database         | SQL Server (Docker)     |
| ORM              | Entity Framework Core   |
| Authentication   | JWT Bearer Tokens       |
| Password Hashing | BCrypt.Net              |
| API reference    | README and frontend guide (Swagger disabled) |

---

## Architecture

This project follows a clean **N-Layer Architecture** with 5 separate projects:

```
DigitalWallet/
├── DigitalWallet.API/            ← Controllers, Middleware, Program.cs
├── DigitalWallet.Application/    ← Services, DTOs, Interfaces
├── DigitalWallet.Domain/         ← Entities, Enums, Exceptions
├── DigitalWallet.Infrastructure/ ← Repositories, DbContext, EF Core
└── DigitalWallet.Common/         ← ApiResponse wrapper, PagedResult
```

### Dependency Flow

```
API → Application → Domain
Infrastructure → Application → Domain
Common ← used by all layers
```

---

## Features

- Profile updates and password changes
- Saved beneficiaries (list, add, delete)
- Atomic money operations, idempotency, and wallet concurrency checks

- ✅ User registration and login with JWT authentication
- ✅ One wallet per user with unique wallet number
- ✅ Deposit money into wallet
- ✅ Withdraw money from wallet
- ✅ Transfer money between wallets
- ✅ Transaction history with type/date filters and pagination
- ✅ Global exception handling middleware
- ✅ Standardized API response wrapper
- ✅ Input validation with Data Annotations

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (optional)

---

### 1. Clone the Repository

```bash
git clone https://github.com/AliYusufzai/DigitalWallet.git
cd DigitalWallet
```

---

### 2. Start SQL Server on Docker

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 --name digitalwallet-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

---

### 3. Configure Connection String

Update `appsettings.json` in `DigitalWallet.API`:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=localhost,1433;Database=DigitalWalletDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
    },
    "JwtSettings": {
        "SecretKey": "your-super-secret-key-that-is-long-enough-32-chars",
        "Issuer": "DigitalWalletAPI",
        "Audience": "DigitalWalletClient",
        "ExpiryMinutes": 60
    }
}
```

---

### 4. Run Migrations

```bash
dotnet ef database update \
  --project DigitalWallet.Infrastructure \
  --startup-project DigitalWallet.API
```

---

### 5. Run the API

```bash
dotnet watch --project DigitalWallet.API
```

The API currently binds to `https://localhost:7123` and `http://localhost:5123`
with HTTPS redirection enabled. Trust the local certificate using
`dotnet dev-certs https --trust` if needed. Swagger middleware is currently
commented out; `/swagger` is not enabled.

Browser CORS currently allows `http://localhost:3000` and
`http://localhost:8080`. Other frontend origins need a backend CORS change.

---

## API Endpoints

### Auth

| Method | Endpoint             | Description             | Auth Required |
| ------ | -------------------- | ----------------------- | ------------- |
| POST   | `/api/auth/register` | Register a new user     | No            |
| POST   | `/api/auth/login`    | Login and get JWT token | No            |

### Wallet

| Method | Endpoint               | Description                    | Auth Required |
| ------ | ---------------------- | ------------------------------ | ------------- |
| POST   | `/api/wallet`          | Create a wallet                | Yes           |
| GET    | `/api/wallet`          | Get wallet details and balance | Yes           |
| POST   | `/api/wallet/deposit`  | Deposit money                  | Yes           |
| POST   | `/api/wallet/withdraw` | Withdraw money                 | Yes           |
| POST   | `/api/wallet/transfer` | Transfer to another wallet     | Yes           |

### Transactions

| Method | Endpoint                 | Description                         | Auth Required |
| ------ | ------------------------ | ----------------------------------- | ------------- |
| GET    | `/api/transactions`      | Get transaction history (paginated) | Yes           |
| GET    | `/api/transactions/{id}` | Get single transaction              | Yes           |

### Profile and password

All endpoints use the authenticated user's identity from the JWT; do not send a user ID.

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/api/user/profile` | Read your profile |
| PUT | `/api/user/profile` | Replace full name and phone number; email is not editable here |
| PUT | `/api/user/password` | Change password after verifying the current password |

Profile update body:

```json
{
  "fullName": "Ali Raza",
  "phoneNumber": "03001234567"
}
```

Both fields are required. Full name must be 3-100 characters; phone number must
pass the DTO's phone validation. GET and PUT return profile data containing
`id`, `fullName`, `email`, `phoneNumber`, `isActive`, and `createdAt`.

Password change body:

```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword456!",
  "confirmPassword": "NewPassword456!"
}
```

All fields are required. New password must be 6-100 characters, match confirmation,
and differ from the current password. Success is HTTP 200 with a message and
null data. This endpoint does not issue a replacement JWT or revoke existing JWTs.

### Beneficiaries

A beneficiary is a saved recipient wallet. All endpoints require a bearer token.

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/api/beneficiaries` | List your saved recipients (not paginated) |
| POST | `/api/beneficiaries` | Save an existing wallet with a nickname |
| DELETE | `/api/beneficiaries/{id}` | Delete your saved recipient using its beneficiary ID |

Add body:

```json
{
  "nickname": "Ali",
  "walletNumber": "W-47392810"
}
```

Nickname is required and at most 50 characters. Wallet number is required and
must exist. Saving your own wallet or saving the same wallet twice is rejected.
Each returned item has `id`, `nickname`, `walletNumber`, and `createdAt`.
Add and delete return HTTP 200; delete returns a message with null data.
Deleting another user's beneficiary currently returns 400; a missing ID returns 404.

To pay a beneficiary, pass its `walletNumber` as `receiverWalletNumber` to
`POST /api/wallet/transfer`. Saving or deleting a beneficiary never moves money.
There is no beneficiary-edit or separate beneficiary-payment endpoint.

### Transaction filters

`GET /api/transactions?type=Transfer&from=2026-10-01&to=2026-10-19&page=1&pageSize=10`

| Query parameter | Default | Meaning |
| --- | --- | --- |
| `type` | All types | `Deposit`, `Withdrawal`, or `Transfer` (also numeric 0, 1, 2) |
| `from` | No lower bound | Include records with `CreatedAt >= from` |
| `to` | No upper bound | Current implementation uses `CreatedAt <= to + 1 day` |
| `page` | 1 | One-based page number |
| `pageSize` | 10 | Number of entries per page |

Prefer date-only values (`YYYY-MM-DD`) for the date picker. The current inclusive
upper bound also includes exactly midnight on the following day. It adds a day
even if a time is supplied. Stored timestamps are UTC; the endpoint does not
convert a user's local calendar dates into UTC boundaries.

Results are newest first and scoped to your wallet. Send positive page and
pageSize values; the endpoint currently does not validate their bounds or cap
pageSize. There is no server-side direction or reference-number filter.

Each transaction includes `id`, `referenceNumber`, `amount`, `type`, `direction`,
`status`, `description`, `receiverWalletNumber`, and `createdAt`.
History/detail mapping currently leaves `receiverWalletNumber` null.
The detail endpoint accepts the entry ID, not the shared reference, and returns
404 for entries not owned by your wallet.

---

## Request & Response Examples

### Register

```json
POST /api/auth/register
{
  "fullName": "Ali Raza",
  "email": "ali@example.com",
  "password": "Password123!",
  "phoneNumber": "03001234567"
}
```

### Login

```json
POST /api/auth/login
{
  "email": "ali@example.com",
  "password": "Password123!"
}
```

Response:

```json
{
    "success": true,
    "message": "Login successful",
    "data": {
        "userId": 1,
        "fullName": "Ali Raza",
        "email": "ali@example.com",
        "token": "eyJhbGci...",
        "expiresAt": "2026-10-08T15:00:00Z"
    }
}
```

### Deposit

```json
POST /api/wallet/deposit
Authorization: Bearer eyJhbGci...
{
  "amount": 5000,
  "description": "Salary",
  "idempotencyKey": "deposit-20261019-001"
}
```

### Transfer

```json
POST /api/wallet/transfer
Authorization: Bearer eyJhbGci...
{
  "receiverWalletNumber": "W-47392810",
  "amount": 1000,
  "description": "Rent payment",
  "idempotencyKey": "transfer-20261019-001"
}
```

### Get Transactions (Paginated)

```
GET /api/transactions?page=1&pageSize=10
Authorization: Bearer eyJhbGci...
```

Response:

```json
{
  "success": true,
  "message": "Success",
  "data": {
    "items": [],
    "totalCount": 0,
    "page": 1,
    "pageSize": 10,
    "totalPages": 0,
    "hasNext": false,
    "hasPrevious": false
  }
}
```

---

## Standard API Response

Controller success responses use this shape (camelCase JSON):

```json
{
    "success": true,
    "message": "Success",
    "data": {},
    "errors": null
}
```

Middleware failures currently use PascalCase because they are serialized
directly with `JsonSerializer.Serialize`:

```json
{
  "Success": false,
  "Message": "Current password is incorrect",
  "Data": null,
  "Errors": ["Current password is incorrect"]
}
```

Automatic DTO validation failures use ASP.NET Problem Details, with an `errors`
object keyed by field name. Authentication failures can have an empty body.
See [frontend error handling](./frontend.md#errors) for handling all three shapes.

---

## Database Schema

```
Users
├── Id (PK)
├── FullName
├── Email (Unique)
├── PasswordHash
├── PhoneNumber
├── IsActive
└── CreatedAt

Wallets
├── Id (PK)
├── WalletNumber (Unique)
├── Balance (decimal 18,2)
├── RowVersion (SQL Server concurrency token)
├── Currency
├── Status (Active/Suspended/Closed)
├── UserId (FK → Users)
└── CreatedAt

Transactions
├── Id (PK)
├── ReferenceNumber (unique together with WalletId)
├── Amount (decimal 18,2)
├── Type (Deposit/Withdrawal/Transfer)
├── Direction (Unknown/Outgoing/Incoming)
├── IdempotencyKey (nullable; unique with WalletId when present)
├── RequestHash (nullable; original request fingerprint)
├── Status (Pending/Completed/Failed/Reversed)
├── Description
├── WalletId (FK → Wallets)
├── ReceiverWalletId (FK → Wallets, nullable)
└── CreatedAt
```

Beneficiaries store `Id`, `UserId`, `Nickname`, `WalletNumber`, and `CreatedAt`.
The pair `(UserId, WalletNumber)` is unique.

---

## Project Structure (core files)

```
DigitalWallet.Domain/
├── Entities/
│   ├── User.cs
│   ├── Wallet.cs
│   └── Transaction.cs
├── Enums/
│   ├── TransactionType.cs
│   ├── TransactionStatus.cs
│   └── WalletStatus.cs
└── Exceptions/
    ├── NotFoundException.cs
    ├── InsufficientFundsException.cs
    └── WalletException.cs

DigitalWallet.Application/
├── DTOs/
│   ├── Auth/
│   ├── Wallet/
│   └── Transaction/
├── Interfaces/
│   ├── Repositories/
│   └── Services/
├── Services/
│   ├── AuthService.cs
│   ├── WalletService.cs
│   └── TransactionService.cs
└── Settings/
    └── JwtSettings.cs

DigitalWallet.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── AppDbContextFactory.cs
│   └── Configurations/
│       ├── UserConfiguration.cs
│       ├── WalletConfiguration.cs
│       └── TransactionConfiguration.cs
└── Repositories/
    ├── UserRepository.cs
    ├── WalletRepository.cs
    └── TransactionRepository.cs

DigitalWallet.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── WalletController.cs
│   └── TransactionController.cs
└── Middleware/
    └── ExceptionMiddleware.cs
```

---

## Author

**Ali Raza Khan** — Backend Software Engineer

- GitHub: [@AliYusufzai](https://github.com/AliYusufzai)


## Money-operation safety and client retries

Deposit, withdrawal, and transfer bodies now require an `idempotencyKey`
(1-64 characters). Generate a new key per intended operation; reuse the same
key and identical body after a timeout or retryable HTTP 409:

```json
{
  "receiverWalletNumber": "W-12345678",
  "amount": 50.00,
  "description": "Lunch",
  "idempotencyKey": "22ed59d86a184b1db73ce9a83ba0d54c"
}
```

Keys are scoped to the initiating wallet across all three operation types.
A completed retry returns the original transaction. Reusing a key with changed
inputs returns HTTP 409. Concurrent balance updates also return 409 and roll
back the operation. Retry with the same key, never a fresh one. A conflict caused
by changed inputs requires correcting the request, not an automatic retry.

Amounts must be between 1 and 1,000,000 with at most two decimal places.
History includes `direction`: Incoming, Outgoing, or Unknown for legacy transfers.
Both transfer entries share a GUID-based reference.

Apply migrations before starting the updated API:

```powershell
dotnet ef database update --project DigitalWallet.Infrastructure --startup-project DigitalWallet.API
```

The safety migration adds wallet rowversions, directions, and a filtered unique
idempotency index. It backfills deposit/withdrawal direction without changing
balances. Legacy transfers remain Unknown because free-text descriptions cannot
reliably establish direction. Replaying requests made before idempotency tracking
was introduced cannot be detected.

### Verification

The current checkout does not include the previously described SQL regression
runner. Documentation was checked against source code; endpoint examples were
not executed against a running API.
