# Pro Cosmetics System — Backend API Documentation

> **Base URL (Dev):** `http://localhost:5089`  
> **Swagger / OpenAPI:** `http://localhost:5089/openapi` *(Development only)*  
> **SignalR Hub:** `ws://localhost:5089/hubs/notifications`

---

## Table of Contents

1. [Global Conventions](#1-global-conventions)
2. [Authentication](#2-authentication)
3. [User Management](#3-user-management)
4. [Roles & Permissions](#4-roles--permissions)
5. [Categories](#5-categories)
6. [Products](#6-products)
7. [Product Images](#7-product-images)
8. [Inventory](#8-inventory)
9. [Customers](#9-customers)
10. [Suppliers](#10-suppliers)
11. [Purchase Orders](#11-purchase-orders)
12. [Salesmen](#12-salesmen)
13. [Sales](#13-sales)
14. [Deliveries](#14-deliveries)
15. [Delivery Men](#15-delivery-men)
16. [Notifications](#16-notifications)
17. [Reports](#17-reports)
18. [Real-Time (SignalR)](#18-real-time-signalr)
19. [Error Handling](#19-error-handling)
20. [Permissions Reference](#20-permissions-reference)

---

## 1. Global Conventions

### Authentication Header

Every **protected** endpoint requires a Bearer token:

```
Authorization: Bearer <jwt_token>
```

### Standard Response Envelope

All endpoints return this wrapper:

```json
{
  "success": true,
  "message": "Optional message",
  "data": { /* payload */ },
  "errors": null
}
```

On failure:

```json
{
  "success": false,
  "message": "Error description",
  "data": null,
  "errors": {
    "FieldName": ["Validation message"]
  }
}
```

### Pagination

Paginated endpoints accept query params and return `PagedResult<T>`:

| Query Param | Type | Default | Description |
|-------------|------|---------|-------------|
| `page` | int | `1` | Page number (1-based) |
| `pageSize` | int | `20` | Items per page (max `100`) |

**Paginated response shape:**

```json
{
  "items": [],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### Enums (sent as integers in requests, returned as strings in responses)

| Enum | Values |
|------|--------|
| `PaymentMethod` | `0 = Cash`, `1 = Card`, `2 = BankTransfer`, `3 = Credit` |
| `SaleStatus` | `Completed`, `Pending`, `Cancelled`, `Refunded` |
| `PurchaseOrderStatus` | `Draft`, `Submitted`, `PartiallyReceived`, `Received`, `Cancelled` |
| `DeliveryStatus` | `Pending`, `Assigned`, `PickedUp`, `InTransit`, `Delivered`, `Failed` |

---

## 2. Authentication

### 2.1 Login

```
POST /api/auth/login
```

**Auth:** None (public)

**Request Body:**

```json
{
  "email": "admin@example.com",
  "password": "YourPassword123!"
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `email` | string | ✅ | Valid email format |
| `password` | string | ✅ | Min 6 characters |

**Response `data`:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "expiration": "2026-02-27T00:00:00Z",
  "user": {
    "id": 1,
    "fullName": "Admin User",
    "email": "admin@example.com",
    "isActive": true,
    "roles": ["Admin"],
    "permissions": ["Products:View", "Sales:Create"],
    "createdAt": "2026-01-01T00:00:00Z"
  }
}
```

---

### 2.2 Register

```
POST /api/auth/register
```

**Auth:** None (public)

**Request Body:**

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "roleName": "Salesman"
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `fullName` | string | ✅ | Full name |
| `email` | string | ✅ | Must be unique |
| `password` | string | ✅ | |
| `roleName` | string | ❌ | If omitted, no role is assigned |

**Response `data`:** `UserDto` (see [User object](#user-object))

---

### 2.3 Refresh Token

```
POST /api/auth/refresh-token
```

**Auth:** None (public)

**Request Body:**

```json
{
  "token": "eyJhbGciOiJ...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

| Field | Type | Required |
|-------|------|----------|
| `token` | string | ✅ |
| `refreshToken` | string | ✅ |

**Response `data`:** Same as Login response.

---

## 3. User Management

> **Permission required:** `UserManagement:View` / `UserManagement:Edit`

### 3.1 List Users

```
GET /api/users?page=1&pageSize=20&search=john
```

| Query Param | Type | Required | Description |
|-------------|------|----------|-------------|
| `page` | int | ❌ | Default `1` |
| `pageSize` | int | ❌ | Default `20`, max `100` |
| `search` | string | ❌ | Searches name/email |

**Response `data`:** `PagedResult<UserDto>`

---

### 3.2 Get User by ID

```
GET /api/users/{id}
```

**Response `data`:** `UserDto`

#### User Object

```json
{
  "id": 1,
  "fullName": "John Doe",
  "email": "john@example.com",
  "isActive": true,
  "roles": ["Admin"],
  "permissions": ["Products:View", "Sales:Create"],
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### 3.3 Assign Role to User

```
POST /api/users/{id}/assign-role
```

**Permission:** `UserManagement:Edit`

**Request Body:**

```json
{
  "roleName": "Salesman"
}
```

| Field | Type | Required |
|-------|------|----------|
| `roleName` | string | ✅ |

**Response:** Success message only.

---

### 3.4 Toggle User Active Status

```
PUT /api/users/{id}/toggle-active
```

**Permission:** `UserManagement:Edit`

No body required. Toggles `isActive` between `true` / `false`.

**Response:** Success message with action applied.

---

## 4. Roles & Permissions

> **Permission required:** `UserManagement:View` / `UserManagement:Create` / `UserManagement:Edit` / `UserManagement:Delete`

### 4.1 List All Roles

```
GET /api/roles
```

**Response `data`:** `List<RoleDto>`

```json
[
  {
    "id": 1,
    "name": "Admin",
    "description": "Full access",
    "permissions": [
      { "id": 1, "name": "Products:View", "module": "Products", "description": null }
    ]
  }
]
```

---

### 4.2 Get Role by ID

```
GET /api/roles/{id}
```

**Response `data`:** `RoleDto`

---

### 4.3 Create Role

```
POST /api/roles
```

**Permission:** `UserManagement:Create`

**Request Body:**

```json
{
  "name": "Cashier",
  "description": "Can process sales",
  "permissionIds": [1, 2, 5]
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `name` | string | ✅ | Must be unique |
| `description` | string | ❌ | |
| `permissionIds` | int[] | ❌ | IDs from `GET /api/roles/permissions` |

**Response `data`:** `RoleDto` of created role.

---

### 4.4 Update Role

```
PUT /api/roles/{id}
```

**Permission:** `UserManagement:Edit`

**Request Body:**

```json
{
  "description": "Updated description",
  "permissionIds": [1, 3, 7]
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `description` | string | ❌ | |
| `permissionIds` | int[] | ✅ | Full replacement of permissions |

---

### 4.5 Delete Role

```
DELETE /api/roles/{id}
```

**Permission:** `UserManagement:Delete`

---

### 4.6 Get All Permissions

```
GET /api/roles/permissions
```

**Permission:** `UserManagement:View`

**Response `data`:** `List<PermissionDto>`

```json
[
  { "id": 1, "name": "Products:View", "module": "Products", "description": null },
  { "id": 2, "name": "Products:Create", "module": "Products", "description": null }
]
```

---

## 5. Categories

> **Permission required:** `Products:View` / `Products:Create` / `Products:Edit` / `Products:Delete`

### 5.1 List All Categories

```
GET /api/categories
```

**Response `data`:** `List<CategoryDto>`

```json
[
  {
    "id": 1,
    "name": "Skincare",
    "description": "All skincare products",
    "parentCategoryId": null,
    "parentCategoryName": null
  }
]
```

---

### 5.2 Get Category by ID

```
GET /api/categories/{id}
```

**Response `data`:** `CategoryDto`

---

### 5.3 Create Category

```
POST /api/categories
```

**Permission:** `Products:Create`

**Request Body:**

```json
{
  "name": "Moisturizers",
  "description": "Hydrating products",
  "parentCategoryId": 1
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `name` | string | ✅ | |
| `description` | string | ❌ | |
| `parentCategoryId` | int | ❌ | For sub-categories |

**Response `data`:** `int` — created category ID.

---

### 5.4 Update Category

```
PUT /api/categories/{id}
```

**Permission:** `Products:Edit`

**Request Body:** Same as Create.

| Field | Type | Required |
|-------|------|----------|
| `name` | string | ✅ |
| `description` | string | ❌ |
| `parentCategoryId` | int | ❌ |

---

### 5.5 Delete Category

```
DELETE /api/categories/{id}
```

**Permission:** `Products:Delete`

---

## 6. Products

> **Permission required:** `Products:View` / `Products:Create` / `Products:Edit` / `Products:Delete`

### 6.1 List Products

```
GET /api/products?page=1&pageSize=20&search=cream&categoryId=1
```

| Query Param | Type | Required | Description |
|-------------|------|----------|-------------|
| `page` | int | ❌ | Default `1` |
| `pageSize` | int | ❌ | Default `20` |
| `search` | string | ❌ | Searches name/SKU/barcode |
| `categoryId` | int | ❌ | Filter by category |

**Response `data`:** `PagedResult<ProductDto>`

```json
{
  "items": [
    {
      "id": 1,
      "name": "Vitamin C Serum",
      "sku": "SKN-001",
      "barcode": "1234567890123",
      "description": "Brightening serum",
      "categoryId": 1,
      "categoryName": "Skincare",
      "costPrice": 15.00,
      "salePrice": 29.99,
      "reorderLevel": 10,
      "isActive": true,
      "quantityOnHand": 50,
      "createdAt": "2026-01-01T00:00:00Z",
      "images": [
        {
          "id": 1,
          "productId": 1,
          "fileName": "serum.png",
          "url": "/uploads/serum.png",
          "isPrimary": true,
          "sortOrder": 1
        }
      ]
    }
  ],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

---

### 6.2 Get Product by ID

```
GET /api/products/{id}
```

**Response `data`:** `ProductDto`

---

### 6.3 Get Product by Barcode

```
GET /api/products/barcode/{code}
```

**Response `data`:**

```json
{
  "productId": 1,
  "productName": "Vitamin C Serum",
  "sku": "SKN-001",
  "barcode": "1234567890123",
  "salePrice": 29.99,
  "quantityOnHand": 50
}
```

---

### 6.4 Get Barcode Image

```
GET /api/products/{id}/barcode-image
```

**Response:** PNG image file (binary download).

---

### 6.5 Create Product

```
POST /api/products
```

**Permission:** `Products:Create`

**Request Body:**

```json
{
  "name": "Vitamin C Serum",
  "sku": "SKN-001",
  "barcode": "1234567890123",
  "description": "Brightening serum",
  "categoryId": 1,
  "costPrice": 15.00,
  "salePrice": 29.99,
  "reorderLevel": 10
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `name` | string | ✅ | |
| `sku` | string | ❌ | Stock Keeping Unit |
| `barcode` | string | ❌ | |
| `description` | string | ❌ | |
| `categoryId` | int | ❌ | |
| `costPrice` | decimal | ✅ | Must be ≥ 0 |
| `salePrice` | decimal | ✅ | Must be ≥ 0 |
| `reorderLevel` | int | ✅ | Alert threshold for low stock |

**Response `data`:** `int` — created product ID.

---

### 6.6 Update Product

```
PUT /api/products/{id}
```

**Permission:** `Products:Edit`

**Request Body:**

```json
{
  "name": "Vitamin C Serum",
  "sku": "SKN-001",
  "barcode": "1234567890123",
  "description": "Updated description",
  "categoryId": 1,
  "costPrice": 15.00,
  "salePrice": 32.99,
  "reorderLevel": 10,
  "isActive": true
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `name` | string | ✅ | |
| `sku` | string | ❌ | |
| `barcode` | string | ❌ | |
| `description` | string | ❌ | |
| `categoryId` | int | ❌ | |
| `costPrice` | decimal | ✅ | |
| `salePrice` | decimal | ✅ | |
| `reorderLevel` | int | ✅ | |
| `isActive` | bool | ✅ | Default `true` |

---

### 6.7 Delete Product

```
DELETE /api/products/{id}
```

**Permission:** `Products:Delete`

---

## 7. Product Images

> **Permission required:** `Products:View` / `Products:Edit`  
> **Base path:** `/api/products/{productId}/images`

### 7.1 List Images for a Product

```
GET /api/products/{productId}/images
```

**Response `data`:** `List<ProductImageDto>`

```json
[
  {
    "id": 1,
    "productId": 1,
    "fileName": "serum-front.png",
    "url": "/uploads/serum-front.png",
    "isPrimary": true,
    "sortOrder": 1
  }
]
```

---

### 7.2 Upload Image

```
POST /api/products/{productId}/images
Content-Type: multipart/form-data
```

**Permission:** `Products:Edit`

| Form Field | Type | Required |
|------------|------|----------|
| `file` | binary (image) | ✅ |

**Response `data`:** `ProductImageDto`

---

### 7.3 Delete Image

```
DELETE /api/products/{productId}/images/{imageId}
```

**Permission:** `Products:Edit`

---

### 7.4 Set Primary Image

```
PUT /api/products/{productId}/images/{imageId}/primary
```

**Permission:** `Products:Edit`

No body required.

---

## 8. Inventory

> **Permission required:** `Products:View` / `Products:Edit`

### 8.1 List Full Inventory

```
GET /api/inventory
```

**Response `data`:** `List<InventoryDto>`

```json
[
  {
    "productId": 1,
    "productName": "Vitamin C Serum",
    "sku": "SKN-001",
    "quantityOnHand": 50,
    "quantityReserved": 5,
    "availableQuantity": 45,
    "reorderLevel": 10,
    "isLowStock": false,
    "lastRestockedAt": "2026-02-01T00:00:00Z"
  }
]
```

---

### 8.2 Get Low Stock Items

```
GET /api/inventory/low-stock
```

**Response `data`:** `List<InventoryDto>` — only items where `quantityOnHand <= reorderLevel`.

---

### 8.3 Adjust Inventory

```
POST /api/inventory/adjust
```

**Permission:** `Products:Edit`

**Request Body:**

```json
{
  "productId": 1,
  "quantity": 20,
  "notes": "Manual stock correction"
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `productId` | int | ✅ | Must exist |
| `quantity` | int | ✅ | Positive = add, negative = subtract |
| `notes` | string | ❌ | Reason for adjustment |

---

## 9. Customers

> **Permission required:** `Customers:View` / `Customers:Create` / `Customers:Edit` / `Customers:Delete`

### 9.1 List Customers

```
GET /api/customers?page=1&pageSize=20&search=john
```

| Query Param | Type | Required |
|-------------|------|----------|
| `page` | int | ❌ |
| `pageSize` | int | ❌ |
| `search` | string | ❌ | Searches name/email/phone |

**Response `data`:** `PagedResult<CustomerDto>`

```json
{
  "items": [
    {
      "id": 1,
      "fullName": "Sarah Johnson",
      "email": "sarah@example.com",
      "phone": "+1234567890",
      "address": "123 Main St",
      "city": "Cairo",
      "notes": "VIP customer",
      "isActive": true,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

---

### 9.2 Get Customer by ID

```
GET /api/customers/{id}
```

**Response `data`:** `CustomerDto`

---

### 9.3 Create Customer

```
POST /api/customers
```

**Permission:** `Customers:Create`

**Request Body:**

```json
{
  "fullName": "Sarah Johnson",
  "email": "sarah@example.com",
  "phone": "+1234567890",
  "address": "123 Main St",
  "city": "Cairo",
  "notes": "VIP customer"
}
```

| Field | Type | Required |
|-------|------|----------|
| `fullName` | string | ✅ |
| `email` | string | ❌ |
| `phone` | string | ❌ |
| `address` | string | ❌ |
| `city` | string | ❌ |
| `notes` | string | ❌ |

**Response `data`:** `int` — created customer ID.

---

### 9.4 Update Customer

```
PUT /api/customers/{id}
```

**Permission:** `Customers:Edit`

**Request Body:**

```json
{
  "fullName": "Sarah Johnson",
  "email": "sarah@example.com",
  "phone": "+1234567890",
  "address": "456 New St",
  "city": "Alexandria",
  "notes": "Updated notes",
  "isActive": true
}
```

| Field | Type | Required |
|-------|------|----------|
| `fullName` | string | ✅ |
| `email` | string | ❌ |
| `phone` | string | ❌ |
| `address` | string | ❌ |
| `city` | string | ❌ |
| `notes` | string | ❌ |
| `isActive` | bool | ✅ | Default `true` |

---

### 9.5 Delete Customer

```
DELETE /api/customers/{id}
```

**Permission:** `Customers:Delete`

---

## 10. Suppliers

> **Permission required:** `Suppliers:View` / `Suppliers:Create` / `Suppliers:Edit` / `Suppliers:Delete`

### 10.1 List Suppliers

```
GET /api/suppliers?page=1&pageSize=20&search=pharma
```

**Response `data`:** `PagedResult<SupplierDto>`

```json
{
  "items": [
    {
      "id": 1,
      "name": "Pharma Corp",
      "contactPerson": "Ali Hassan",
      "email": "ali@pharmacorp.com",
      "phone": "+201001234567",
      "address": "10 Industrial Zone",
      "notes": null,
      "isActive": true,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

---

### 10.2 Get Supplier by ID

```
GET /api/suppliers/{id}
```

**Response `data`:** `SupplierDto`

---

### 10.3 Create Supplier

```
POST /api/suppliers
```

**Permission:** `Suppliers:Create`

**Request Body:**

```json
{
  "name": "Pharma Corp",
  "contactPerson": "Ali Hassan",
  "email": "ali@pharmacorp.com",
  "phone": "+201001234567",
  "address": "10 Industrial Zone",
  "notes": "Main distributor"
}
```

| Field | Type | Required |
|-------|------|----------|
| `name` | string | ✅ |
| `contactPerson` | string | ❌ |
| `email` | string | ❌ |
| `phone` | string | ❌ |
| `address` | string | ❌ |
| `notes` | string | ❌ |

**Response `data`:** `int` — created supplier ID.

---

### 10.4 Update Supplier

```
PUT /api/suppliers/{id}
```

**Permission:** `Suppliers:Edit`

Same as Create plus:

| Field | Type | Required |
|-------|------|----------|
| `name` | string | ✅ |
| `isActive` | bool | ✅ | Default `true` |

---

### 10.5 Delete Supplier

```
DELETE /api/suppliers/{id}
```

**Permission:** `Suppliers:Delete`

---

## 11. Purchase Orders

> **Permission required:** `Purchases:View` / `Purchases:Create` / `Purchases:Edit`

### 11.1 List Purchase Orders

```
GET /api/purchase-orders?page=1&pageSize=20&supplierId=1
```

| Query Param | Type | Required |
|-------------|------|----------|
| `page` | int | ❌ |
| `pageSize` | int | ❌ |
| `supplierId` | int | ❌ | Filter by supplier |

**Response `data`:** `PagedResult<PurchaseOrderDto>`

```json
{
  "items": [
    {
      "id": 1,
      "supplierId": 1,
      "supplierName": "Pharma Corp",
      "orderNumber": "PO-20260201-001",
      "orderDate": "2026-02-01T00:00:00Z",
      "expectedDeliveryDate": "2026-02-10T00:00:00Z",
      "status": "Draft",
      "totalAmount": 1500.00,
      "notes": null,
      "createdAt": "2026-02-01T00:00:00Z",
      "items": [
        {
          "id": 1,
          "productId": 1,
          "productName": "Vitamin C Serum",
          "quantity": 100,
          "unitPrice": 15.00,
          "totalPrice": 1500.00
        }
      ]
    }
  ]
}
```

---

### 11.2 Get Purchase Order by ID

```
GET /api/purchase-orders/{id}
```

**Response `data`:** `PurchaseOrderDto`

---

### 11.3 Create Purchase Order

```
POST /api/purchase-orders
```

**Permission:** `Purchases:Create`

**Request Body:**

```json
{
  "supplierId": 1,
  "expectedDeliveryDate": "2026-02-10T00:00:00Z",
  "notes": "Urgent order",
  "items": [
    {
      "productId": 1,
      "quantity": 100,
      "unitPrice": 15.00
    }
  ]
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `supplierId` | int | ✅ | |
| `expectedDeliveryDate` | datetime | ❌ | ISO 8601 |
| `notes` | string | ❌ | |
| `items` | array | ✅ | At least 1 item |
| `items[].productId` | int | ✅ | |
| `items[].quantity` | int | ✅ | Must be > 0 |
| `items[].unitPrice` | decimal | ✅ | Must be ≥ 0 |

**Response `data`:** `int` — created order ID.

---

### 11.4 Submit Purchase Order

```
PUT /api/purchase-orders/{id}/submit
```

**Permission:** `Purchases:Edit`

No body required. Changes status from `Draft` → `Submitted`.

---

### 11.5 Receive Purchase Order

```
PUT /api/purchase-orders/{id}/receive
```

**Permission:** `Purchases:Edit`

Inventory is automatically updated for all received items.

**Request Body:**

```json
{
  "items": [
    {
      "productId": 1,
      "quantityReceived": 90
    }
  ]
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `items` | array | ✅ | |
| `items[].productId` | int | ✅ | |
| `items[].quantityReceived` | int | ✅ | Must be > 0 |

Status becomes `Received` (all) or `PartiallyReceived` (partial).

---

### 11.6 Cancel Purchase Order

```
PUT /api/purchase-orders/{id}/cancel
```

**Permission:** `Purchases:Edit`

No body required. Status → `Cancelled`.

---

## 12. Salesmen

> **Permission required:** `Sales:View` / `Sales:Create` / `Sales:Edit` / `Sales:Delete`

### 12.1 List Salesmen

```
GET /api/salesmen?page=1&pageSize=20&search=ahmed
```

**Response `data`:** `PagedResult<SalesmanDto>`

```json
{
  "items": [
    {
      "id": 1,
      "name": "Ahmed Salah",
      "phone": "+201001234567",
      "email": "ahmed@company.com",
      "commissionRate": 5.00,
      "isActive": true,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

---

### 12.2 Get Salesman by ID

```
GET /api/salesmen/{id}
```

**Response `data`:** `SalesmanDto`

---

### 12.3 Create Salesman

```
POST /api/salesmen
```

**Permission:** `Sales:Create`

**Request Body:**

```json
{
  "name": "Ahmed Salah",
  "phone": "+201001234567",
  "email": "ahmed@company.com",
  "commissionRate": 5.00
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `name` | string | ✅ | |
| `phone` | string | ❌ | |
| `email` | string | ❌ | |
| `commissionRate` | decimal | ✅ | Percentage (e.g. `5.0` = 5%) |

**Response `data`:** `int` — created salesman ID.

---

### 12.4 Update Salesman

```
PUT /api/salesmen/{id}
```

**Permission:** `Sales:Edit`

**Request Body:**

```json
{
  "name": "Ahmed Salah",
  "phone": "+201001234567",
  "email": "ahmed@company.com",
  "commissionRate": 6.00,
  "isActive": true
}
```

| Field | Type | Required |
|-------|------|----------|
| `name` | string | ✅ |
| `phone` | string | ❌ |
| `email` | string | ❌ |
| `commissionRate` | decimal | ✅ |
| `isActive` | bool | ✅ |

---

### 12.5 Delete Salesman

```
DELETE /api/salesmen/{id}
```

**Permission:** `Sales:Delete`

---

## 13. Sales

> **Permission required:** `Sales:View` / `Sales:Create` / `Sales:Edit`

### 13.1 List Sales

```
GET /api/sales?page=1&pageSize=20&customerId=1&salesmanId=2
```

| Query Param | Type | Required |
|-------------|------|----------|
| `page` | int | ❌ |
| `pageSize` | int | ❌ |
| `customerId` | int | ❌ |
| `salesmanId` | int | ❌ |

**Response `data`:** `PagedResult<SaleDto>`

```json
{
  "items": [
    {
      "id": 1,
      "saleNumber": "SALE-20260226-001",
      "customerId": 1,
      "customerName": "Sarah Johnson",
      "salesmanId": 1,
      "salesmanName": "Ahmed Salah",
      "saleDate": "2026-02-26T10:00:00Z",
      "subTotal": 89.97,
      "discount": 5.00,
      "tax": 8.50,
      "totalAmount": 93.47,
      "paymentMethod": "Cash",
      "status": "Completed",
      "notes": null,
      "createdAt": "2026-02-26T10:00:00Z",
      "items": [
        {
          "id": 1,
          "productId": 1,
          "productName": "Vitamin C Serum",
          "quantity": 3,
          "unitPrice": 29.99,
          "discount": 0,
          "totalPrice": 89.97
        }
      ]
    }
  ]
}
```

---

### 13.2 Get Sale by ID

```
GET /api/sales/{id}
```

**Response `data`:** `SaleDto`

---

### 13.3 Create Sale

```
POST /api/sales
```

**Permission:** `Sales:Create`

Inventory is automatically decremented on sale creation.

**Request Body:**

```json
{
  "customerId": 1,
  "salesmanId": 1,
  "discount": 5.00,
  "tax": 8.50,
  "paymentMethod": 0,
  "notes": "Walk-in customer",
  "items": [
    {
      "productId": 1,
      "quantity": 3,
      "unitPrice": 29.99,
      "discount": 0
    }
  ]
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `customerId` | int | ❌ | Null for walk-in |
| `salesmanId` | int | ❌ | |
| `discount` | decimal | ✅ | Overall discount amount (default `0`) |
| `tax` | decimal | ✅ | Tax amount (default `0`) |
| `paymentMethod` | int | ✅ | `0=Cash`, `1=Card`, `2=BankTransfer`, `3=Credit` |
| `notes` | string | ❌ | |
| `items` | array | ✅ | At least 1 item |
| `items[].productId` | int | ✅ | |
| `items[].quantity` | int | ✅ | Must be > 0 |
| `items[].unitPrice` | decimal | ✅ | Must be ≥ 0 |
| `items[].discount` | decimal | ✅ | Per-item discount (default `0`) |

**Response `data`:** `int` — created sale ID.

---

### 13.4 Cancel Sale

```
PUT /api/sales/{id}/cancel
```

**Permission:** `Sales:Edit`

No body required. Status → `Cancelled`. Inventory is automatically restored.

---

## 14. Deliveries

> **Permission required:** `Deliveries:View` / `Deliveries:Create` / `Deliveries:Edit`

### 14.1 List Deliveries

```
GET /api/deliveries?page=1&pageSize=20&deliveryManId=1&status=Pending
```

| Query Param | Type | Required | Notes |
|-------------|------|----------|-------|
| `page` | int | ❌ | |
| `pageSize` | int | ❌ | |
| `deliveryManId` | int | ❌ | |
| `status` | string | ❌ | One of the `DeliveryStatus` values |

**Response `data`:** `PagedResult<DeliveryDto>`

```json
{
  "items": [
    {
      "id": 1,
      "saleId": 1,
      "saleNumber": "SALE-20260226-001",
      "deliveryManId": 1,
      "deliveryManName": "Omar Khaled",
      "status": "Pending",
      "assignedAt": null,
      "pickedUpAt": null,
      "deliveredAt": null,
      "deliveryAddress": "456 Client St, Cairo",
      "notes": null,
      "createdAt": "2026-02-26T10:00:00Z"
    }
  ]
}
```

---

### 14.2 Get Delivery by ID

```
GET /api/deliveries/{id}
```

**Response `data`:** `DeliveryDto`

---

### 14.3 Create Delivery

```
POST /api/deliveries
```

**Permission:** `Deliveries:Create`

**Request Body:**

```json
{
  "saleId": 1,
  "deliveryManId": 1,
  "deliveryAddress": "456 Client St, Cairo",
  "notes": "Call before delivery"
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `saleId` | int | ✅ | Must be a completed sale |
| `deliveryManId` | int | ❌ | Can assign later |
| `deliveryAddress` | string | ❌ | |
| `notes` | string | ❌ | |

**Response `data`:** `int` — created delivery ID.

---

### 14.4 Mark as Picked Up

```
PUT /api/deliveries/{id}/pickup
```

**Permission:** `Deliveries:Edit`

Status transitions: `Pending/Assigned` → `PickedUp`

**Request Body:**

```json
{
  "notes": "Package collected from warehouse"
}
```

| Field | Type | Required |
|-------|------|----------|
| `notes` | string | ❌ |

---

### 14.5 Mark as Delivered

```
PUT /api/deliveries/{id}/deliver
```

**Permission:** `Deliveries:Edit`

Status transitions: `PickedUp/InTransit` → `Delivered`

**Request Body:**

```json
{
  "notes": "Left with receptionist"
}
```

| Field | Type | Required |
|-------|------|----------|
| `notes` | string | ❌ |

---

## 15. Delivery Men

> **Permission required:** `Deliveries:View` / `Deliveries:Create` / `Deliveries:Edit` / `Deliveries:Delete`

### 15.1 List Delivery Men

```
GET /api/delivery-men?page=1&pageSize=20&search=omar
```

**Response `data`:** `PagedResult<DeliveryManDto>`

```json
{
  "items": [
    {
      "id": 1,
      "name": "Omar Khaled",
      "phone": "+201001234567",
      "email": "omar@company.com",
      "isAvailable": true,
      "isActive": true,
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

---

### 15.2 Get Delivery Man by ID

```
GET /api/delivery-men/{id}
```

**Response `data`:** `DeliveryManDto`

---

### 15.3 Create Delivery Man

```
POST /api/delivery-men
```

**Permission:** `Deliveries:Create`

**Request Body:**

```json
{
  "name": "Omar Khaled",
  "phone": "+201001234567",
  "email": "omar@company.com"
}
```

| Field | Type | Required |
|-------|------|----------|
| `name` | string | ✅ |
| `phone` | string | ❌ |
| `email` | string | ❌ |

**Response `data`:** `int` — created delivery man ID.

---

### 15.4 Update Delivery Man

```
PUT /api/delivery-men/{id}
```

**Permission:** `Deliveries:Edit`

**Request Body:**

```json
{
  "name": "Omar Khaled",
  "phone": "+201001234567",
  "email": "omar@company.com",
  "isAvailable": false,
  "isActive": true
}
```

| Field | Type | Required |
|-------|------|----------|
| `name` | string | ✅ |
| `phone` | string | ❌ |
| `email` | string | ❌ |
| `isAvailable` | bool | ✅ |
| `isActive` | bool | ✅ |

---

### 15.5 Delete Delivery Man

```
DELETE /api/delivery-men/{id}
```

**Permission:** `Deliveries:Delete`

Soft delete — record is marked inactive, not removed.

---

## 16. Notifications

> **Auth required** (no specific permission — user sees own notifications only)

### 16.1 Get My Notifications

```
GET /api/notifications
```

**Response `data`:** `List<NotificationDto>`

```json
[
  {
    "id": 1,
    "title": "Low Stock Alert",
    "message": "Vitamin C Serum is running low (8 remaining).",
    "isRead": false,
    "createdAt": "2026-02-26T09:00:00Z"
  }
]
```

---

### 16.2 Mark Notification as Read

```
PUT /api/notifications/{id}/read
```

No body required.

---

## 17. Reports

> **Permission required:** `Reports:View`  
> All date params use ISO 8601 format: `2026-01-01T00:00:00Z`

### 17.1 Sales Report

```
GET /api/reports/sales?from=2026-01-01&to=2026-02-28&groupBy=day
```

| Query Param | Type | Required | Notes |
|-------------|------|----------|-------|
| `from` | datetime | ✅ | |
| `to` | datetime | ✅ | |
| `groupBy` | string | ❌ | `day` (default), `week`, `month` |

**Response `data`:**

```json
{
  "items": [
    {
      "period": "2026-01-01",
      "orderCount": 15,
      "revenue": 4500.00,
      "discount": 200.00,
      "netRevenue": 4300.00
    }
  ],
  "totalRevenue": 4500.00,
  "totalOrders": 15,
  "averageOrderValue": 300.00
}
```

---

### 17.2 Top Products Report

```
GET /api/reports/top-products?from=2026-01-01&to=2026-02-28&top=10
```

| Query Param | Type | Required | Notes |
|-------------|------|----------|-------|
| `from` | datetime | ✅ | |
| `to` | datetime | ✅ | |
| `top` | int | ❌ | Default `10` |

**Response `data`:** `List<TopProductDto>`

```json
[
  {
    "productId": 1,
    "productName": "Vitamin C Serum",
    "quantitySold": 250,
    "revenue": 7497.50
  }
]
```

---

### 17.3 Salesman Performance Report

```
GET /api/reports/salesman-performance?from=2026-01-01&to=2026-02-28
```

| Query Param | Type | Required |
|-------------|------|----------|
| `from` | datetime | ✅ |
| `to` | datetime | ✅ |

**Response `data`:** `List<SalesmanPerformanceDto>`

```json
[
  {
    "salesmanId": 1,
    "salesmanName": "Ahmed Salah",
    "totalSales": 45,
    "totalRevenue": 13500.00,
    "commissionRate": 5.00,
    "commissionAmount": 675.00
  }
]
```

---

### 17.4 Inventory Report

```
GET /api/reports/inventory
```

**Response `data`:**

```json
{
  "totalProducts": 120,
  "lowStockCount": 8,
  "outOfStockCount": 2,
  "totalStockValue": 85000.00,
  "items": [
    {
      "productId": 1,
      "productName": "Vitamin C Serum",
      "sku": "SKN-001",
      "quantityOnHand": 50,
      "costPrice": 15.00,
      "stockValue": 750.00,
      "reorderLevel": 10,
      "isLowStock": false
    }
  ]
}
```

---

### 17.5 Purchase Report

```
GET /api/reports/purchases?from=2026-01-01&to=2026-02-28
```

**Response `data`:**

```json
{
  "totalOrders": 12,
  "totalSpent": 18000.00,
  "items": [
    {
      "supplierId": 1,
      "supplierName": "Pharma Corp",
      "orderCount": 5,
      "totalSpent": 9000.00
    }
  ]
}
```

---

### 17.6 Delivery Report

```
GET /api/reports/deliveries?from=2026-01-01&to=2026-02-28
```

**Response `data`:**

```json
{
  "totalDeliveries": 80,
  "deliveredCount": 70,
  "failedCount": 3,
  "pendingCount": 7,
  "successRate": 87.50,
  "averageDeliveryTimeHours": 4.5
}
```

---

### 17.7 Financial Summary

```
GET /api/reports/financial-summary?from=2026-01-01&to=2026-02-28
```

**Response `data`:**

```json
{
  "totalRevenue": 95000.00,
  "totalCosts": 55000.00,
  "grossProfit": 40000.00,
  "profitMargin": 42.10,
  "totalSales": 320,
  "totalPurchases": 18
}
```

---

## 18. Real-Time (SignalR)

### Hub URL

```
ws://localhost:5089/hubs/notifications
```

**Auth:** Pass JWT as query param (for WebSocket connections):

```
ws://localhost:5089/hubs/notifications?access_token=<jwt_token>
```

### Client Events (server → client)

| Event | Payload | Description |
|-------|---------|-------------|
| `ReceiveNotification` | `NotificationDto` | New notification pushed to the user |

### Connection behaviour

- On connect, user is added to group `user_{userId}` automatically.
- On disconnect, user is removed from the group.
- Each user only receives notifications addressed to them.

---

## 19. Error Handling

All errors follow the same response envelope:

### 400 — Validation Error

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": {
    "Email": ["Email is required."],
    "Password": ["Password must be at least 6 characters."]
  }
}
```

### 401 — Unauthorized

```json
{
  "success": false,
  "message": "Unauthorized"
}
```

### 403 — Forbidden (missing permission)

```json
{
  "success": false,
  "message": "Forbidden: missing permission 'Sales:Create'"
}
```

### 404 — Not Found

```json
{
  "success": false,
  "message": "Sale with ID 999 was not found."
}
```

### 500 — Server Error

```json
{
  "success": false,
  "message": "An unexpected error occurred."
}
```

---

## 20. Permissions Reference

Permissions are checked on every protected endpoint. The authenticated user must have the required permission string in their `permissions` list (returned on login).

| Module | Permissions |
|--------|------------|
| **Products** | `Products:View`, `Products:Create`, `Products:Edit`, `Products:Delete` |
| **Customers** | `Customers:View`, `Customers:Create`, `Customers:Edit`, `Customers:Delete` |
| **Sales** | `Sales:View`, `Sales:Create`, `Sales:Edit`, `Sales:Delete` |
| **Deliveries** | `Deliveries:View`, `Deliveries:Create`, `Deliveries:Edit`, `Deliveries:Delete` |
| **Purchases** | `Purchases:View`, `Purchases:Create`, `Purchases:Edit`, `Purchases:Delete` |
| **Suppliers** | `Suppliers:View`, `Suppliers:Create`, `Suppliers:Edit`, `Suppliers:Delete` |
| **Reports** | `Reports:View` |
| **User Management** | `UserManagement:View`, `UserManagement:Create`, `UserManagement:Edit`, `UserManagement:Delete` |

---

*Generated: February 26, 2026 — Pro Cosmetics System v1.0*
