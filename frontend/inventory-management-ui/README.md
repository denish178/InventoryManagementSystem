# Inventory Management UI

Angular frontend for the Inventory Management System. Screens live under `src/app/features` (dashboard, products, categories, suppliers, stock, reports, users). API calls go through the services in `src/app/core`, and the API base URL is `src/environments/environment.ts`.

## Run

Start the API first (see the repository root README). Then:

```bash
npm install
npx ng serve
```

Open `http://localhost:4200` and sign in with a seeded account, for example `admin@ims.local` / `Admin123!`.

Stock quantity is not edited on the product form. Use Stock in and Stock out. Timestamps from the API are shown in local time.

## Build

```bash
npx ng build
```
