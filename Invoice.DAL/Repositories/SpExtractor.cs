using System.Data;
using System.Data.Common;
using Invoice.Data.Db;
using Invoice.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Invoice.DAL.Repositories;

/// <summary>
/// Runs stored procedures through the AppDbContext connection.
///  - joins the current EF transaction (needed by ITransactionRunner)
///  - opens / closes the connection only when it was closed
///  - translates SQL errors into BusinessRuleException / NotFoundException
/// </summary>
internal static class SpExecutor
{
    public static SqlParameter P(string name, object? value)
        => new(name, value ?? DBNull.Value);

    public static SqlParameter Dec(string name, decimal value)
        => new(name, SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = value };

    public static async Task<int> ScalarIntAsync(
        AppDbContext db, string sp, params SqlParameter[] parameters)
    {
        var result = await ExecuteAsync<object?>(
            db, sp, parameters, cmd => cmd.ExecuteScalarAsync());

        return Convert.ToInt32(result);
    }

    public static async Task<bool> ScalarBoolAsync(
        AppDbContext db, string sp, params SqlParameter[] parameters)
    {
        var result = await ExecuteAsync<object?>(
            db, sp, parameters, cmd => cmd.ExecuteScalarAsync());

        return result is not null && result != DBNull.Value && Convert.ToBoolean(result);
    }

    public static Task<int> NonQueryAsync(
        AppDbContext db, string sp, params SqlParameter[] parameters)
    {
        return ExecuteAsync<int>(
            db, sp, parameters, cmd => cmd.ExecuteNonQueryAsync());
    }

    /// <summary>Procedure with 2 result sets: rows, then one row with the total count.</summary>
    public static async Task<(List<T> Rows, int Total)> PagedAsync<T>(
        AppDbContext db,
        string sp,
        Func<DbDataReader, T> map,
        params SqlParameter[] parameters)
    {
        return await ExecuteAsync<(List<T> Rows, int Total)>(
            db, sp, parameters,
            async cmd =>
            {
                await using var reader = await cmd.ExecuteReaderAsync();

                var rows = new List<T>();

                while (await reader.ReadAsync())
                    rows.Add(map(reader));

                var total = 0;

                if (await reader.NextResultAsync() && await reader.ReadAsync())
                    total = reader.GetInt32(0);

                return (rows, total);
            });
    }

    private static async Task<T> ExecuteAsync<T>(
        AppDbContext db,
        string sp,
        SqlParameter[] parameters,
        Func<DbCommand, Task<T>> run)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();

            command.CommandText = sp;
            command.CommandType = CommandType.StoredProcedure;
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            command.Parameters.AddRange(parameters);

            return await run(command);
        }
        catch (SqlException ex)
        {
            var translated = Translate(ex);

            if (translated is null)
                throw;

            throw translated;
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }

    private static Exception? Translate(SqlException ex)
    {
        return ex.Number switch
        {
            51404 => new NotFoundException(ex.Message, ex),

            >= 51000 and <= 51999 => new BusinessRuleException(ex.Message, ex),

            2601 or 2627 => new BusinessRuleException(
                "A record with the same unique value (for example the document number) already exists.", ex),

            547 => new BusinessRuleException(
                "The operation violates a reference rule: a related record does not exist or the record is in use.", ex),

            _ => null
        };
    }
}
