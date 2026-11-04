# Requirements summary

Source: Inventory Management System final project requirements v1.1 (26 October 2026).

Version 1.0 is a single-location inventory. Warehouses, purchase orders, barcode scanning, email alerts, forecasting, and a native mobile app are out of scope.

## Functional coverage

| ID | Feature | Implementation |
| --- | --- | --- |
| FR-01 | Categories | CRUD. Delete returns 409 when products still use the category. |
| FR-02 | Suppliers | CRUD. Name required, email validated when supplied. Delete returns 409 when products still use the supplier. |
| FR-03 | Products | Create, list, view, update. SKU unique (database unique index, case-insensitive collation on SQL Server). Price, quantity, and reorder level cannot be negative. |
| FR-04 | Search and filters | Name/SKU search, category, supplier, low-stock, paging, and sorting on `GET /api/products`. |
| FR-05 | Stock in | Quantity greater than zero. Balance increases and one `StockIn` row is inserted in one transaction. |
| FR-06 | Stock out | Quantity greater than zero and not above available stock. Balance decreases and one `StockOut` row is inserted in one transaction. |
| FR-07 | History | Newest first, filter by product, type, and date range. |
| FR-08 | Low stock | `QuantityInStock <= ReorderLevel`, shown on the product list, dashboard, and low-stock report. |
| FR-09 | Reports | Current inventory (units and value), low stock, and movement totals. Filters live on the API. |
| FR-10 | Errors | 400 validation, 401 unauthenticated, 403 forbidden, 404 missing, 409 conflict, 500 unexpected. |
| FR-11 | Audit | Movement rows are append-only. The UI has no edit or delete for history. |
| FR-12 | Swagger | Enabled in Development at `/swagger`. |
| FR-13 | Authentication | ASP.NET Core Identity password hashing and JWT bearer tokens. |
| FR-14 | Authorization | Admin, Inventory Staff, and Viewer enforced on the API. |

Ordinary product edits do not change `QuantityInStock`. Opening stock is recorded with stock-in so the history stays complete.

## Acceptance tests

`backend/InventoryManagement.Api.Tests` covers T-01 through T-12, including duplicate SKU, insufficient stock, invalid quantity, a missing product, forced rollback, low stock at the reorder level, blocked deletes, and movement filters. Authentication checks cover 401 and 403.
