# Backend API Updates — Ledger, Payments, Credit/Debit Notes & Account Statements

This document describes all backend changes made to support financial ledger tracking. Use this to implement the corresponding frontend pages, forms, and integrations.

**Base URL:** `http://localhost:5089` (dev)

**Auth:** All endpoints require JWT Bearer token in `Authorization` header. All endpoints require specific permissions (listed per endpoint).

**Standard Response Wrapper:**
```json
{
  "success": true,
  "message": "optional message",
  "data": { ... },
  "errors": null
}
```

**Standard Paginated Response:**
```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "totalCount": 50,
    "page": 1,
    "pageSize": 20
  }
}
```

---

## Table of Contents

1. [Changes to Existing Modules](#1-changes-to-existing-modules)
2. [Ledger Module (NEW)](#2-ledger-module)
3. [Payments Module (NEW)](#3-payments-module)
4. [Credit/Debit Notes Module (NEW)](#4-creditdebit-notes-module)
5. [Accounts Module (NEW)](#5-accounts-module)
6. [Enums Reference](#6-enums-reference)
7. [Permissions Reference](#7-permissions-reference)
8. [Business Logic Notes](#8-business-logic-notes)

---

## 1. Changes to Existing Modules

### 1.1 Customers — New Fields

The Customer model now includes credit management fields. Update all customer forms and displays.

**New fields added to `CustomerDto` response:**
| Field | Type | Description |
|---|---|---|
| `creditDays` | `int` | Number of days before payment is due (0 = no credit) |
| `creditLimit` | `decimal` | Maximum outstanding credit balance allowed (0 = no limit) |

**New fields added to `CreateCustomerRequest`:**
| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `creditDays` | `int` | No | `0` | Credit period in days |
| `creditLimit` | `decimal` | No | `0` | Max credit limit. 0 means no limit enforcement |

**New fields added to `UpdateCustomerRequest`:**
| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `creditDays` | `int` | No | `0` | Credit period in days |
| `creditLimit` | `decimal` | No | `0` | Max credit limit |

**Frontend TODO:**
- Add `creditDays` (number input) and `creditLimit` (currency input) to Customer create/edit forms
- Display these fields on Customer detail/list views
- Consider showing credit utilization (current balance vs limit) — use the Account Statement endpoint

---

### 1.2 Suppliers — New Fields

**New field added to `SupplierDto` response:**
| Field | Type | Description |
|---|---|---|
| `paymentTermDays` | `int` | Default payment terms in days |

**New field added to `CreateSupplierRequest`:**
| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `paymentTermDays` | `int` | No | `0` | Default payment term days for POs from this supplier |

**New field added to `UpdateSupplierRequest`:**
| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `paymentTermDays` | `int` | No | `0` | Default payment term days |

**Frontend TODO:**
- Add `paymentTermDays` (number input) to Supplier create/edit forms
- Display on Supplier detail view

---

### 1.3 Sales — New Fields & Behavior

**New field added to `SaleDto` response:**
| Field | Type | Description |
|---|---|---|
| `dueDate` | `datetime?` | Payment due date (only set for credit sales) |

**No changes to `CreateSaleRequest`** — the backend automatically sets `dueDate` when:
- `paymentMethod` = `3` (Credit)
- `customerId` is provided
- DueDate = sale date + customer's `creditDays` (defaults to 30 if creditDays = 0)

**New backend behavior on sale creation (PaymentMethod = Credit):**
- Validates customer's credit limit. If `creditLimit > 0` and `currentBalance + saleAmount > creditLimit`, the API returns a **validation error**: `"Credit limit exceeded. Limit: X, Current balance: Y, Sale amount: Z"`
- Auto-creates a ledger entry (debit to CustomerReceivable)

**New backend behavior on sale cancellation:**
- Auto-reverses any ledger entries created for that sale

**Frontend TODO:**
- Display `dueDate` on sale detail view (only when present)
- Handle the credit limit exceeded validation error gracefully in the create sale form
- Consider showing a warning before creating a credit sale if the customer is near their limit

---

### 1.4 Purchase Orders — New Fields & Behavior

**New fields added to `PurchaseOrderDto` response:**
| Field | Type | Description |
|---|---|---|
| `paymentTermDays` | `int` | Payment terms for this PO |
| `dueDate` | `datetime?` | Payment due date (set when fully received) |

**New field added to `CreatePurchaseOrderRequest`:**
| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `paymentTermDays` | `int?` | No | Supplier's default | Override payment terms for this PO. If null, uses supplier's `paymentTermDays` |

**New backend behavior on PO receive (when fully received):**
- Sets `dueDate` = receive date + `paymentTermDays` (defaults to 30 if 0)
- Auto-creates a ledger entry (credit to SupplierPayable)

**Frontend TODO:**
- Add optional `paymentTermDays` field to Create PO form (show supplier's default as placeholder)
- Display `paymentTermDays` and `dueDate` on PO detail view

---

## 2. Ledger Module

The ledger is the core financial tracking system. Most entries are auto-created by sales, purchases, payments, and credit/debit notes. Manual entries are also supported.

### 2.1 GET `/api/ledger` — List Ledger Entries
**Permission:** `Ledger:View`

**Query Parameters:**
| Param | Type | Required | Default | Description |
|---|---|---|---|---|
| `page` | `int` | No | `1` | Page number |
| `pageSize` | `int` | No | `20` | Items per page (max 100) |
| `customerId` | `int` | No | — | Filter by customer |
| `supplierId` | `int` | No | — | Filter by supplier |

**Response: `PagedResult<LedgerEntryDto>`**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "entryDate": "2026-02-26T10:00:00Z",
        "accountType": "CustomerReceivable",
        "customerId": 5,
        "customerName": "John Doe",
        "supplierId": null,
        "supplierName": null,
        "referenceType": "Sale",
        "referenceId": 42,
        "description": "Credit sale SL-20260226-A1B2C3",
        "debitAmount": 1500.00,
        "creditAmount": 0.00,
        "isReversed": false,
        "createdAt": "2026-02-26T10:00:00Z"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  }
}
```

**`accountType` values:** `"CustomerReceivable"`, `"SupplierPayable"`

**`referenceType` values:** `"Sale"`, `"PurchaseOrder"`, `"Payment"`, `"CreditNote"`, `"DebitNote"`, `"Manual"`

---

### 2.2 GET `/api/ledger/{id}` — Get Ledger Entry by ID
**Permission:** `Ledger:View`

**Response:** Same `LedgerEntryDto` as above (single object, not paginated).

---

### 2.3 POST `/api/ledger/manual` — Create Manual Ledger Entry
**Permission:** `Ledger:Create`

**Request Body:**
```json
{
  "accountType": 0,
  "customerId": 5,
  "supplierId": null,
  "description": "Opening balance adjustment",
  "debitAmount": 500.00,
  "creditAmount": 0.00
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| `accountType` | `int` | **Yes** | `0` = CustomerReceivable, `1` = SupplierPayable |
| `customerId` | `int?` | **Conditional** | Required when `accountType = 0` |
| `supplierId` | `int?` | **Conditional** | Required when `accountType = 1` |
| `description` | `string` | **Yes** | Cannot be empty |
| `debitAmount` | `decimal` | **Yes** | >= 0. At least one of debit/credit must be > 0 |
| `creditAmount` | `decimal` | **Yes** | >= 0. At least one of debit/credit must be > 0 |

**Success Response:** `201 Created`
```json
{
  "success": true,
  "data": 15,
  "message": "Ledger entry created."
}
```

---

## 3. Payments Module

Payments record money received from customers or paid to suppliers. Each payment auto-creates a corresponding ledger entry.

### 3.1 GET `/api/payments` — List Payments
**Permission:** `Payments:View`

**Query Parameters:**
| Param | Type | Required | Default | Description |
|---|---|---|---|---|
| `page` | `int` | No | `1` | Page number |
| `pageSize` | `int` | No | `20` | Items per page (max 100) |
| `customerId` | `int` | No | — | Filter by customer |
| `supplierId` | `int` | No | — | Filter by supplier |

**Response: `PagedResult<PaymentDto>`**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "receiptNumber": "RCP-20260226-A1B2C3",
        "paymentType": "CustomerReceipt",
        "customerId": 5,
        "customerName": "John Doe",
        "supplierId": null,
        "supplierName": null,
        "paymentDate": "2026-02-26T10:00:00Z",
        "amount": 500.00,
        "paymentMethod": "Cash",
        "chequeNumber": null,
        "bankName": null,
        "chequeDate": null,
        "bankAccountReference": null,
        "notes": "Partial payment for invoice",
        "createdAt": "2026-02-26T10:00:00Z"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  }
}
```

**`paymentType` values:** `"CustomerReceipt"`, `"SupplierPayment"`

**`paymentMethod` values:** `"Cash"`, `"Cheque"`, `"BankTransfer"`

---

### 3.2 GET `/api/payments/{id}` — Get Payment by ID
**Permission:** `Payments:View`

**Response:** Single `PaymentDto`.

---

### 3.3 POST `/api/payments/customer` — Record Customer Payment
**Permission:** `Payments:Create`

**Request Body:**
```json
{
  "customerId": 5,
  "amount": 500.00,
  "paymentMethod": 0,
  "chequeNumber": null,
  "bankName": null,
  "chequeDate": null,
  "bankAccountReference": null,
  "notes": "Partial payment"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| `customerId` | `int` | **Yes** | Must be a valid customer |
| `amount` | `decimal` | **Yes** | Must be > 0 |
| `paymentMethod` | `int` | **Yes** | `0` = Cash, `1` = Cheque, `2` = BankTransfer |
| `chequeNumber` | `string?` | **Conditional** | **Required** when paymentMethod = 1 (Cheque) |
| `bankName` | `string?` | **Conditional** | **Required** when paymentMethod = 1 (Cheque) |
| `chequeDate` | `datetime?` | **Conditional** | **Required** when paymentMethod = 1 (Cheque) |
| `bankAccountReference` | `string?` | **Conditional** | **Required** when paymentMethod = 2 (BankTransfer) |
| `notes` | `string?` | No | — |

**Success Response:** `201 Created`
```json
{
  "success": true,
  "data": 1,
  "message": "Customer payment recorded."
}
```

**Automatic side effects:**
- Creates a ledger entry: Credit to CustomerReceivable (reduces customer's outstanding balance)

---

### 3.4 POST `/api/payments/supplier` — Record Supplier Payment
**Permission:** `Payments:Create`

**Request Body:**
```json
{
  "supplierId": 3,
  "amount": 2000.00,
  "paymentMethod": 2,
  "chequeNumber": null,
  "bankName": null,
  "chequeDate": null,
  "bankAccountReference": "TRX-9876543",
  "notes": "PO payment"
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| `supplierId` | `int` | **Yes** | Must be a valid supplier |
| `amount` | `decimal` | **Yes** | Must be > 0 |
| `paymentMethod` | `int` | **Yes** | `0` = Cash, `1` = Cheque, `2` = BankTransfer |
| `chequeNumber` | `string?` | **Conditional** | **Required** when paymentMethod = 1 |
| `bankName` | `string?` | **Conditional** | **Required** when paymentMethod = 1 |
| `chequeDate` | `datetime?` | **Conditional** | **Required** when paymentMethod = 1 |
| `bankAccountReference` | `string?` | **Conditional** | **Required** when paymentMethod = 2 |
| `notes` | `string?` | No | — |

**Success Response:** `201 Created`
```json
{
  "success": true,
  "data": 2,
  "message": "Supplier payment recorded."
}
```

**Automatic side effects:**
- Creates a ledger entry: Debit to SupplierPayable (reduces what we owe the supplier)

---

### 3.5 DELETE `/api/payments/{id}` — Void Payment
**Permission:** `Payments:Delete`

**Response:**
```json
{
  "success": true,
  "message": "Payment voided."
}
```

**Automatic side effects:**
- Reverses all associated ledger entries (creates reversal entries, marks originals as reversed)
- Soft-deletes the payment record

---

### 3.6 GET `/api/payments/{id}/pdf` — Download Payment Receipt PDF
**Permission:** `Payments:View`

**Response:** Binary PDF file download (`application/pdf`)
- Filename: `receipt-{receiptNumber}.pdf`
- Contains: Company header, receipt number, date, customer/supplier name, amount, payment method details, notes

---

## 4. Credit/Debit Notes Module

Credit notes reduce a balance; debit notes increase a balance. They can be issued for both customers and suppliers.

### 4.1 GET `/api/credit-debit-notes` — List Notes
**Permission:** `CreditNotes:View`

**Query Parameters:**
| Param | Type | Required | Default | Description |
|---|---|---|---|---|
| `page` | `int` | No | `1` | Page number |
| `pageSize` | `int` | No | `20` | Items per page (max 100) |
| `customerId` | `int` | No | — | Filter by customer |
| `supplierId` | `int` | No | — | Filter by supplier |

**Response: `PagedResult<CreditDebitNoteDto>`**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "noteNumber": "CN-20260226-A1B2C3",
        "noteType": "CreditNote",
        "accountType": "Customer",
        "customerId": 5,
        "customerName": "John Doe",
        "supplierId": null,
        "supplierName": null,
        "noteDate": "2026-02-26T10:00:00Z",
        "amount": 200.00,
        "reason": "Goods returned - damaged items",
        "saleId": 42,
        "saleNumber": "SL-20260225-X1Y2Z3",
        "purchaseOrderId": null,
        "purchaseOrderNumber": null,
        "createdAt": "2026-02-26T10:00:00Z"
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20
  }
}
```

**`noteType` values:** `"CreditNote"`, `"DebitNote"`

**`accountType` values:** `"Customer"`, `"Supplier"`

---

### 4.2 GET `/api/credit-debit-notes/{id}` — Get Note by ID
**Permission:** `CreditNotes:View`

**Response:** Single `CreditDebitNoteDto`.

---

### 4.3 POST `/api/credit-debit-notes` — Create Credit/Debit Note
**Permission:** `CreditNotes:Create`

**Request Body:**
```json
{
  "noteType": 0,
  "accountType": 0,
  "customerId": 5,
  "supplierId": null,
  "amount": 200.00,
  "reason": "Goods returned - damaged items",
  "saleId": 42,
  "purchaseOrderId": null
}
```

| Field | Type | Required | Validation |
|---|---|---|---|
| `noteType` | `int` | **Yes** | `0` = CreditNote, `1` = DebitNote |
| `accountType` | `int` | **Yes** | `0` = Customer, `1` = Supplier |
| `customerId` | `int?` | **Conditional** | **Required** when accountType = 0 (Customer). Must be valid |
| `supplierId` | `int?` | **Conditional** | **Required** when accountType = 1 (Supplier). Must be valid |
| `amount` | `decimal` | **Yes** | Must be > 0 |
| `reason` | `string` | **Yes** | Cannot be empty |
| `saleId` | `int?` | No | Optional reference to a related sale |
| `purchaseOrderId` | `int?` | No | Optional reference to a related PO |

**Success Response:** `201 Created`
```json
{
  "success": true,
  "data": 1,
  "message": "Note created."
}
```

**Automatic ledger entry logic:**

| noteType | accountType | Ledger Effect | Balance Impact |
|---|---|---|---|
| CreditNote (0) | Customer (0) | Credit to CustomerReceivable | Reduces what customer owes |
| CreditNote (0) | Supplier (1) | Debit to SupplierPayable | Reduces what we owe supplier |
| DebitNote (1) | Customer (0) | Debit to CustomerReceivable | Increases what customer owes |
| DebitNote (1) | Supplier (1) | Credit to SupplierPayable | Increases what we owe supplier |

---

### 4.4 DELETE `/api/credit-debit-notes/{id}` — Void Note
**Permission:** `CreditNotes:Delete`

**Response:**
```json
{
  "success": true,
  "message": "Note voided."
}
```

**Automatic side effects:** Reverses associated ledger entries + soft-deletes the note.

---

### 4.5 GET `/api/credit-debit-notes/{id}/pdf` — Download Note PDF
**Permission:** `CreditNotes:View`

**Response:** Binary PDF file download (`application/pdf`)
- Filename: `note-{noteNumber}.pdf`
- Contains: Company header, note number, type, date, customer/supplier, amount, reason, related sale/PO

---

## 5. Accounts Module

Account statements and aging reports for customers and suppliers.

### 5.1 GET `/api/accounts/customer/{id}/statement` — Customer Statement
**Permission:** `Accounts:View`

**Path Parameters:**
| Param | Type | Description |
|---|---|---|
| `id` | `int` | Customer ID |

**Query Parameters:**
| Param | Type | Required | Default | Description |
|---|---|---|---|---|
| `fromDate` | `datetime` | No | 3 months ago | Start date |
| `toDate` | `datetime` | No | Now | End date |

**Response:**
```json
{
  "success": true,
  "data": {
    "accountId": 5,
    "accountName": "John Doe",
    "accountType": "Customer",
    "fromDate": "2025-12-01T00:00:00Z",
    "toDate": "2026-02-26T23:59:59Z",
    "openingBalance": 1000.00,
    "totalDebits": 3500.00,
    "totalCredits": 2000.00,
    "closingBalance": 2500.00,
    "lines": [
      {
        "id": 10,
        "entryDate": "2025-12-05T10:00:00Z",
        "referenceType": "Sale",
        "referenceId": 42,
        "description": "Credit sale SL-20251205-A1B2C3",
        "debitAmount": 1500.00,
        "creditAmount": 0.00,
        "runningBalance": 2500.00
      },
      {
        "id": 15,
        "entryDate": "2025-12-20T10:00:00Z",
        "referenceType": "Payment",
        "referenceId": 5,
        "description": "Payment received RCP-20251220-X1Y2Z3",
        "debitAmount": 0.00,
        "creditAmount": 1000.00,
        "runningBalance": 1500.00
      }
    ]
  }
}
```

**Balance logic for customers (Receivable):**
- Opening balance = SUM(debits) - SUM(credits) before `fromDate`
- Debits increase balance (customer owes more)
- Credits decrease balance (customer paid / credit note)
- Running balance accumulates line by line

---

### 5.2 GET `/api/accounts/supplier/{id}/statement` — Supplier Statement
**Permission:** `Accounts:View`

Same structure as customer statement. Path param `id` = Supplier ID.

**Balance logic for suppliers (Payable):**
- Opening balance = SUM(credits) - SUM(debits) before `fromDate`
- Credits increase balance (we owe more)
- Debits decrease balance (we paid / credit note)

---

### 5.3 GET `/api/accounts/customer/{id}/statement/pdf` — Customer Statement PDF
**Permission:** `Accounts:Export`

**Query Parameters:** Same `fromDate`, `toDate` as above.

**Response:** Binary PDF file download (`application/pdf`)
- Filename: `customer-statement-{id}.pdf`
- Landscape A4 with: Company header, account name, period, opening balance, transaction table (date, reference, description, debit, credit, running balance), totals, closing balance

---

### 5.4 GET `/api/accounts/supplier/{id}/statement/pdf` — Supplier Statement PDF
**Permission:** `Accounts:Export`

Same as customer statement PDF. Filename: `supplier-statement-{id}.pdf`

---

### 5.5 GET `/api/accounts/aging/receivables` — Receivables Aging Report
**Permission:** `Accounts:View`

**No query parameters.**

**Response:**
```json
{
  "success": true,
  "data": {
    "reportType": "Receivables",
    "asOfDate": "2026-02-26T10:00:00Z",
    "totalCurrent": 5000.00,
    "total1To30": 3000.00,
    "total31To60": 1500.00,
    "total61To90": 800.00,
    "totalOver90": 200.00,
    "grandTotal": 10500.00,
    "details": [
      {
        "accountId": 5,
        "accountName": "John Doe",
        "current": 2000.00,
        "days1To30": 1000.00,
        "days31To60": 500.00,
        "days61To90": 300.00,
        "over90Days": 0.00,
        "total": 3800.00
      },
      {
        "accountId": 8,
        "accountName": "Jane Smith",
        "current": 3000.00,
        "days1To30": 2000.00,
        "days31To60": 1000.00,
        "days61To90": 500.00,
        "over90Days": 200.00,
        "total": 6700.00
      }
    ]
  }
}
```

**Aging buckets:**
- **Current**: 0 days old
- **1-30**: 1 to 30 days old
- **31-60**: 31 to 60 days old
- **61-90**: 61 to 90 days old
- **Over 90**: more than 90 days old

Only customers with a non-zero balance are included.

---

### 5.6 GET `/api/accounts/aging/payables` — Payables Aging Report
**Permission:** `Accounts:View`

Same response structure as receivables. `reportType` = `"Payables"`. Shows outstanding amounts owed to each supplier.

---

## 6. Enums Reference

### PaymentMethod (existing — used in Sales)
| Value | Label |
|---|---|
| `0` | Cash |
| `1` | Card |
| `2` | BankTransfer |
| `3` | Credit |

### PaymentMethodLedger (new — used in Payments module)
| Value | Label |
|---|---|
| `0` | Cash |
| `1` | Cheque |
| `2` | BankTransfer |

### LedgerAccountType
| Value | Label |
|---|---|
| `0` | CustomerReceivable |
| `1` | SupplierPayable |

### PaymentType
| Value | Label |
|---|---|
| `0` | CustomerReceipt |
| `1` | SupplierPayment |

### NoteType
| Value | Label |
|---|---|
| `0` | CreditNote |
| `1` | DebitNote |

### NoteAccountType
| Value | Label |
|---|---|
| `0` | Customer |
| `1` | Supplier |

---

## 7. Permissions Reference

These new permissions must be assigned to roles that need access:

| Permission | Description |
|---|---|
| `Ledger:View` | View ledger entries |
| `Ledger:Create` | Create manual ledger entries |
| `Payments:View` | View payments + download PDF receipts |
| `Payments:Create` | Create customer/supplier payments |
| `Payments:Delete` | Void payments |
| `CreditNotes:View` | View credit/debit notes + download PDF |
| `CreditNotes:Create` | Create credit/debit notes |
| `CreditNotes:Delete` | Void credit/debit notes |
| `Accounts:View` | View account statements + aging reports |
| `Accounts:Export` | Download account statement PDFs |

All permissions are auto-assigned to the **Admin** role.

---

## 8. Business Logic Notes

### Credit Sale Flow
1. Customer is created/updated with `creditDays` and `creditLimit`
2. A sale is created with `paymentMethod = 3` (Credit) and a `customerId`
3. Backend checks: if `creditLimit > 0`, verifies `currentBalance + saleAmount <= creditLimit`
4. If OK: sale is created, `dueDate` is set, a ledger entry (debit) is auto-created
5. If limit exceeded: returns `400` with validation error

### Credit Sale Cancellation
1. Sale is cancelled via `PUT /api/sales/{id}/cancel`
2. Inventory is restored (existing behavior)
3. All ledger entries for that sale are reversed (new behavior)

### Purchase Order Receive Flow
1. PO is created with `paymentTermDays` (or uses supplier default)
2. PO is submitted, then received
3. When **fully received**: `dueDate` is set, a ledger entry (credit to SupplierPayable) is auto-created
4. Partial receives do NOT create ledger entries — only full receipt does

### Payment Recording
1. A payment is recorded for a customer or supplier
2. A ledger entry is auto-created to reduce the outstanding balance
3. Voiding a payment reverses the ledger entry

### Credit/Debit Note Logic
- **Credit Note to Customer**: Reduces what customer owes (e.g., goods returned)
- **Debit Note to Customer**: Increases what customer owes (e.g., additional charge)
- **Credit Note to Supplier**: Reduces what we owe supplier (e.g., defective goods)
- **Debit Note to Supplier**: Increases what we owe supplier (e.g., additional charge)

### PDF Downloads
All PDF endpoints return binary file data. Use `responseType: 'blob'` in axios/fetch and create a download link:
```js
const response = await api.get(`/api/payments/${id}/pdf`, { responseType: 'blob' });
const url = window.URL.createObjectURL(new Blob([response.data]));
const link = document.createElement('a');
link.href = url;
link.setAttribute('download', `receipt.pdf`);
document.body.appendChild(link);
link.click();
link.remove();
```

---

## Suggested Frontend Pages

### New Pages
1. **Ledger** — List/filter ledger entries (read-only for most users, manual entry form for admins)
2. **Payments** — List payments, create customer payment form, create supplier payment form, void button, PDF download
3. **Credit/Debit Notes** — List notes, create note form (with type/account type selector), void button, PDF download
4. **Customer Account Statement** — Statement view with date range picker, running balance table, PDF export button
5. **Supplier Account Statement** — Same as customer but for suppliers
6. **Aging Reports** — Two tabs (Receivables / Payables), table with aging buckets, totals row

### Modified Pages
1. **Customer Form** — Add credit days + credit limit fields
2. **Customer Detail** — Show credit info + link to account statement
3. **Supplier Form** — Add payment term days field
4. **Supplier Detail** — Show payment terms + link to account statement
5. **Sale Detail** — Show due date field (when present)
6. **PO Form** — Add optional payment term days field
7. **PO Detail** — Show payment term days + due date
