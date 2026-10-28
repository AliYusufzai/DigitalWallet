# Digital Wallet API

A backend learning project built with **ASP.NET Core 10**, **SQL Server**, **Entity Framework Core**, and **Dapper**. It supports verified accounts, wallet operations, email OTP confirmation for transfers, daily spending limits, audit records, and financial reports.

This repository contains the API and supporting layers. It does not include a frontend or connect deposits/withdrawals to a real bank or payment provider.

## Features

### Accounts

- Registration and email verification before login.
- Random verification tokens stored as hashes; one-hour expiry and 60-second resend cooldown.
- JWT bearer authentication and BCrypt password hashing.
- Profile retrieval and updates to full name and phone number.
- Password changes after checking the current password.
- Authenticated user identity from token claims for ownership checks and audit attribution.

Registration does not return a login token. Verify the email, then log in. Registration and resend responses use generic wording to avoid revealing account existence.

### Wallets and money movement

- One wallet per user, unique wallet numbers, and PKR as the default currency.
- Deposits, withdrawals, and transfers between active wallets with matching currencies.
- Amounts from 1 to 1,000,000, with at most two decimal places.
- Insufficient-funds and self-transfer checks.
- Combined **PKR 100,000 daily outgoing limit** for withdrawals and outgoing transfers, resetting at midnight Pakistan time.
- Database transactions covering balance changes, transaction records, and related audit writes.
- SQL Server rowversion checks for concurrent wallet updates.
- Idempotency keys and request fingerprints to prevent duplicate execution and reject changed inputs using the same key.
- Separate outgoing and incoming transfer entries sharing a reference number.

### Email OTP transfer confirmation

Transfers use two steps: request a confirmation code, then submit it to execute the saved transfer.

- Random six-digit OTP stored as a BCrypt hash.
- **120-second expiry**, **3 incorrect attempts**, **60-second send cooldown**.
- Cooldown applies per user across requests and resends, including different idempotency keys.
- Resending replaces the code without resetting failed attempts.
- Approval is tied to the saved recipient, amount, currency, and description.
- Account ownership/status, wallet state, balance, and daily-limit checks before execution.
- SQL Server transaction-owned application locks serialize OTP operations per user.
- Payment and confirmation completion commit together.
- Repeated completed confirmations return the original transaction without paying again.
- Recoverable email-delivery failures through the saved request and resend flow.

**Requesting an OTP does not move or reserve money.** Deposits and withdrawals retain their direct API flow.

### Transactions and beneficiaries

- Transaction history with type/date filters and pagination.
- Individual transaction lookup scoped to the authenticated user.
- Saved recipient wallets with nicknames: list, add, and delete.
- Duplicate and own-wallet beneficiary checks.

### Reports

Reports use **Dapper and SQL Server stored procedures**, scoped to the authenticated user.

| Report | Contents |
| --- | --- |
| Spending by type | Counts and totals for completed outgoing transfers and withdrawals |
| Daily volume | Counts and separate incoming/outgoing totals per Pakistan calendar day |
| Monthly statement | Opening balance, incoming/outgoing totals, closing balance, transaction rows, running balances |

Report date ranges include both selected Pakistan calendar days and are converted to UTC boundaries. Monthly statements use a Pakistan calendar month. Daily-volume results omit days without activity.

Statement balances come from completed transaction history, assuming an initial zero balance. Direct balance edits or incomplete/legacy history can cause differences from the wallet balance. Reports currently return JSON, not PDF or CSV files.

### Logging and auditing

- Serilog structured logging to the console and Seq.
- HTTP method, path, status, and elapsed time in request logs.
- Centralized exception handling.
- EF Core save interceptor audits wallet balance/status changes, user full name/phone number changes, and beneficiary deletions.
- Explicit beneficiary-creation auditing after obtaining the database ID, inside the same transaction.
- Audit records include actor, entity name/ID, action, old/new values, and UTC timestamp.

Audit fields are allowlisted; this is not an audit of every database column. No public audit-log browsing endpoint exists.

## Technology and architecture

| Concern | Implementation |
| --- | --- |
| HTTP API | ASP.NET Core 10 controllers |
| Database | SQL Server, local installation or container |
| Persistence | Entity Framework Core 10 and migrations |
| Reporting | Dapper and stored procedures |
| Authentication | JWT bearer tokens |
| Password / transfer OTP hashing | BCrypt.Net |
| Email | MailKit / MimeKit with SMTP STARTTLS |
| Logging | Serilog and Seq |
| Responses | ApiResponse and PagedResult wrappers |

