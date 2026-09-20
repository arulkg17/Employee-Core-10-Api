using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Invoice.DAL.Repositories;

public class ProductRepositorySp : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepositorySp(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(ProductEntity product)
    {
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = "dbo.sp_Product_Insert";
        command.CommandType = CommandType.StoredProcedure;

        var idParameter = new SqlParameter("@Id", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(idParameter);

        command.Parameters.Add(new SqlParameter(
            "@Name",
            product.Name ?? (object)DBNull.Value));

        command.Parameters.Add(new SqlParameter(
            "@Description",
            product.Description ?? (object)DBNull.Value));

        command.Parameters.Add(new SqlParameter(
            "@Price",
            product.Price));

        command.Parameters.Add(new SqlParameter(
            "@Stock",
            product.Stock));

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await command.ExecuteNonQueryAsync();

        return Convert.ToInt32(idParameter.Value);
    }

    public async Task<IEnumerable<ProductEntity>> GetAllAsync()
    {
        var products = new List<ProductEntity>();

        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = "dbo.sp_Product_GetAll";
        command.CommandType = CommandType.StoredProcedure;

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(MapProduct(reader));
        }

        return products;
    }

    public async Task<ProductEntity?> GetByIdAsync(int id)
    {
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = "dbo.sp_Product_GetById";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter("@Id", SqlDbType.Int)
            {
                Value = id
            });

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapProduct(reader);
        }

        return null;
    }

    public async Task<bool> UpdateAsync(ProductEntity product)
    {
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = "dbo.sp_Product_Update";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter("@Id", SqlDbType.Int)
            {
                Value = product.Id
            });

        command.Parameters.Add(
            new SqlParameter("@Name", SqlDbType.NVarChar, 150)
            {
                Value = product.Name ?? (object)DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter("@Description", SqlDbType.NVarChar, 500)
            {
                Value = product.Description ?? (object)DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter("@Price", SqlDbType.Decimal)
            {
                Precision = 10,
                Scale = 2,
                Value = product.Price
            });

        command.Parameters.Add(
            new SqlParameter("@Stock", SqlDbType.Int)
            {
                Value = product.Stock
            });

        command.Parameters.Add(
            new SqlParameter("@RowVersion", SqlDbType.VarBinary, 8)
            {
                Value = product.RowVersion
            });

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        var affectedRows = await command.ExecuteScalarAsync();

        return Convert.ToInt32(affectedRows) > 0;
    }

    public async Task<bool> DeleteAsync(int id, byte[] rowVersion)
    {
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = "dbo.sp_Product_Delete";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter("@Id", SqlDbType.Int)
            {
                Value = id
            });

        command.Parameters.Add(
            new SqlParameter("@RowVersion", SqlDbType.VarBinary, 8)
            {
                Value = rowVersion
            });

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        var affectedRows = await command.ExecuteScalarAsync();

        return Convert.ToInt32(affectedRows) > 0;
    }

    private static ProductEntity MapProduct(
        System.Data.Common.DbDataReader reader)
    {
        return new ProductEntity
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),

            Name = reader.IsDBNull(reader.GetOrdinal("Name"))
                ? null
                : reader.GetString(reader.GetOrdinal("Name")),

            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? null
                : reader.GetString(reader.GetOrdinal("Description")),

            Price = reader.GetDecimal(
                reader.GetOrdinal("Price")),

            Stock = reader.GetInt32(
                reader.GetOrdinal("Stock")),

            RowVersion = reader.IsDBNull(
                reader.GetOrdinal("RowVersion"))
                ? Array.Empty<byte>()
                : (byte[])reader["RowVersion"]
        };
    }
}