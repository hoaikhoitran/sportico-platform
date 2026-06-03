# PayOS Chi/Payout — Root Cause Report

**Verdict: A — Code bug found and fixed** (signature canonical string was not URL-encoded).
Residual risk **B (credential mismatch)** cannot be ruled out without an Azure-side run, but is now
secondary.

---

## 1. PayOS docs verified

- **Endpoint:** `POST /v1/payouts` — "Tạo lệnh chi đơn" (https://payos.vn/docs/api/ → Payouts).
- **Required headers:** `x-client-id`, `x-api-key`, `x-idempotency-key`, `x-signature`,
  `Content-Type: application/json`.
- **Body schema:** `referenceId`, `amount`, `description`, `toBin`, `toAccountNumber`,
  optional `category` (**array**). `toAccountName` is **response-only** (appears in transaction data).
- **Signature formula status: NOT documented in the prose docs.** The authoritative formula is the
  official PayOS payout demo SDK.

### Evidence (verbatim official SDK — saved in this folder)
- `docs/payos-evidence/official-payos-signature.js`  (= payos-payout-demo-nodejs/lib/signature.js)
- `docs/payos-evidence/official-payos-create-payout.js` (= payos-payout-demo-nodejs/index.js)
- Source: https://github.com/payOSHQ/payos-payout-demo-nodejs

> Literal browser screenshots could not be captured in this headless environment. The verbatim
> official source is stronger evidence than a screenshot of the prose docs, which do **not** contain
> the formula. See `README.md` in this folder for the quoted code.

The SDK's `createSignature(checksumKey, payoutData)`:
1. deep-sorts body keys alphabetically;
2. for each value: arrays/objects → `JSON.stringify`; null → `""`;
3. builds `encodeURIComponent(key)=encodeURIComponent(value)` joined by `&`;  ← **URL-ENCODED**
4. HMAC-SHA256(queryString, checksumKey) → hex.
It signs the **entire body** (incl. `category`).

---

## 2. Current code audit

| File | Status |
|------|--------|
| `PayOsPayoutSigner.cs` | **WAS WRONG** → fixed. Old code joined `key=value` with **no URL-encoding** and signed a hardcoded 5-field list. |
| `PayOsPayoutService.cs` | Header placement correct (x-signature header). Body schema correct. Now signs the exact body via the corrected signer. |
| `PayOsCreatePayoutRequest.cs` | Correct — no `signature`/`toAccountName`; `category` nullable. |
| `PayOsPayoutSettings.cs` | Correct — bound from `PayOsPayout` section only. |
| `DependencyInjection.cs` | Correct — credentials from `PayOsPayout` **only** (no `PayOs` fallback). Only `AutoPayoutEnabled`/`PayoutCategory` fall back. |
| `WithdrawalService.cs` | Wallet state machine correct (see §G). |

### Definitely wrong (now fixed)
`description = "SPORTICO WD"` contains a space. PayOS verifies the signature by recomputing it over
`description=SPORTICO%20WD`; our old signer used the raw space `description=SPORTICO WD`.
Different HMAC input → `code=201 "Mã kiểm tra(signature) không hợp lệ"`. **Proven** by a golden-vector
unit test that reproduces the official SDK output byte-for-byte.

---

## 3. Runtime evidence

- **Azure log (latest):** `signatureLocation=header`, body = 5 documented fields,
  `categoryIncluded=False`, `checksumKeyLength=64`, `code=201` invalid signature.
  → schema and placement are correct; only the **signature bytes** were wrong → consistent with the
  missing URL-encoding.
- **code=20 (earlier):** old body carried `signature`+`toAccountName` (schema error). Fixed.
- **Local diagnostic = `code=403 "Địa chỉ IP không được phép truy cập"`** → **INCONCLUSIVE**. The
  signature is never even evaluated on a 403; the IP isn't whitelisted. A 403 must NOT be read as a
  signature pass/fail. (The diagnostic tool now enforces this.)

---

## 4. Credential / config audit

- `PayOsPayoutSettings` ← `PayOsPayout` section **only**. Payout `ClientId/ApiKey/ChecksumKey` are
  **not** inherited from inbound `PayOs__*`. Distinct-by-design. ✅
- Azure App Settings `PayOsPayout__ClientId/ApiKey/ChecksumKey` (double-underscore) are sufficient.
- **Cannot verify without secrets:** whether the operator pasted the *inbound* ChecksumKey into
  `PayOsPayout__ChecksumKey`. `checksumKeyLength=64` is consistent with a real PayOS key but does not
  prove it is the *Chi-channel* key. If `code=201` persists after this fix, this is the next suspect.

---

## 5. Fix plan

**Minimal production change (this PR):**
- `PayOsPayoutSigner`: sign the whole body, deep-sort, `encodeURIComponent` keys+values,
  JSON-stringify array values — byte-identical to the official SDK.
- `PayOsPayoutService.CreatePayoutAsync`: build body first, then sign that exact body.

**Temporary diagnostic (already in repo, fixed here, REMOVE after debugging):**
- `tools/PayOsSignatureDiagnostic`: variant A is now the corrected formula; 403 → INCONCLUSIVE/stop;
  process env overrides `.env`; never logs secrets.

**Rollback:** revert the two `src/` files; no schema/migration/state change involved.

**Real-money safety:** no wallet mutation in the signer/service change. A future `code=00` triggers a
real transfer — the diagnostic warns and must be run with a small amount and the intended account.

---

## 6. Verification

- `dotnet build SporticoApp.Api.sln` → **0 errors**.
- `dotnet test` → **171 passed / 0 failed**, incl. golden-vector test
  `BuildCanonicalString_MatchesOfficialSdkGoldenVector` and
  `BuildCanonicalString_UrlEncodesSpaceInDescription`.

**Runtime test after deploy (from Azure, which is whitelisted):**
1. Approve/retry a withdrawal. 2. Expect pre-send log `signatureScheme=encodeURIComponent`,
   `canonicalStringSafe=...&description=SPORTICO%20WD&...`. 3. Expect `code=00`/processing.
   If still `code=201` → run `tools/PayOsSignatureDiagnostic` **from Azure**; if even variant A is
   201, the ChecksumKey is wrong for this channel → PayOS support.

---

## 7. Final recommendation

**A — Code bug found and fixed.** The canonical string was not URL-encoded; the space in
`"SPORTICO WD"` produced a different HMAC than PayOS expected → `code=201`. The fix reproduces the
official SDK exactly (proven by golden-vector test).

Because the signature was never validated locally (403 IP block), **final confirmation requires one
Azure run**. If `code=201` persists post-deploy, escalate to **B (credential mismatch)** —
verify `PayOsPayout__ChecksumKey` is the Chi-channel key — and only then **D (channel not provisioned)
/ C (support)**. Not enough evidence to claim success purely from local runs; smallest next step is the
single Azure retry above.