| Project | Responsibility |
| --- | --- |
| [API](DigitalWallet.API/) | Controllers, authentication, current-user identity, middleware, configuration |
| [Application](DigitalWallet.Application/) | Business services, DTOs, interfaces, settings |
| [Domain](DigitalWallet.Domain/) | Entities, enums, domain exceptions |
| [Infrastructure](DigitalWallet.Infrastructure/) | EF mappings/migrations, repositories, Dapper, SMTP, auditing, unit of work |
| [Common](DigitalWallet.Common/) | Shared response and pagination wrappers |

~~~text
Controller -> Application service -> Repository -> SQL Server
                                 -> Email sender -> SMTP
~~~

The API composes Application and Infrastructure through dependency injection. Infrastructure implements interfaces declared in Application.

## Local setup

Run these PowerShell commands from the repository root.

### 1. Prerequisites

- .NET 10 SDK.
- SQL Server; Docker is optional if SQL Server is already installed.
- SMTP account supporting STARTTLS, normally on port 587.
- Seq for the configured log dashboard, or an adjusted Seq sink for your environment.
- A frontend verification page, or an API client to submit verification tokens manually.

### 2. Clone and restore

~~~powershell
git clone https://github.com/AliYusufzai/DigitalWallet.git
cd DigitalWallet
dotnet restore DigitalWallet.slnx
~~~

If the EF CLI is not installed:

~~~powershell
dotnet tool install --global dotnet-ef --version 10.*
~~~

### 3. Configure services

Use an existing local SQL Server instance, or create a development container after replacing the password placeholder:

~~~powershell
docker run --name digitalwallet-sql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<strong-local-password>" -p 1433:1433 -v digitalwallet-sql-data:/var/opt/mssql -d mcr.microsoft.com/mssql/server:2022-latest
~~~

The configured Seq sink sends events to http://localhost:5341. Point it at your Seq server if different.

The API already has a UserSecretsId. Store development credentials in user secrets rather than committed configuration:

~~~powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=DigitalWalletDb;User Id=sa;Password=<strong-local-password>;TrustServerCertificate=True;" --project DigitalWallet.API

dotnet user-secrets set "JwtSettings:SecretKey" "<random-secret-at-least-32-bytes>" --project DigitalWallet.API
dotnet user-secrets set "JwtSettings:Issuer" "DigitalWalletAPI" --project DigitalWallet.API
dotnet user-secrets set "JwtSettings:Audience" "DigitalWalletClient" --project DigitalWallet.API
dotnet user-secrets set "JwtSettings:ExpiryMinutes" "60" --project DigitalWallet.API

dotnet user-secrets set "EmailSettings:Host" "<smtp-host>" --project DigitalWallet.API
dotnet user-secrets set "EmailSettings:Port" "587" --project DigitalWallet.API
dotnet user-secrets set "EmailSettings:Username" "<smtp-user>" --project DigitalWallet.API
dotnet user-secrets set "EmailSettings:Password" "<smtp-password>" --project DigitalWallet.API
dotnet user-secrets set "EmailSettings:FromEmail" "<sender-email>" --project DigitalWallet.API
dotnet user-secrets set "EmailVerification:FrontendUrl" "http://localhost:8080" --project DigitalWallet.API
~~~

For Windows-integrated local SQL Server, use your actual instance name with a connection string such as:

~~~text
Server=localhost;Database=DigitalWalletDb;Trusted_Connection=True;TrustServerCertificate=True;
~~~

User secrets are for local development. Other environments can supply environment variables using double underscores, for example ConnectionStrings__DefaultConnection.

Current business settings in [appsettings.json](DigitalWallet.API/appsettings.json):

~~~json
{
  "WalletLimits": {
    "DailyOutgoingLimit": 100000
  },
  "TransferConfirmation": {
    "ExpirySeconds": 120,
    "MaxAttempts": 3,
    "ResendCooldownSeconds": 60
  }
}
~~~

Verification emails link to the configured frontend's /verify-email page. That page submits the email and token to the verification endpoint.

### 4. Apply migrations

~~~powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project DigitalWallet.Infrastructure --startup-project DigitalWallet.API
~~~

