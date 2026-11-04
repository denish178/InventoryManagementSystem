# Inventory Management System

Web application for a single-location inventory: categories, suppliers, products, stock in, stock out, low-stock alerts, reports, and role-based access.

- Backend: ASP.NET Core Web API, Entity Framework Core, SQL Server, JWT authentication
- Frontend: Angular standalone components
- Roles: Admin, Inventory Staff, Viewer

Stock quantity changes only through stock-in and stock-out. Each movement updates the balance and inserts one history row in the same database transaction. A failed stock-out does not change the balance or the history.

## Prerequisites

- .NET SDK 10
- Node.js 22+ and npm
- Docker Desktop (for the local SQL Server), or an existing SQL Server instance

## 1. Database

From the repository root:

```bash
docker compose up -d
```

This starts SQL Server on `localhost:1433` with the password used by the development example (`Change_me1!`).

Copy the example settings and keep that file local. It is gitignored.

```bash
copy backend\InventoryManagement.Api\appsettings.Development.example.json backend\InventoryManagement.Api\appsettings.Development.json
```

Change `Jwt:Key`, `AdminSeed:Password`, and the SQL password before any shared environment. Do not commit `appsettings.Development.json`.

## 2. API

```bash
cd backend\InventoryManagement.Api
dotnet run --launch-profile http
```

On startup the API applies EF Core migrations, creates the Admin, Inventory Staff, and Viewer roles, and seeds the admin account from `AdminSeed`. When `SeedDemoData` is true it also loads sample categories, suppliers, products, and opening stock movements.

- API: `http://localhost:5145`
- Swagger (Development only): `http://localhost:5145/swagger`
- Health: `http://localhost:5145/health`

Seeded local accounts from the example file:

| Email | Password | Role |
| --- | --- | --- |
| admin@ims.local | Admin123! | Admin |
| staff@ims.local | Staff123! | Inventory Staff |
| viewer@ims.local | Viewer123! | Viewer |

## 3. Angular app

```bash
cd frontend\inventory-management-ui
npm install
npx ng serve
```

Open `http://localhost:4200`. The API base URL is `src/environments/environment.ts` (`apiUrl`).

## Tests

Acceptance tests T-01 through T-12, plus authentication checks, run against a temporary SQLite database. They do not need Docker.

```bash
dotnet test InventoryManagement.slnx
```

## Documentation

- [Requirements summary](docs/requirements.md)
- [API examples](docs/api-examples.md)
- [Entity diagram](docs/er-diagram.png)
- [Demo script](docs/demo-script.md)

## Roles

| Action | Admin | Inventory Staff | Viewer |
| --- | --- | --- | --- |
| View products, stock, reports | Yes | Yes | Yes |
| Stock in / stock out | Yes | Yes | No |
| Create or edit categories, suppliers, products | Yes | No | No |
| Delete master data | Yes | No | No |
| Manage users and roles | Yes | No | No |

Deleting a category, supplier, or product that is still referenced returns `409 Conflict`. Unauthenticated calls return `401`. The wrong role returns `403`.
