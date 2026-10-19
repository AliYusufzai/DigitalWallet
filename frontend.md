# Frontend integration guide

Use this guide with the [README](./README.md). The contracts below describe the
current controllers and DTOs; examples have not been verified against a running API.

## Connection and authentication

- Local base URL: `https://localhost:7123`. HTTP on port 5123 redirects to HTTPS.
- Current allowed browser origins: `http://localhost:3000` and `http://localhost:8080`.
- Login through `POST /api/auth/login` with `email` and `password`; read the JWT
  from `data.token` and expiry from `data.expiresAt`.
- All endpoints below require `Authorization: Bearer <token>`.
- Send `Content-Type: application/json` for JSON bodies.
- Do not send user IDs. The backend gets ownership from the token.

Controller successes are HTTP 200 and use:
```json
{ "success": true, "message": "Success", "data": {}, "errors": null }
```
Read the actual result from `data`. List results use an array there.

## Profile screen

| Action | Request | Result in data |
| --- | --- | --- |
| Load profile | GET `/api/user/profile` | Profile object |
| Save profile | PUT `/api/user/profile` | Updated profile object |
| Change password | PUT `/api/user/password` | null; show response message |

Profile object fields: `id`, `fullName`, `email`, `phoneNumber`, `isActive`,
`createdAt`.

Save profile body:
```json
{ "fullName": "Ali Raza", "phoneNumber": "03001234567" }
```
Send both fields, even if only one changed. Name: 3-100 characters. Phone:
required, validated as a phone number. Email cannot be changed by this endpoint.

Change password body:
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword456!",
  "confirmPassword": "NewPassword456!"
}
```
All fields are required. New password: 6-100 characters, different from current,
and equal to confirmation. Show 400 responses as form errors. Success does not
return a new token; the backend does not revoke existing tokens.

## Beneficiaries screen

| Action | Request | Result in data |
| --- | --- | --- |
| List | GET `/api/beneficiaries` | Array of saved recipients |
| Add | POST `/api/beneficiaries` | Created recipient |
| Delete | DELETE `/api/beneficiaries/{id}` | null |

Add body:
```json
{ "nickname": "Ali", "walletNumber": "W-47392810" }
```

Each item contains `id`, `nickname`, `walletNumber`, and `createdAt`.
Nickname is required, maximum 50 characters. Wallet must exist and cannot be
your own or already saved. There is no pagination or edit endpoint.

Delete using the beneficiary's `id`, not its wallet number. Refresh the list
after a successful add/delete. To send money, copy the selected item's
`walletNumber` into the normal transfer form's `receiverWalletNumber`.

## Money operations and idempotency

| Endpoint (POST) | Body fields |
| --- | --- |
| `/api/wallet/deposit` | amount, description (optional), idempotencyKey |
| `/api/wallet/withdraw` | amount, description (optional), idempotencyKey |
| `/api/wallet/transfer` | receiverWalletNumber, amount, description (optional), idempotencyKey |

Amount: JSON number from 1 to 1,000,000 with at most two decimal places.
Description: maximum 200 characters. Idempotency key: nonblank, 1-64 characters.
Send the key in the **JSON body**, not an HTTP header.

Prepare the request once when the user confirms a payment:
```javascript
const pendingPayment = {
  receiverWalletNumber: selectedWalletNumber,
  amount: Number(amountInput),
  description: descriptionInput || null,
  idempotencyKey: crypto.randomUUID()
};

