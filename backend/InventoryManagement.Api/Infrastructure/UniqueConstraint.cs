using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Infrastructure;

public static class UniqueConstraint
{
    public static bool IsViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}
