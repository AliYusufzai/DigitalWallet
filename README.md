# Digital Wallet API

A production-ready Digital Wallet REST API built with **ASP.NET Core 10**, **Entity Framework Core**, and **SQL Server**. Supports user authentication, wallet management, deposits, withdrawals, and wallet-to-wallet transfers.

---

## Tech Stack

| Layer            | Technology              |
| ---------------- | ----------------------- |
| Framework        | ASP.NET Core 10 Web API |
| Database         | SQL Server (Docker)     |
| ORM              | Entity Framework Core   |
| Authentication   | JWT Bearer Tokens       |
| Password Hashing | BCrypt.Net              |
| Documentation    | Swagger / OpenAPI       |

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

- ✅ User registration and login with JWT authentication
- ✅ One wallet per user with unique wallet number
- ✅ Deposit money into wallet
- ✅ Withdraw money from wallet
- ✅ Transfer money between wallets
- ✅ Transaction history with pagination
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
dotnet ef migrations add InitialCreate \
  --project DigitalWallet.Infrastructure \
  --startup-project DigitalWallet.API

dotnet ef database update \
  --project DigitalWallet.Infrastructure \
  --startup-project DigitalWallet.API
```

---

### 5. Run the API

```bash
dotnet watch --project DigitalWallet.API
```

Open Swagger UI at:

```
https://localhost:{port}/swagger
```

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
  "description": "Salary"
}
```

### Transfer

```json
POST /api/wallet/transfer
Authorization: Bearer eyJhbGci...
{
  "receiverWalletNumber": "W-47392810",
  "amount": 1000,
  "description": "Rent payment"
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
    "items": [...],
    "totalCount": 25,
    "page": 1,
    "pageSize": 10,
    "totalPages": 3,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

---

## Standard API Response

Every response follows this consistent shape:

```json
{
    "success": true,
    "message": "Success",
    "data": {},
    "errors": null
}
```

On failure:

```json
{
    "success": false,
    "message": "Insufficient funds",
    "data": null,
    "errors": ["Insufficient funds. Available: 500, Attempted: 1000"]
}
```

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
├── Currency
├── Status (Active/Suspended/Closed)
├── UserId (FK → Users)
└── CreatedAt

Transactions
├── Id (PK)
├── ReferenceNumber (Unique)
├── Amount (decimal 18,2)
├── Type (Deposit/Withdrawal/Transfer)
├── Status (Pending/Completed/Failed/Reversed)
├── Description
├── WalletId (FK → Wallets)
├── ReceiverWalletId (FK → Wallets, nullable)
└── CreatedAt
```

---

## Project Structure

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
