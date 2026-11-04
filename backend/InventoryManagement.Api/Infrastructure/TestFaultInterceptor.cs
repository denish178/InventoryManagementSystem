using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InventoryManagement.Api.Infrastructure;

/// <summary>
/// Test-only hook used to prove a stock movement rolls back when saving the history row fails.
/// It is registered only when the host environment is Testing.
/// </summary>
public class TestFaultInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestFaultInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_httpContextAccessor.HttpContext?.Request.Headers["X-Test-Fault"] == "throw")
        {
            throw new InvalidOperationException("Injected save failure for transaction rollback test.");
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
