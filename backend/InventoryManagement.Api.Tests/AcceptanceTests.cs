using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace InventoryManagement.Api.Tests;

public class AcceptanceTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AcceptanceTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task T01_CreateProduct_ReturnsId()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var ids = await CreateCatalogAsync(admin, $"SKU-{Guid.NewGuid():N}"[..12]);

        using var response = await SendAsync(HttpMethod.Get, $"/api/products/{ids.ProductId}", admin);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadAsync(response);
        Assert.Equal(ids.ProductId, body.GetProperty("id").GetInt32());
        Assert.Equal(0, body.GetProperty("quantityInStock").GetInt32());
    }

    [Fact]
    public async Task T02_DuplicateSku_IsRejected()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var sku = $"DUP-{Guid.NewGuid():N}"[..12];
        await CreateCatalogAsync(admin, sku);

        using var response = await SendAsync(HttpMethod.Post, "/api/products", admin, new
        {
            sku,
            name = "Duplicate attempt",
            unitPrice = 1.25m,
            reorderLevel = 1,
            categoryId = await LatestCategoryId(admin),
            supplierId = await LatestSupplierId(admin)
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task T03_T04_StockInAndStockOut_UpdateBalanceAndHistory()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var ids = await CreateCatalogAsync(admin, $"MOV-{Guid.NewGuid():N}"[..12], reorderLevel: 5);

        using var firstIn = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-in", admin, new
        {
            productId = ids.ProductId,
            quantity = 5
        });
        Assert.Equal(HttpStatusCode.Created, firstIn.StatusCode);

        using var secondIn = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-in", admin, new
        {
            productId = ids.ProductId,
            quantity = 10
        });
        Assert.Equal(HttpStatusCode.Created, secondIn.StatusCode);
        Assert.Equal(15, await QuantityAsync(admin, ids.ProductId));
        Assert.Equal(2, await MovementCountAsync(admin, ids.ProductId, "StockIn"));

        using var stockOut = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-out", admin, new
        {
            productId = ids.ProductId,
            quantity = 4
        });
        Assert.Equal(HttpStatusCode.Created, stockOut.StatusCode);
        Assert.Equal(11, await QuantityAsync(admin, ids.ProductId));
        Assert.Equal(1, await MovementCountAsync(admin, ids.ProductId, "StockOut"));
    }

    [Fact]
    public async Task T05_StockOutBeyondAvailable_LeavesBalanceUnchanged()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var ids = await CreateCatalogAsync(admin, $"LOW-{Guid.NewGuid():N}"[..12]);
        await StockInAsync(admin, ids.ProductId, 5);

        using var response = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-out", admin, new
        {
            productId = ids.ProductId,
            quantity = 9
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(5, await QuantityAsync(admin, ids.ProductId));
        Assert.Equal(0, await MovementCountAsync(admin, ids.ProductId, "StockOut"));
    }

    [Fact]
    public async Task T06_ZeroOrNegativeQuantity_IsRejected()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var ids = await CreateCatalogAsync(admin, $"QTY-{Guid.NewGuid():N}"[..12]);

        using var zero = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-in", admin, new
        {
            productId = ids.ProductId,
            quantity = 0
        });
        using var negative = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-out", admin, new
        {
            productId = ids.ProductId,
            quantity = -3
        });

        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        Assert.Equal(0, await QuantityAsync(admin, ids.ProductId));
        Assert.Equal(0, await MovementCountAsync(admin, ids.ProductId, null));
    }

    [Fact]
    public async Task T07_MissingProduct_ReturnsNotFound()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        using var response = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-in", admin, new
        {
            productId = 999999,
            quantity = 1
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task T08_SaveFailure_RollsBackStockBalance()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var ids = await CreateCatalogAsync(admin, $"RBK-{Guid.NewGuid():N}"[..12]);
        await StockInAsync(admin, ids.ProductId, 5);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/stock-transactions/stock-in")
        {
            Content = JsonContent.Create(new { productId = ids.ProductId, quantity = 10 })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
        request.Headers.Add("X-Test-Fault", "throw");

        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(5, await QuantityAsync(admin, ids.ProductId));
        Assert.Equal(1, await MovementCountAsync(admin, ids.ProductId, null));
    }

    [Fact]
    public async Task T09_QuantityEqualToReorderLevel_AppearsInLowStock()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var ids = await CreateCatalogAsync(admin, $"RDR-{Guid.NewGuid():N}"[..12], reorderLevel: 5);
        await StockInAsync(admin, ids.ProductId, 5);

        using var response = await SendAsync(HttpMethod.Get, "/api/reports/low-stock", admin);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadAsync(response);
        var match = body.GetProperty("products").EnumerateArray()
            .Any(item => item.GetProperty("id").GetInt32() == ids.ProductId
                && item.GetProperty("quantityInStock").GetInt32() == 5);
        Assert.True(match);
    }

    [Fact]
    public async Task T10_DeleteReferencedCategoryAndSupplier_IsBlocked()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var ids = await CreateCatalogAsync(admin, $"DEL-{Guid.NewGuid():N}"[..12]);

        using var category = await SendAsync(HttpMethod.Delete, $"/api/categories/{ids.CategoryId}", admin);
        using var supplier = await SendAsync(HttpMethod.Delete, $"/api/suppliers/{ids.SupplierId}", admin);

        Assert.Equal(HttpStatusCode.Conflict, category.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, supplier.StatusCode);
        Assert.Contains("products", (await ReadAsync(category)).GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("products", (await ReadAsync(supplier)).GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task T11_MovementReport_FiltersByProductTypeAndDate()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var first = await CreateCatalogAsync(admin, $"R1-{Guid.NewGuid():N}"[..12]);
        var second = await CreateCatalogAsync(admin, $"R2-{Guid.NewGuid():N}"[..12]);
        await StockInAsync(admin, first.ProductId, 3);
        await StockInAsync(admin, second.ProductId, 8);
        using var stockOut = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-out", admin, new
        {
            productId = second.ProductId,
            quantity = 2
        });
        Assert.Equal(HttpStatusCode.Created, stockOut.StatusCode);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/reports/stock-movements?productId={second.ProductId}&transactionType=StockOut&startDate={today}&endDate={today}",
            admin);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadAsync(response);
        var rows = body.GetProperty("transactions").EnumerateArray().ToList();
        Assert.Single(rows);
        Assert.Equal(second.ProductId, rows[0].GetProperty("productId").GetInt32());
        Assert.Equal("StockOut", rows[0].GetProperty("transactionType").GetString());
        Assert.Equal(1, body.GetProperty("totalTransactions").GetInt32());
    }

    [Fact]
    public async Task ChangeUserRole_IsReturnedByUserList()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var email = $"role-{Guid.NewGuid():N}@ims.local";

        using var created = await SendAsync(HttpMethod.Post, "/api/users", admin, new
        {
            fullName = "Role Test User",
            email,
            password = "Viewer123!",
            role = "Viewer"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var userId = (await ReadAsync(created)).GetProperty("id").GetString()!;

        using var changed = await SendAsync(HttpMethod.Put, $"/api/users/{userId}/role", admin, new
        {
            role = "Inventory Staff"
        });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        using var users = await SendAsync(HttpMethod.Get, "/api/users", admin);
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
        var usersJson = await ReadAsync(users);
        var changedUser = usersJson.EnumerateArray().Single(user => user.GetProperty("id").GetString() == userId);
        var roles = changedUser.GetProperty("roles").EnumerateArray()
            .Select(role => role.GetString())
            .ToArray();

        Assert.Single(roles);
        Assert.Equal("Inventory Staff", roles[0]);
    }

    [Fact]
    public async Task T12_FreshDatabase_SeedsAdminAndAcceptsLogin()
    {
        using var health = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        var token = await LoginAsync("admin@ims.local", "Admin123!");
        using var users = await SendAsync(HttpMethod.Get, "/api/users", token);
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_Returns401_AndViewerCannotStockOut()
    {
        using var anonymous = await _client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        using var created = await SendAsync(HttpMethod.Post, "/api/auth/register", admin, new
        {
            fullName = "Read Only",
            email = $"viewer-{Guid.NewGuid():N}@ims.local",
            password = "Viewer123!",
            role = "Viewer"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var viewerEmail = (await ReadAsync(created)).GetProperty("email").GetString()!;
        var viewer = await LoginAsync(viewerEmail, "Viewer123!");

        using var denied = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-out", viewer, new
        {
            productId = 1,
            quantity = 1
        });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task ProductUpdate_DoesNotChangeQuantity()
    {
        var admin = await LoginAsync("admin@ims.local", "Admin123!");
        var ids = await CreateCatalogAsync(admin, $"UPD-{Guid.NewGuid():N}"[..12]);
        await StockInAsync(admin, ids.ProductId, 7);

        using var response = await SendAsync(HttpMethod.Put, $"/api/products/{ids.ProductId}", admin, new
        {
            name = "Renamed product",
            description = "Updated",
            unitPrice = 4.5m,
            reorderLevel = 2,
            categoryId = ids.CategoryId,
            supplierId = ids.SupplierId,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(7, await QuantityAsync(admin, ids.ProductId));
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return JsonDocument.Parse(body).RootElement.GetProperty("token").GetString()!;
    }

    private async Task<(int CategoryId, int SupplierId, int ProductId)> CreateCatalogAsync(
        string token,
        string sku,
        int reorderLevel = 1)
    {
        using var categoryResponse = await SendAsync(HttpMethod.Post, "/api/categories", token, new
        {
            name = $"Cat {sku}",
            description = "Acceptance category"
        });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var categoryId = (await ReadAsync(categoryResponse)).GetProperty("id").GetInt32();

        using var supplierResponse = await SendAsync(HttpMethod.Post, "/api/suppliers", token, new
        {
            name = $"Supplier {sku}",
            email = $"{sku}@example.com".Replace("-", "")
        });
        Assert.Equal(HttpStatusCode.Created, supplierResponse.StatusCode);
        var supplierId = (await ReadAsync(supplierResponse)).GetProperty("id").GetInt32();

        using var productResponse = await SendAsync(HttpMethod.Post, "/api/products", token, new
        {
            sku,
            name = $"Product {sku}",
            unitPrice = 2.50m,
            reorderLevel,
            categoryId,
            supplierId
        });
        var productBody = await productResponse.Content.ReadAsStringAsync();
        Assert.True(productResponse.StatusCode == HttpStatusCode.Created, productBody);
        var productId = JsonDocument.Parse(productBody).RootElement.GetProperty("id").GetInt32();
        return (categoryId, supplierId, productId);
    }

    private async Task StockInAsync(string token, int productId, int quantity)
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/stock-transactions/stock-in", token, new
        {
            productId,
            quantity
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<int> QuantityAsync(string token, int productId)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/api/products/{productId}", token);
        var body = await ReadAsync(response);
        return body.GetProperty("quantityInStock").GetInt32();
    }

    private async Task<int> MovementCountAsync(string token, int productId, string? type)
    {
        var url = $"/api/stock-transactions?productId={productId}";
        if (!string.IsNullOrEmpty(type))
        {
            url += $"&transactionType={type}";
        }

        using var response = await SendAsync(HttpMethod.Get, url, token);
        var body = await ReadAsync(response);
        return body.GetArrayLength();
    }

    private async Task<int> LatestCategoryId(string token)
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/categories", token);
        return (await ReadAsync(response)).EnumerateArray().Last().GetProperty("id").GetInt32();
    }

    private async Task<int> LatestSupplierId(string token)
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/suppliers", token);
        return (await ReadAsync(response)).EnumerateArray().Last().GetProperty("id").GetInt32();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.Clone();
    }
}
