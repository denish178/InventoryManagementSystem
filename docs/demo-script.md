# Demo script

Use the seeded Development database (`SeedDemoData: true`) and the accounts in the root README. Start the API, then `npx ng serve`, then open `http://localhost:4200`.

1. Sign in as `viewer@ims.local`. Open Dashboard, Products, Stock history, and the three report pages. Confirm Stock in, Stock out, and Users are absent. Sign out.
2. Sign in as `staff@ims.local`. Record a stock-in of 10 for Hex bolt M8 x 20. Open the product and history and show the new quantity and the `StockIn` row. Try a stock-out larger than the balance and show the error. Confirm category and product edit actions are hidden. Sign out.
3. Sign in as `admin@ims.local`. Create a category, a supplier, and a product. Point out that current stock stays 0 until a stock movement. Stock in 5, then stock out 2. Open Inventory, Low stock, and Movements and filter the movement report to that product.
4. Try to delete the category you just used. The API returns a conflict because products still reference it.
5. Open Users and show the three roles. Optional: open `http://localhost:5145/swagger`, authorize with the admin token, and call `GET /api/reports/low-stock`.

Talking point for the transaction: stock-out checks available quantity in the database update. If the history insert fails, the balance update rolls back, so quantity and history stay in sync.
