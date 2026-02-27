# Product Combo API Update — Image Field Added

## Endpoint
`GET /api/combos/products?search={query}&limit={number}`

## What Changed
The product combo endpoint now returns the **primary product image path** in the response.

## New Response Shape
```json
{
  "id": 1,
  "name": "Product Name",
  "sku": "SKU-001",
  "salePrice": 29.99,
  "quantityOnHand": 50,
  "imagePath": "/uploads/products/abc123.jpg"
}
```

### New Field
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `imagePath` | `string` | Yes | Relative path to the product's primary image. `null` if no image exists. |

## Frontend Notes
- `imagePath` is relative to the backend base URL (e.g. `http://localhost:5089/uploads/products/abc123.jpg`)
- Products without images will have `imagePath: null` — show a fallback/placeholder in that case
- Use this field wherever the product combo/select component is rendered to display a thumbnail alongside the product name
