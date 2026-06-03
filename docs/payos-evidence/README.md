# PayOS Chi/Payout signature — evidence

## Source of truth

The official PayOS API docs (https://payos.vn/docs/api/ → **Tạo lệnh chi đơn** / `POST /v1/payouts`)
document the **headers** (`x-idempotency-key`, `x-signature`, `x-client-id`, `x-api-key`) and the
**request body** (`referenceId`, `amount`, `description`, `toBin`, `toAccountNumber`, optional
`category` array) — but they do **NOT** publish the exact `x-signature` canonical-string formula.

The authoritative formula therefore comes from the **official PayOS payout demo**:

- Repo: https://github.com/payOSHQ/payos-payout-demo-nodejs
- `lib/signature.js` → [official-payos-signature.js](official-payos-signature.js) (verbatim copy)
- `index.js`         → [official-payos-create-payout.js](official-payos-create-payout.js) (verbatim copy)

(Local literal browser screenshots could not be captured in this headless environment; the verbatim
official source files above are stronger primary evidence than a screenshot of the prose docs, which
do not contain the formula at all.)

## What the official SDK actually does (verbatim, `lib/signature.js`)

```js
function createSignature(secretKey, jsonData) {
  const sortedData = deepSortObj(jsonData, false);          // sort keys alphabetically
  const queryString = Object.keys(sortedData)
    .map((key) => {
      let value = sortedData[key];
      if (Array.isArray(value) || (typeof value === 'object' && value !== null)) {
        value = JSON.stringify(value);                       // arrays/objects -> compact JSON
      }
      if (value === null || value === undefined) value = '';
      return `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`;  // URL-ENCODE both
    })
    .join('&');
  return crypto.createHmac('sha256', secretKey).update(queryString).digest('hex');
}
```

And `index.js` signs the **entire payout body** including `category`:

```js
const payoutData = { referenceId, amount, description, toBin, toAccountNumber, category: ['salary','hoa'] };
const signature  = createSignature(checksumKey, payoutData);   // whole body is signed
```

## The two facts that break our previous implementation

1. **`encodeURIComponent` is applied to every key and value.**
   Our description `"SPORTICO WD"` contains a space. PayOS signs `description=SPORTICO%20WD`;
   our old signer signed `description=SPORTICO WD` (raw space) → different HMAC input →
   `code=201 "Mã kiểm tra(signature) không hợp lệ"`.

2. **The signature covers the whole body, including `category`** (as a JSON-stringified array),
   not a hardcoded 5-field subset. When `category` is configured it must be signed too.

## Canonical string — corrected example

Body `{ referenceId:"R", amount:47600, description:"SPORTICO WD", toBin:"970422", toAccountNumber:"&lt;toAccountNumber&gt;" }`

```
amount=47600&description=SPORTICO%20WD&referenceId=R&toAccountNumber=&lt;toAccountNumber&gt;&toBin=970422
```

With `category:["salary"]` added:

```
amount=47600&category=%5B%22salary%22%5D&description=SPORTICO%20WD&referenceId=R&toAccountNumber=&lt;toAccountNumber&gt;&toBin=970422
```