This updates the database selected by the active connection string. Migrations create tables, indexes, concurrency columns, and reporting stored procedures.

The [SQL files](DigitalWallet.Infrastructure/sql/) are reference copies; migrations contain the SQL used for deployment. Editing a standalone SQL file does not update the database. Procedure changes require a new migration or explicit deployment script.

OTP coordination uses sys.sp_getapplock; the database account must be able to acquire transaction-owned application locks.

### 5. Build and run

~~~powershell
dotnet build DigitalWallet.slnx
dotnet dev-certs https --trust
dotnet run --project DigitalWallet.API --launch-profile https
~~~

Current bindings in [Program.cs](DigitalWallet.API/Program.cs):

- API: https://localhost:7123
- HTTP: http://localhost:5123, with HTTPS redirection
- GET /: simple application status response, not a database/SMTP health check

CORS allows http://localhost:8080 and http://localhost:8081 and exposes Retry-After. Other browser origins require a policy change.

Swagger UI and an OpenAPI document endpoint are not currently enabled.

## API endpoints

Except for authentication routes and GET /, these endpoints require:

~~~http
Authorization: Bearer <access-token>
~~~

Ownership comes from the token; clients do not submit a user ID.

### Authentication

| Method | Route | Purpose |
| --- | --- | --- |
| POST | /api/auth/register | Register and send verification email |
| POST | /api/auth/verify-email | Verify using email and token |
| POST | /api/auth/resend-verification | Request another verification link |
| POST | /api/auth/login | Log in after verification and receive JWT |

### Wallet

| Method | Route | Purpose |
| --- | --- | --- |
| POST | /api/wallet | Create wallet |
| GET | /api/wallet | Read wallet and balance |
| POST | /api/wallet/deposit | Deposit money |
| POST | /api/wallet/withdraw | Withdraw money |
| POST | /api/wallet/transfer | Save proposed transfer and request OTP |
| POST | /api/wallet/transfer/confirm | Verify OTP and execute saved transfer |
| POST | /api/wallet/transfer/confirmations/{confirmationId}/resend | Replace OTP without resetting attempts |

### Profile, history, and beneficiaries

| Method | Route | Purpose |
| --- | --- | --- |
| GET | /api/user/profile | Read profile |
| PUT | /api/user/profile | Update full name and phone number |
| PUT | /api/user/password | Change password |
| GET | /api/transactions | Paginated transaction history |
| GET | /api/transactions/{id} | Read an owned transaction |
| GET | /api/beneficiaries | List saved recipients |
| POST | /api/beneficiaries | Add recipient wallet and nickname |
| DELETE | /api/beneficiaries/{id} | Delete saved recipient |

History example:

~~~text
/api/transactions?type=Transfer&from=2026-10-01&to=2026-10-31&page=1&pageSize=10
~~~

History defaults to page 1 and page size 10, with newest records first. Its date filter compares stored UTC timestamps directly and uses an inclusive upper bound of to + one day. Unlike reporting, it does not convert Pakistan dates to UTC. Pagination bounds are not currently capped or validated.

### Reports

| Method | Route example | Purpose |
| --- | --- | --- |
| GET | /api/reports/spending-by-type?from=2026-10-01&to=2026-10-31 | Spending grouped by type |
| GET | /api/reports/daily-volume?from=2026-10-01&to=2026-10-31 | Daily incoming/outgoing activity |
| GET | /api/reports/monthly-statement?year=2026&month=10 | Monthly balances and transactions |

Range reports require both dates; statements require year and month. Send YYYY-MM-DD dates without browser timezone conversion. Same-day ranges are supported. No activity returns empty rows; statements carry forward historical balances when applicable.

## Example: transfer with email confirmation

### Request OTP

~~~http
POST /api/wallet/transfer
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "receiverWalletNumber": "W-12345678",
  "amount": 500,
  "description": "Lunch",
  "idempotencyKey": "a-new-key-for-this-payment"
}
~~~

Example response:

~~~json
{
  "success": true,
  "message": "Confirmation request retrieved",
  "data": {
    "confirmationId": 12,
    "expiresAtUtc": "2026-10-28T08:02:00Z",
    "resendAvailableAtUtc": "2026-10-28T08:01:00Z",
    "attemptsRemaining": 3,
    "transactionId": null
  },
  "errors": null
}
~~~

