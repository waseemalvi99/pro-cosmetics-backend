# Barcode API Updates — Frontend Integration Guide

## Overview

Two barcode-related API capabilities have been added/enhanced for use with barcode scanners and barcode label printing:

1. **Barcode Lookup** (enhanced) — search a product by scanning its barcode, now returns product images and cost price
2. **Barcode Labels PDF** (new) — generate a printable PDF of barcode labels for multiple products

All endpoints require JWT authentication and `Products:View` permission.

Base URL: `http://localhost:5089` (dev)

---

## 1. Barcode Scanner Lookup

Used when the user scans a barcode during sale creation, purchase creation, or purchase receiving.

### Endpoint

```
GET /api/products/barcode/{code}
```

### Parameters

| Parameter | Location | Type   | Description                     |
|-----------|----------|--------|---------------------------------|
| `code`    | path     | string | The barcode value to search for |

### Response

```json
{
  "success": true,
  "message": null,
  "data": {
    "productId": 5,
    "productName": "Face Cream SPF 30",
    "sku": "FC-SPF30-001",
    "barcode": "8901234567890",
    "costPrice": 150.00,
    "salePrice": 250.00,
    "quantityOnHand": 42,
    "images": [
      {
        "id": 12,
        "productId": 5,
        "fileName": "face-cream-front.jpg",
        "url": "/uploads/products/face-cream-front.jpg",
        "isPrimary": true,
        "sortOrder": 0
      },
      {
        "id": 13,
        "productId": 5,
        "fileName": "face-cream-back.jpg",
        "url": "/uploads/products/face-cream-back.jpg",
        "isPrimary": false,
        "sortOrder": 1
      }
    ]
  },
  "errors": null
}
```

### Error Response (product not found)

```json
{
  "success": false,
  "message": "Product not found.",
  "data": null,
  "errors": null
}
```

### Frontend Usage Notes

- The barcode scanner will fire a rapid text input followed by Enter. Bind a listener on the barcode input field that triggers the API call on Enter or after detecting a complete barcode string.
- Use `costPrice` when adding items to a **purchase order** or **receiving** screen.
- Use `salePrice` when adding items to a **sale** screen.
- Use `quantityOnHand` to show available stock inline.
- Display the primary image (`isPrimary: true`) as a thumbnail next to the scanned product row. If no images exist, the `images` array will be empty (`[]`).
- The `images[].url` is a relative path — prepend the API base URL to get the full image URL (e.g. `http://localhost:5089/uploads/products/face-cream-front.jpg`).

---

## 2. Print Barcode Labels (PDF)

Used when the user wants to print barcode stickers for products (typically after creating new products).

### Endpoint

```
POST /api/products/barcode-labels
```

### Request Body

```json
{
  "productIds": [1, 2, 3, 5, 10]
}
```

| Field        | Type        | Required | Description                                |
|--------------|-------------|----------|--------------------------------------------|
| `productIds` | `int[]`     | Yes      | List of product IDs to generate labels for |

**Constraints:**
- At least 1 product ID required
- Maximum 100 product IDs per request

### Response

- **Content-Type:** `application/pdf`
- **Content-Disposition:** `attachment; filename=barcode-labels.pdf`
- The response body is a binary PDF file.

### PDF Layout

- **Page size:** A4 portrait
- **Grid:** 3 columns x 8 rows = **24 labels per page**
- **Each label contains:**
  - Product name
  - SKU (if available)
  - Sale price (formatted as `Rs. X,XXX.XX`)
  - CODE_128 barcode image with human-readable text below
- Automatically paginates when more than 24 products are provided

### Frontend Usage Notes

- Send the request and handle the response as a binary blob.
- Either open the PDF in a new browser tab or trigger a file download.
- Example implementation:
  ```javascript
  const response = await api.post('/api/products/barcode-labels',
    { productIds: [1, 2, 3] },
    { responseType: 'blob' }
  );
  const url = URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }));
  window.open(url, '_blank');
  ```
- This endpoint should be accessible from:
  - **Products list page** — a "Print Barcodes" button that works on selected products
  - **Product create/edit page** — a "Print Barcode" button after saving the product

---

## 3. Single Barcode Image (already existed, unchanged)

Returns a barcode as a PNG image for a single product. Useful for inline display.

### Endpoint

```
GET /api/products/{id}/barcode-image
```

### Response

- **Content-Type:** `image/png`
- Binary PNG image of the CODE_128 barcode (300x100 px)

### Frontend Usage Notes

- Can be used as an `<img>` src directly: `<img src="/api/products/5/barcode-image" />`
- Include the JWT token in the request header (use fetch + blob URL, or an authenticated image loader)

---

## Summary of Where to Integrate

| Screen / Feature          | API to Use                          | Key Fields to Use                              |
|---------------------------|-------------------------------------|------------------------------------------------|
| **Sale creation**         | `GET /barcode/{code}`               | productId, productName, salePrice, quantityOnHand, images |
| **Purchase order creation** | `GET /barcode/{code}`             | productId, productName, costPrice, images      |
| **Purchase receiving**    | `GET /barcode/{code}`               | productId, productName, costPrice, quantityOnHand, images |
| **Products list page**    | `POST /barcode-labels`              | Selected product IDs → PDF download            |
| **Product create/edit**   | `POST /barcode-labels`              | Single product ID → PDF download               |
| **Inline barcode display**| `GET /{id}/barcode-image`           | PNG image for display                          |
