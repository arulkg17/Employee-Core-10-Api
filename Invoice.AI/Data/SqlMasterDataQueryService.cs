using System.Data;
using Dapper;
using Invoice.AI.Abstractions;
using Invoice.AI.Models;
using static Invoice.AI.Data.MasterTableMap;

namespace Invoice.AI.Data;

/// <summary>
/// Real SQL Server implementation (read-only COUNT queries via Dapper).
/// Uses the IDbConnection that is already registered in your API's Program.cs.
/// Table/column names come from MasterTableMap - they are constants, never user input,
/// and every value is passed as a SQL parameter, so there is no SQL-injection risk.
/// </summary>
public sealed class SqlMasterDataQueryService : IMasterDataQueryService
{
    private readonly IDbConnection _db;

    public SqlMasterDataQueryService(IDbConnection db) => _db = db;

    public async Task<IReadOnlyList<string>> GetCategoryNamesAsync(CancellationToken ct = default)
    {
        var sql = $"SELECT DISTINCT LTRIM(RTRIM([{CategoryName}])) FROM [{CategoryTable}] WHERE [{CategoryName}] IS NOT NULL";
        var rows = await _db.QueryAsync<string>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
    }

    public async Task<IReadOnlyList<string>> GetCityNamesAsync(CancellationToken ct = default)
    {
        var sql = $"SELECT DISTINCT LTRIM(RTRIM([{CustomerCity}])) FROM [{CustomerTable}] WHERE [{CustomerCity}] IS NOT NULL";
        var rows = await _db.QueryAsync<string>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x).ToList();
    }

    public Task<int> CountCategoriesAsync(bool? isActive, CancellationToken ct = default)
        => CountAsync(CategoryTable, CategoryIsActive, isActive, ct);

    public async Task<int?> CountItemsInCategoryAsync(string categoryName, bool? itemIsActive, CancellationToken ct = default)
    {
        var idSql = $"SELECT TOP 1 [{CategoryId}] FROM [{CategoryTable}] WHERE LTRIM(RTRIM([{CategoryName}])) = @Name";
        var categoryId = await _db.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(idSql, new { Name = categoryName.Trim() }, cancellationToken: ct));

        if (categoryId is null)
            return null;

        var sql =
            $"SELECT COUNT(*) FROM [{ItemTable}] " +
            $"WHERE [{ItemCategoryId}] = @CategoryId AND (@IsActive IS NULL OR [{ItemIsActive}] = @IsActive)";

        return await _db.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { CategoryId = categoryId.Value, IsActive = itemIsActive }, cancellationToken: ct));
    }

    public Task<int> CountItemsAsync(bool? isActive, CancellationToken ct = default)
        => CountAsync(ItemTable, ItemIsActive, isActive, ct);

    public Task<int> CountCustomersAsync(bool? isActive, CancellationToken ct = default)
        => CountAsync(CustomerTable, CustomerIsActive, isActive, ct);

    public async Task<int> CountCustomersByCityAsync(string city, bool? isActive, CancellationToken ct = default)
    {
        var sql =
            $"SELECT COUNT(*) FROM [{CustomerTable}] " +
            $"WHERE LTRIM(RTRIM([{CustomerCity}])) = @City AND (@IsActive IS NULL OR [{CustomerIsActive}] = @IsActive)";

        return await _db.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { City = city.Trim(), IsActive = isActive }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<CityCount>> CountCustomersGroupedByCityAsync(bool? isActive, CancellationToken ct = default)
    {
        var sql =
            $"SELECT LTRIM(RTRIM([{CustomerCity}])) AS City, COUNT(*) AS [Count] FROM [{CustomerTable}] " +
            $"WHERE [{CustomerCity}] IS NOT NULL AND LTRIM(RTRIM([{CustomerCity}])) <> '' " +
            $"AND (@IsActive IS NULL OR [{CustomerIsActive}] = @IsActive) " +
            $"GROUP BY LTRIM(RTRIM([{CustomerCity}])) ORDER BY City";

        var rows = await _db.QueryAsync<CityRow>(
            new CommandDefinition(sql, new { IsActive = isActive }, cancellationToken: ct));

        return rows.Select(r => new CityCount(r.City, r.Count)).ToList();
    }

    public Task<int> CountVendorsAsync(bool? isActive, CancellationToken ct = default)
        => CountAsync(VendorTable, VendorIsActive, isActive, ct);

    public Task<int> CountUsersAsync(bool? isActive, CancellationToken ct = default)
        => CountAsync(UserTable, UserIsActive, isActive, ct);

    private async Task<int> CountAsync(string table, string activeColumn, bool? isActive, CancellationToken ct)
    {
        var sql = $"SELECT COUNT(*) FROM [{table}] WHERE (@IsActive IS NULL OR [{activeColumn}] = @IsActive)";

        return await _db.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { IsActive = isActive }, cancellationToken: ct));
    }
}

internal sealed class CityRow
{
    public string City { get; set; } = string.Empty;
    public int Count { get; set; }
}
