# API examples

Base URL: `http://localhost:5145`

Login returns a JWT. Send it as `Authorization: Bearer <token>` on every route except `POST /api/auth/login` and `GET /health`.

## Login

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "admin@ims.local", "password": "Admin123!" }
```

## Category, supplier, product

```http
POST /api/categories
{ "name": "Fasteners", "description": "Bolts and nuts" }

POST /api/suppliers
{ "name": "Northwind Supply", "email": "orders@northwind.example", "phone": "555-0148" }

POST /api/products
{
  "sku": "HEX-M8-20",
  "name": "Hex bolt M8 x 20",
  "unitPrice": 0.42,
  "reorderLevel": 25,
  "categoryId": 1,
  "supplierId": 1
}
```

`POST /api/products` always starts `quantityInStock` at 0. A later `PUT /api/products/{id}` updates name, price, reorder level, category, and supplier only.

List with filters:

```http
GET /api/products?search=hex&categoryId=1&lowStock=true&page=1&pageSize=20&sort=name&direction=asc
```

## Stock

```http
POST /api/stock-transactions/stock-in
{ "productId": 1, "quantity": 10, "referenceNumber": "PO-1001", "notes": "Receipt" }

POST /api/stock-transactions/stock-out
{ "productId": 1, "quantity": 4, "referenceNumber": "SO-2001" }

GET /api/stock-transactions?productId=1&transactionType=StockIn&startDate=2026-11-01&endDate=2026-11-05
```

`transactionType` must be `StockIn` or `StockOut`. Quantity must be a whole number greater than zero. Stock-out above the balance returns `409` and writes nothing.

## Reports

```http
GET /api/reports/inventory?search=bolt
GET /api/reports/low-stock
GET /api/reports/stock-movements?productId=1&transactionType=StockOut&startDate=2026-11-01&endDate=2026-11-05
```

Inventory and movement reports include totals calculated by the API.

## Users (Admin)

```http
POST /api/auth/register
{ "fullName": "Alex Staff", "email": "alex@ims.local", "password": "Staff123!", "role": "Inventory Staff" }

PUT /api/users/{id}/role
{ "role": "Viewer" }
```

Allowed roles: `Admin`, `Inventory Staff`, `Viewer`.