async function submitPayment(token) {
  return fetch("https://localhost:7123/api/wallet/transfer", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json"
    },
    body: JSON.stringify(pendingPayment)
  });
}
```

The calling UI must inspect `response.ok` and handle errors below; fetch does not
throw for HTTP 400/409/500. Disable repeated clicks while the request is pending.

**Keep pendingPayment unchanged for retries.** Generate a new key only for a
new intended payment. Preserve pending request state if retries must survive
navigation or reload, and keep it scoped to the logged-in user.

- Same key and same details: returns the original transaction, without paying again.
- Same key and different amount/recipient/description/operation: 409 conflict.
- Concurrent wallet update: 409; retry with the same key and unchanged body.
- Timeout or uncertain server error: the outcome may be unknown. Reconcile history
  or retry with the same key. Never generate a new key just because a response was lost.
- Do not retry indefinitely. Surface repeated conflicts to the user.
- A reused-key/different-details conflict requires restoring the original payload,
  or explicitly starting a new payment after confirming the previous outcome.

Keys are scoped to the initiating wallet across deposit, withdrawal, and transfer.
The frontend does not generate or send RequestHash or RowVersion; the backend
handles those. After success, refresh `GET /api/wallet` and transaction history.

## Transaction history

```text
GET /api/transactions?type=Transfer&from=2026-10-01&to=2026-10-19&page=1&pageSize=10
GET /api/transactions/123
```

| Parameter | Value |
| --- | --- |
| type | Omit for all, otherwise Deposit, Withdrawal, or Transfer |
| from | Optional date, e.g. 2026-10-01 |
| to | Optional date, e.g. 2026-10-19 |
| page | Positive integer, default 1 |
| pageSize | Positive integer, default 10; choose a modest UI size such as 10 or 20 |

Build query strings using URLSearchParams; omit empty filters. Reset page to 1
when filters change. The backend does not currently enforce pagination bounds.

Date filtering currently means `CreatedAt >= from` and
`CreatedAt <= to + 1 day`, including exactly midnight on the following day.
Use date-only picker values; passing a time still adds a full day. Stored times
are UTC and the endpoint does not translate local-day boundaries for you.

Paged data contains:
```json
{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 10,
  "totalPages": 0,
  "hasNext": false,
  "hasPrevious": false
}
```

A transaction object looks like:
```json
{
  "id": 123,
  "referenceNumber": "TXN-22ed59d86a184b1db73ce9a83ba0d54c",
  "amount": 50,
  "type": "Transfer",
  "direction": "Outgoing",
  "status": "Completed",
  "description": "Lunch",
  "receiverWalletNumber": null,
  "createdAt": "2026-10-19T10:00:00"
}
```

- Incoming: display +amount. Outgoing: display -amount. Amount itself is positive.
- Unknown: legacy entry; show a neutral amount instead of guessing its direction.
- Both sides share a reference but have separate IDs. Use ID for list keys and details.
- Only the logged-in user's entry is accessible; another user's ID returns 404.
- History/detail responses currently leave receiverWalletNumber null.
- There is no server-side direction/reference filter; do not treat filtering one
  loaded page in JavaScript as filtering the complete history.
- SQL datetime2 values may serialize without a timezone suffix even though they
  represent UTC. Interpret these API timestamps as UTC before local display.

## Errors

| Status | Frontend behavior |
| --- | --- |
| 400 | Show validation/business-rule errors; correct input |
| 401 | Request login again; body may be empty |
| 403 | Access denied |
| 404 | Resource missing or transaction not owned |
| 409 | Conflict; distinguish changed-key-payload from a retryable money-operation conflict |
| 422 | Insufficient funds; show message and refresh balance |
| 500 | Server error; do not assume a payment failed before committing |

409 also covers other database uniqueness violations. Do not automatically retry
every endpoint merely because it returns 409.

Current response formats differ:
- Controller responses: `success`, `message`, `data`, `errors` (camelCase).
- Exception middleware: `Success`, `Message`, `Data`, `Errors` (PascalCase).
- Automatic validation: Problem Details with an `errors` object of field-to-message arrays.
- Authentication failure: may have no JSON body.

A small error-reading helper:
```javascript
async function errorMessage(response) {
  const body = await response.json().catch(() => null);
  const errors = body?.errors ?? body?.Errors;
  const details = Array.isArray(errors)
    ? errors
    : errors && typeof errors === "object"
      ? Object.values(errors).flat()
      : [];
  return details.join(" ") || body?.message || body?.Message ||
    body?.title || `Request failed (${response.status})`;
}
```

No frontend API exists for manually supplying rowversions, hashes, or marking a
transaction completed. Those are server responsibilities.
