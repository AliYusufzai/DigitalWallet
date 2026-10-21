# DigitalWallet frontend integration guide

Source-reviewed on 21 October 2026. This describes the current API, including email verification and the daily outgoing limit. Examples are illustrative; live API and concurrent-request tests have not been run for this handoff.

## Connection and authentication

- Local API: `https://localhost:7123`. HTTP on port 5123 redirects to HTTPS.
- Allowed development origins: `http://localhost:8080` and `http://localhost:8081`.
- Send JSON bodies with `Content-Type: application/json`.
- All routes below except `/api/auth/*` require `Authorization: Bearer <token>`.
- The backend determines the current user from the token. Do not send a user ID or a sender wallet ID.
- Use a trusted local HTTPS certificate for browser requests. Keep the API URL configurable for deployment.

## Responses and errors

Successful controller responses use HTTP 200 and this wrapper:

```json
{
  "success": true,
  "message": "Success",
  "data": {},
  "errors": null
}
```

Read the result from `data`. Actions without a result return `data: null`.

Exception middleware currently uses PascalCase instead:

```json
{
  "Success": false,
  "Message": "Daily outgoing limit of PKR 100,000.00 exceeded.",
  "Data": null,
  "Errors": ["Daily outgoing limit of PKR 100,000.00 exceeded."]
}
```

The example above is HTTP **400**, not 422 or 429. Number formatting in the message can depend on server culture. There is no stable machine-readable daily-limit error code or remaining-allowance value.

| HTTP status | Frontend action |
| --- | --- |
| 400 | Show validation or business-rule error, including the daily limit. Let the user correct the request. |
| 401 | Ask the user to log in again; the response body may be empty. |
| 403 | Show access denied; the response body may be empty. |
| 404 | Show missing resource. Login also currently returns 404 for an unknown email. |
| 409 | A conflict occurred. Distinguish a reused key with different inputs from a retryable concurrent update. |
| 422 | Insufficient funds. Refresh the wallet balance and show the message. |
| 500 / network timeout | Payment outcome may be unknown. Preserve the original request and key for reconciliation/retry. |

Automatic input validation returns Problem Details with an `errors` object containing field-to-message arrays. Support both wrapper casing styles and this validation format:

```javascript
async function readApiError(response) {
  const body = await response.json().catch(() => null);
  const errors = body?.errors ?? body?.Errors;
  const messages = Array.isArray(errors)
    ? errors
    : errors && typeof errors === "object"
      ? Object.values(errors).flat()
      : [];

  return messages.join(" ") || body?.message || body?.Message ||
    body?.title || `Request failed (${response.status})`;
}
```

`fetch` does not throw for HTTP 400/409/500: always check `response.ok`. Display server messages as text, never as HTML.

## Registration, email verification, and login

| Method and route | JSON body | Success result |
| --- | --- | --- |
| POST `/api/auth/register` | `fullName`, `email`, `password`, `phoneNumber` | Generic acknowledgement; no login token. |
| POST `/api/auth/verify-email` | `email`, `token` | Verification confirmation; user can then log in. |
| POST `/api/auth/resend-verification` | `email` | Generic acknowledgement; does not guarantee an email was sent. |
| POST `/api/auth/login` | `email`, `password` | Authentication object in `data`. |

Registration: full name 3–100 characters, valid email, password 6–100 characters, and required valid phone number.

Implement a frontend `/verify-email` page. The email link carries `email` and `token` query parameters. Read them with `URLSearchParams` and send them to the verification POST endpoint; the token is a 64-character string. An invalid, expired, or used link returns 400. Offer resend verification. The server controls resend eligibility; an acknowledgement does not confirm account existence.

Login is blocked until the email is verified. A login result has:

```json
{
  "userId": 1,
  "fullName": "Example User",
  "email": "user@example.com",
  "token": "<JWT>",
  "expiresAt": "2026-10-21T12:00:00Z"
}
```

No refresh-token or server logout endpoint exists. On logout, clear local authentication and user-specific pending UI state. On token expiry, request login again.

## Wallet and money operations

| Method and route | Body | Result in `data` |
| --- | --- | --- |
| GET `/api/wallet` | None | Wallet object |
| POST `/api/wallet` | None | Created wallet; an existing wallet causes 400 |
| POST `/api/wallet/deposit` | `amount`, optional `description`, `idempotencyKey` | Transaction object |
| POST `/api/wallet/withdraw` | `amount`, optional `description`, `idempotencyKey` | Transaction object |
| POST `/api/wallet/transfer` | `receiverWalletNumber`, `amount`, optional `description`, `idempotencyKey` | Transaction object |

Wallet fields: `id`, `walletNumber`, `balance`, `currency`, `status`, `createdAt`. Fetch the wallet after login; if absent, offer creation.

Amounts are JSON numbers from 1 to 1,000,000 with at most two decimal places. Descriptions have a maximum of 200 characters. Idempotency keys must be nonblank and at most 64 characters. Transfers require an existing different wallet with matching currency; both wallets must be active.

### Daily outgoing limit

- Current configured allowance: **PKR 100,000 per wallet per Pakistan calendar day**.
- Completed withdrawals and outgoing transfers share one allowance.
- Deposits and incoming transfers do not consume it.
- Exactly 100,000 is allowed; a request taking the total above it returns 400.
- The day resets at midnight in `Asia/Karachi`, not the browser's timezone and not 24 hours after the last payment.
- A replay of an already-completed request returns its original result without consuming allowance again.
- Insufficient balance is checked before the daily limit, so an operation violating both can return 422 first.

Suggested form text: “Withdrawals and transfers share a PKR 100,000 daily limit. Resets at midnight Pakistan time.” The amount is configuration-controlled; coordinate frontend copy if it changes.