Repeating the same request/key returns metadata without sending another email or renewing expiry. A non-null transactionId indicates an already completed request.

### Confirm

~~~http
POST /api/wallet/transfer/confirm
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "confirmationId": 12,
  "otp": "038291"
}
~~~

Keep OTP as a string to preserve leading zeroes. Successful confirmation returns the completed transaction in data. Only then show payment success and refresh balance/history.

### Resend

~~~http
POST /api/wallet/transfer/confirmations/12/resend
Authorization: Bearer <access-token>
~~~

Wait for the cooldown. Only the new code works; failed attempts remain unchanged. Completed or attempt-exhausted requests cannot be resent.

## Idempotency, concurrency, and failures

- Deposits, withdrawals, and transfer requests require an idempotencyKey of 1–64 characters.
- Generate one key per intended operation; preserve the key and unchanged body after a timeout.
- Changed inputs using an existing key return 409.
- Deposits/withdrawals replay the original transaction. Transfer requests replay metadata; completed confirmations replay the payment.
- Wallet rowversion conflicts roll back the operation and return 409. Retry transient conflicts with the same intent, not a fresh key.
- OTP operations acquire a per-user SQL application lock inside the transaction. It coordinates OTP operations; wallet concurrency tokens protect balance updates.
- Balances, both transfer entries, and confirmation completion share one transaction.
- Incorrect OTP attempts are committed even though the API returns an error.
- Email is sent after committing the request. Delivery failure does not erase the request or move money.
- For uncertain payment outcomes, retry the original confirmation ID rather than starting another payment.

## Responses and errors

Controller successes and current exception middleware use a camelCase envelope:

~~~json
{
  "success": true,
  "message": "Success",
  "data": {},
  "errors": null
}
~~~

Application errors use the same wrapper with success false, data null, and an errors array. Automatic DTO validation uses ASP.NET validation Problem Details with errors keyed by field. Authentication failures may have an empty body.

| Status | Typical meaning |
| --- | --- |
| 400 | Invalid input, wallet rule, daily limit, wrong/expired OTP, attempt limit |
| 401 / 403 | Authentication or access failure |
| 404 | Missing resource or inaccessible confirmation/transaction |
| 409 | Changed request using the same key, concurrency conflict, completed resend |
| 422 | Insufficient funds |
| 429 | OTP send cooldown; Retry-After gives seconds to wait |
| 503 | Confirmation saved but email delivery could not be confirmed |
| 500 | Unexpected server error |

On OTP delivery failure, repeat the original request/key to recover its ID, then resend after cooldown. A changed-input 409 requires correcting the request, not automatic retries.

## Database overview

| Table | Main contents |
| --- | --- |
| Users | Identity/profile, password hash, verification-token hash and timestamps |
| Wallets | Owner, wallet number, balance, currency, status, rowversion |
| Transactions | Amount, type, direction, status, wallet links, reference, idempotency key, request hash |
| Beneficiaries | Owner, nickname, saved wallet number |
| AuditLogs | Actor, entity, action, old/new JSON, UTC timestamp |
| TransferConfirmation | Transfer details, OTP hash, expiry, attempts, completion/transaction link, rowversion |

Uniqueness constraints include email, wallet number, beneficiary owner/wallet pair, transaction wallet/idempotency-key pair, and confirmation user/idempotency-key pair.

Transfer entries share a reference; reference uniqueness is scoped to a wallet. Legacy transfers with unknown direction cannot reliably be reconstructed from descriptions and are excluded from reports requiring Incoming/Outgoing directions.

## Current boundaries and next work

- Learning API with simulated funding: no bank integration, settlement, or reconciliation.
- Verification and OTP emails exist; payment-success notifications are still planned.
- Transaction receipt downloads and downloadable statements are still planned.
- No refresh-token, password-recovery, or server-side logout/revocation flow.
- Password changes do not revoke existing JWTs.
- No reversal/refund endpoint, despite status values available in the domain.
- Wallet status is checked, but no public freeze/unfreeze management endpoint exists.
- No audit UI, automated notification worker, or frontend application is included.
- This checkout has no automated test project. Verify SQL concurrency, SMTP delivery, and full API flows against the configured environment.

## Author

**Ali Raza Khan** — Backend Software Engineer

GitHub: [@AliYusufzai](https://github.com/AliYusufzai)
