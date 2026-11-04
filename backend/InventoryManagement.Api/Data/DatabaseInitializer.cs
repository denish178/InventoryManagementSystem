using InventoryManagement.Api.DTOs.StockTransactions;
using InventoryManagement.Api.Entities;
using InventoryManagement.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            for (var attempt = 1; attempt <= 20; attempt++)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    break;
                }
                catch (Exception ex) when (attempt < 20)
                {
                    logger.LogWarning(ex, "Database is not ready (attempt {Attempt}/20). Retrying.", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
        }

        await SeedRolesAndAdminAsync(services);
        await SeedDemoUsersAsync(services);

        if (app.Configuration.GetValue<bool>("SeedDemoData"))
        {
            await SeedCatalogAsync(services);
        }
    }

    private static async Task SeedRolesAndAdminAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        string[] roles = ["Admin", "Inventory Staff", "Viewer"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        var email = configuration["AdminSeed:Email"]
            ?? throw new InvalidOperationException("AdminSeed:Email is missing.");
        var password = configuration["AdminSeed:Password"]
            ?? throw new InvalidOperationException("AdminSeed:Password is missing.");
        var fullName = configuration["AdminSeed:FullName"] ?? "System Admin";

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                IsActive = true,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin creation failed: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin role assignment failed: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedDemoUsersAsync(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var users = services.GetRequiredService<IUserAccountService>();
        var configured = configuration.GetSection("DemoUsers").GetChildren();

        foreach (var entry in configured)
        {
            var email = entry["Email"];
            var password = entry["Password"];
            var fullName = entry["FullName"];
            var role = entry["Role"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var (_, error) = await users.CreateAsync(new DTOs.Users.CreateUserDto
            {
                Email = email,
                Password = password,
                FullName = string.IsNullOrWhiteSpace(fullName) ? email : fullName,
                Role = role
            });

            if (error is not null)
            {
                throw new InvalidOperationException($"Demo user '{email}' was not created: {error.Message}");
            }
        }
    }

    private static async Task SeedCatalogAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (await db.Categories.AnyAsync())
        {
            return;
        }

        var fasteners = new Category { Name = "Fasteners", Description = "Bolts, nuts, and fittings" };
        var electrical = new Category { Name = "Electrical", Description = "Cable and electrical supplies" };
        var safety = new Category { Name = "Safety", Description = "Personal protective equipment" };
        db.Categories.AddRange(fasteners, electrical, safety);

        var northwind = new Supplier
        {
            Name = "Northwind Supply",
            ContactPerson = "Purchasing desk",
            Email = "orders@northwind.example",
            Phone = "555-0148",
            Address = "18 Harbor Road"
        };
        var harbor = new Supplier
        {
            Name = "Harbor Industrial",
            ContactPerson = "Sales desk",
            Email = "sales@harbor.example",
            Phone = "555-0172",
            Address = "4 Dock Street"
        };
        db.Suppliers.AddRange(northwind, harbor);
        await db.SaveChangesAsync();

        var bolt = new Product
        {
            SKU = "HEX-M8-20",
            Name = "Hex bolt M8 x 20",
            Description = "Zinc-plated hex bolt",
            UnitPrice = 0.42m,
            QuantityInStock = 0,
            ReorderLevel = 25,
            CategoryId = fasteners.Id,
            SupplierId = northwind.Id
        };
        var cable = new Product
        {
            SKU = "WIRE-12-100",
            Name = "12 AWG cable, 100 m",
            Description = "Copper building wire",
            UnitPrice = 86.50m,
            QuantityInStock = 0,
            ReorderLevel = 8,
            CategoryId = electrical.Id,
            SupplierId = harbor.Id
        };
        var vest = new Product
        {
            SKU = "VEST-L",
            Name = "High-visibility vest, large",
            UnitPrice = 18.00m,
            QuantityInStock = 0,
            ReorderLevel = 10,
            CategoryId = safety.Id,
            SupplierId = northwind.Id
        };
        db.Products.AddRange(bolt, cable, vest);
        await db.SaveChangesAsync();

        var stock = services.GetRequiredService<IStockMovementService>();
        await stock.StockInAsync(new StockMovementRequestDto { ProductId = bolt.Id, Quantity = 12, ReferenceNumber = "PO-1001", Notes = "Opening receipt" }, "seed", CancellationToken.None);
        await stock.StockInAsync(new StockMovementRequestDto { ProductId = cable.Id, Quantity = 20, ReferenceNumber = "PO-1002", Notes = "Opening receipt" }, "seed", CancellationToken.None);
        await stock.StockInAsync(new StockMovementRequestDto { ProductId = vest.Id, Quantity = 10, ReferenceNumber = "PO-1003", Notes = "Opening receipt" }, "seed", CancellationToken.None);
        await stock.StockOutAsync(new StockMovementRequestDto { ProductId = cable.Id, Quantity = 4, ReferenceNumber = "SO-2001", Notes = "Workshop issue" }, "seed", CancellationToken.None);
    }
}