**There is no limit-summary endpoint and no response field for spent today, remaining allowance, or reset time.** Do not display a supposedly authoritative remaining balance computed from one history page. Show the rule and handle the server rejection. A remaining-allowance widget requires a backend contract addition.

### Idempotency and retries

Create one key for each new intended money operation. Preserve the exact request body and key for retries:

```javascript
const pendingTransfer = {
  receiverWalletNumber: "W-47392810",
  amount: 25000,
  description: "Payment",
  idempotencyKey: crypto.randomUUID()
};

async function submitTransfer(apiBase, token, pendingTransfer) {
  const response = await fetch(`${apiBase}/api/wallet/transfer`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`
    },
    body: JSON.stringify(pendingTransfer)
  });

  if (!response.ok) throw new Error(await readApiError(response));
  return (await response.json()).data;
}
```

Keep `pendingTransfer` outside the submit handler so repeated clicks/retries do not generate new keys. The caller should catch errors and display them. Disable submission while a request is pending.

- Same key and same inputs: returns the existing transaction.
- Same key and different amount, description, operation, or recipient: 409. Do not blindly retry this conflict.
- Concurrent wallet update: 409; retry the unchanged request with the same key, using bounded retries or a user retry action.
- Timeout/uncertain server failure: retain the original key. A new key could create a second payment.
- Changed payment details represent a new intent. Resolve any uncertain prior payment before submitting it with a new key.
- Keys are scoped to the initiating wallet across all three money operations.

After success, refresh wallet balance and transaction history. The returned transaction amount is always positive; use its direction for display.

## Transaction history

`GET /api/transactions?type=Transfer&from=2026-10-01&to=2026-10-21&page=1&pageSize=10`

`GET /api/transactions/{id}` returns one owned transaction. Another user's transaction returns 404.

| Parameter | Meaning |
| --- | --- |
| `type` | Optional: `Deposit`, `Withdrawal`, or `Transfer` |
| `from`, `to` | Optional dates; see the date-filter limitation below |
| `page` | Defaults to 1; send a positive integer |
| `pageSize` | Defaults to 10; use a modest positive size |

The list's `data` contains `items`, `totalCount`, `page`, `pageSize`, `totalPages`, `hasNext`, and `hasPrevious`. Reset page to 1 when filters change. Pagination bounds are not currently enforced by the backend.

Transaction shape (illustrative):

```json
{
  "id": 123,
  "direction": "Outgoing",
  "referenceNumber": "TXN-example",
  "amount": 25000,
  "type": "Transfer",
  "status": "Completed",
  "description": "Payment",
  "receiverWalletNumber": null,
  "createdAt": "2026-10-21T10:00:00"
}
```

- `Incoming`: show a credit; `Outgoing`: show a debit. For legacy `Unknown`, show a neutral amount.
- History and detail responses currently leave `receiverWalletNumber` null. A transfer success response can include it.
- Transfer entries share a reference number but have different IDs. Use the ID as the UI list key.
- Stored timestamps are UTC, but retrieved SQL timestamps may omit the timezone suffix. Treat offset-less API `createdAt` values as UTC before formatting; preserve existing offsets or `Z`. For Pakistan display, use `Intl.DateTimeFormat` with `timeZone: "Asia/Karachi"`.
- **History filtering is separate from the daily-limit calculation.** It currently applies `CreatedAt >= from` and `CreatedAt <= to + 1 day`, without Pakistan-time conversion. The upper boundary includes the following midnight. Do not treat this endpoint as an exact Pakistan-day allowance query.
- There are no server-side direction or reference-number filters. Filtering the current page is not equivalent to searching the whole history.

## Profile

| Method and route | Body/result |
| --- | --- |
| GET `/api/user/profile` | Returns `id`, `fullName`, `email`, `phoneNumber`, `isActive`, `createdAt` |
| PUT `/api/user/profile` | Send `fullName` and `phoneNumber`; returns updated profile |
| PUT `/api/user/password` | Send `currentPassword`, `newPassword`, `confirmPassword`; returns acknowledgement |

Profile name: 3–100 characters. Phone: required and valid. Email cannot be edited through this endpoint. New password: 6–100 characters, matching confirmation and different from the current password. Password changes do not issue a replacement JWT or revoke existing JWTs.

## Beneficiaries

| Method and route | Body/result |
| --- | --- |
| GET `/api/beneficiaries` | Array of `id`, `nickname`, `walletNumber`, `createdAt` |
| POST `/api/beneficiaries` | Send `nickname` and `walletNumber`; returns saved beneficiary |
| DELETE `/api/beneficiaries/{id}` | Acknowledgement; use the beneficiary ID |

Nickname is required, maximum 50 characters. Wallet number is required. Use the selected beneficiary's `walletNumber` as `receiverWalletNumber` in the normal transfer request. No edit or pagination endpoint exists.

## Frontend acceptance checks

1. Register, open the verification page, verify, then log in. Handle expired links and resend acknowledgements.
2. Fetch/create the wallet and show loading, empty, success, and failure states.
3. With sufficient balance and no prior outgoing spending today, withdraw 30,000 and transfer 70,000. Both should succeed; the next outgoing 1 should return 400.
4. Confirm deposits still work after the outgoing allowance is exhausted.
5. Retry a successful payment with the exact same body/key; it must not duplicate the payment. Change the amount with that key and expect 409.
6. Show daily-limit 400 and insufficient-funds 422 messages correctly, including PascalCase responses and validation Problem Details.
7. Refresh balance/history after success. Do not infer allowance from the currently loaded history page.
8. Display timestamps in the intended timezone and keep the daily-limit reset tied to Pakistan time.

Backend midnight and concurrency verification remains a separate integration test task. Logging through Seq requires no frontend integration. Audit and reporting endpoints for later Phase B work are not available yet.
