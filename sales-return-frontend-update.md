# Sales Return (Partial + Full) — Frontend Update Guide

## New API Endpoint

### `PUT /api/sales/{id}/return`
**Permission required:** `Sales:Return`

**Request body:**
```json
{
  "reason": "Customer returned damaged items",  // optional, defaults to "Sales return"
  "items": [
    { "productId": 5, "quantityReturned": 2 },
    { "productId": 12, "quantityReturned": 1 }
  ]
}
```

**Success response:** `200 OK`
```json
{
  "success": true,
  "message": "Sale return processed. Inventory restored."
}
```

**Validation errors (400/422):**
- `"At least one return item is required."` — empty items array
- `"Return quantity must be greater than zero."` — quantityReturned <= 0
- `"Cannot return X of 'Product Name'. Already returned: Y, sold: Z."` — exceeding returnable qty
- `"Only completed sales can have returns."` — sale is not in Completed status
- `"Sale item for product not found."` — productId doesn't match any item in the sale

---

## Updated Response Fields

### `GET /api/sales` and `GET /api/sales/{id}` — New fields

**SaleDto** now includes:
```json
{
  "id": 1,
  "saleNumber": "SL-20260227-ABC123",
  "returnedAmount": 150.00,   // <-- NEW: cumulative total of all returned value
  "status": "Completed",       // Can now be "Refunded" when all items fully returned
  // ... all other existing fields unchanged
  "items": [
    {
      "id": 1,
      "productId": 5,
      "productName": "Face Cream",
      "quantity": 10,
      "quantityReturned": 2,   // <-- NEW: how many units returned so far
      "unitPrice": 25.00,
      "discount": 5.00,
      "totalPrice": 245.00
    }
  ]
}
```

---

## Updated Cancel Behavior

### `PUT /api/sales/{id}/cancel`
**New restriction:** Cancel is now blocked if any return has been made on the sale.

**New error (when returnedAmount > 0):**
```json
{
  "success": false,
  "message": "Cannot cancel a sale with returned items."
}
```

---

## Sale Status Values (updated meaning)

| Status | Meaning |
|--------|---------|
| `Completed` | Active sale (may have partial returns) |
| `Pending` | Awaiting completion |
| `Cancelled` | Fully cancelled (only if no returns were made) |
| `Refunded` | **NEW usage** — All items have been fully returned |

---

## Business Logic Summary (for UI/UX decisions)

1. **Multiple returns allowed** — A sale can have multiple partial returns over time. Each return processes specific items with specific quantities.
2. **Returnable quantity** = `quantity - quantityReturned` per item. If this is 0, that item is fully returned.
3. **Auto credit note** — A credit note is automatically created for sales with a customer. No frontend action needed.
4. **Ledger reversal** — Only happens for credit payment sales. No frontend action needed.
5. **Status transitions:**
   - Partial return → status stays `Completed`
   - All items fully returned → status becomes `Refunded`
   - Once any return is made, cancel is blocked
6. **Permission** — The return action requires `Sales:Return` permission (separate from `Sales:Edit`).

---

## Suggested Frontend Changes

### Sales List Page
- Show `returnedAmount` if > 0 (e.g. badge or column showing "Returned: $150.00")
- Handle `Refunded` status with appropriate styling (e.g. distinct color/tag)

### Sale Detail Page
- Show `quantityReturned` per item alongside `quantity` (e.g. "Sold: 10 | Returned: 2")
- Show `returnedAmount` in the sale summary section
- Add **"Process Return"** button (visible when status is `Completed` and user has `Sales:Return` permission)
- Disable/hide cancel button when `returnedAmount > 0`

### Return Dialog/Form
- Show list of sale items with returnable quantities (`quantity - quantityReturned`)
- For each item, allow entering return quantity (1 to returnable max)
- Only show items that still have returnable quantity > 0
- Optional reason text field
- Confirm action before submitting

### Navigation/Menu
- Ensure `Sales:Return` permission is checked for showing the return action
