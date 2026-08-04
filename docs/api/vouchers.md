# API — Vouchers

New module. Controllers: `VouchersController` (`/api/vouchers`, learner-facing), `AdminVoucherCampaignsController` (`/api/admin/voucher-campaigns`, admin-only). Service: `VoucherService`. Entities: `VoucherCampaign`, `VoucherRedemption` (tables `voucher_campaigns`, `voucher_redemptions`).

## Concept

A `VoucherCampaign` is an admin-created, **platform-funded** discount for `TrainingPackage` purchases.
A learner redeems at most one campaign per booking, tracked as a `VoucherRedemption` with lifecycle:

```
reserved  →  applied   (payment confirmed paid — permanent)
          →  released  (payment cancelled/failed/expired, or reservation timed out — quota given back)
```

`reserved`/`applied` both count toward `MaxUsesTotal`/`MaxUsesPerLearner`, so two learners racing for the
last use cannot both succeed. `applied` and `released` are terminal — a booking's redemption never
moves again, which is what keeps the PayOS webhook and the learner-triggered reconcile endpoint
idempotent even if both fire for the same payment.

## Financial rule (platform-funded — never reduces coach income)

```
OriginalAmount        = TrainingPackage.Price
DiscountAmount         = voucher discount (fixed_amount, or percentage capped by MaxDiscountAmount)
TotalAmount             = max(0, OriginalAmount − DiscountAmount)   ← what the learner pays
PlatformFeeAmount       = OriginalAmount × commissionRate           ← off the ORIGINAL price
CoachReceiveAmount      = OriginalAmount − PlatformFeeAmount        ← never reduced by a voucher
PerSessionCoachAmount   = CoachReceiveAmount / TotalSessions
PlatformNetRevenue      = TotalAmount − CoachReceiveAmount          ← the platform absorbs the discount
```

A **100%-off** voucher (`TotalAmount == 0`) skips PayOS entirely: an internal `Payment` (`method: "voucher"`) is created already `paid`, and the booking activates immediately.

---

## POST /api/vouchers/validate  (learner, read-only preview)

```json
// Request
{ "code": "WELCOME10", "trainingPackageId": "..." }
```
```json
// Response
{
  "isSuccess": true,
  "data": {
    "code": "WELCOME10", "originalAmount": 1000000, "discountAmount": 100000,
    "totalAmount": 900000, "discountType": "percentage", "discountValue": 10, "maxDiscountAmount": 100000
  }
}
```
**Never reserves a seat.** The server re-computes the same quote again, authoritatively, at purchase time.

**Errors**: `404 VOUCHER_NOT_FOUND`, `409 VOUCHER_NOT_ACTIVE`, `409 VOUCHER_NOT_STARTED`, `409 VOUCHER_EXPIRED`, `409 VOUCHER_MIN_ORDER_NOT_MET`, `409 VOUCHER_USAGE_LIMIT_REACHED`, `409 VOUCHER_LEARNER_LIMIT_REACHED`, `409 VOUCHER_BUDGET_EXCEEDED`.

## Purchasing with a voucher

Both purchase endpoints accept an optional `voucherCode`:

```json
// POST /api/bookings/purchase/payos
{ "trainingPackageId": "...", "voucherCode": "WELCOME10" }
```
```json
// Response (PayOS still needed — TotalAmount > 0)
{
  "isSuccess": true,
  "data": {
    "bookingId": "...", "paymentId": "...", "orderCode": 1732000000000,
    "checkoutUrl": "https://pay.payos.vn/...", "status": "pending",
    "paymentStatus": "pending", "paymentRequired": true, "bookingStatus": "pending_payment",
    "expiredAt": "..."
  }
}
```
```json
// Response (100%-off voucher — no PayOS)
{
  "isSuccess": true,
  "data": {
    "bookingId": "...", "paymentId": "...", "orderCode": null, "checkoutUrl": null,
    "status": "paid", "paymentStatus": "paid", "paymentRequired": false,
    "bookingStatus": "active", "expiredAt": null
  }
}
```
`voucherCode: null`/omitted purchases behave exactly as before — fully backward compatible.

The `Booking` response also now includes `originalAmount`, `discountAmount`, `voucherCampaignId`, `voucherCode` alongside the existing `totalAmount`/`platformFeeAmount`/`coachReceiveAmount`/`perSessionCoachAmount`.

**Purchase order of operations** (avoids an orphaned PayOS checkout with no matching DB row): the pending booking + payment + seat reservation + voucher reservation are committed to the database **first**; PayOS is called **after**. If PayOS then fails, the booking is cleanly cancelled and the seat/voucher reservations released in the same request — the caller gets a clean `500 PAYOS_CREATE_PAYMENT_FAILED`, never a dangling pending booking.

---

## Admin campaign management (`/api/admin/voucher-campaigns`, role `admin`)

| Endpoint | Notes |
|---|---|
| `POST /api/admin/voucher-campaigns` | Creates a `draft` campaign. |
| `GET /api/admin/voucher-campaigns` | Filter by `status`/`keyword`, paged. |
| `GET /api/admin/voucher-campaigns/{id}` | Detail. |
| `PUT /api/admin/voucher-campaigns/{id}` | Once the campaign has ANY redemption (reserved/applied/released, ever), `discountType`/`discountValue`/`maxDiscountAmount`/`minOrderAmount` become locked (`409 VOUCHER_CAMPAIGN_HAS_REDEMPTIONS` if you try) — end the campaign and create a new one instead. Name/description/dates/limits/budget remain editable. |
| `PUT .../{id}/activate` \| `/pause` \| `/end` | Status transitions. An `ended` campaign can never be reactivated. |
| `GET .../{id}/redemptions` | Paged redemption history for the campaign. |

```json
// POST /api/admin/voucher-campaigns
{
  "code": "TET2026", "name": "Tết 2026", "discountType": "percentage", "discountValue": 15,
  "maxDiscountAmount": 200000, "minOrderAmount": 500000, "startAt": "2026-02-14T00:00:00Z",
  "endAt": "2026-02-21T00:00:00Z", "maxUsesTotal": 500, "maxUsesPerLearner": 1, "budgetAmount": 50000000
}
```

## Reporting

`GET /api/admin/payments/dashboard`'s `statistics` now separates:
`grossPackageValue` (before discount) · `totalDiscount` · `netCollected` (= `totalRevenue`, after discount) ·
`platformGrossFee` (= `platformRevenue`, commission on the original price) · `platformNetRevenue` (the platform's real profit after funding vouchers: `netCollected − coachRevenue`).

## Background sweep

A `PaymentAndVoucherExpirySweepBackgroundService` runs every 10 minutes: it cancels `pending_payment` bookings whose PayOS link expired without ever being resolved by the webhook/reconcile, releasing their seats and voucher reservation — so a voucher use can never be held indefinitely by an abandoned checkout.
