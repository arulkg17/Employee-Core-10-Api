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
        var idParam = new SqlParameter("@Id", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        await _context.Database.ExecuteSqlRawAsync(
            @"EXEC dbo.sp_Product_Insert
                @Id OUTPUT,
                @Name,
                @Description,
                @Price,
                @Stock",
            idParam,
            new SqlParameter(
                "@Name",
                product.Name ?? (object)DBNull.Value),
            new SqlParameter(
                "@Description",
                product.Description ?? (object)DBNull.Value),
            new SqlParameter("@Price", product.Price),
            new SqlParameter("@Stock", product.Stock));

        return (int)idParam.Value;
    }

    public async Task<IEnumerable<ProductEntity>> GetAllAsync()
    {
        return await _context.Products
            .FromSqlRaw("EXEC dbo.sp_Product_GetAll")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ProductEntity?> GetByIdAsync(int id)
    {
        var products = await _context.Products
            .FromSqlRaw(
                "EXEC dbo.sp_Product_GetById @Id",
                new SqlParameter("@Id", id))
            .AsNoTracking()
            .ToListAsync();

        return products.FirstOrDefault();
    }

    public async Task<bool> UpdateAsync(ProductEntity product)
    {
        var connection = _context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();

        command.CommandText = "dbo.sp_Product_Update";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@Id", product.Id));
        command.Parameters.Add(new SqlParameter("@Name",
            product.Name ?? (object)DBNull.Value));
        command.Parameters.Add(new SqlParameter("@Description",
            product.Description ?? (object)DBNull.Value));
        command.Parameters.Add(new SqlParameter("@Price", product.Price));
        command.Parameters.Add(new SqlParameter("@Stock", product.Stock));
        command.Parameters.Add(new SqlParameter(
            "@RowVersion",
            product.RowVersion));

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

        command.Parameters.Add(new SqlParameter("@Id", id));
        command.Parameters.Add(new SqlParameter(
            "@RowVersion",
            rowVersion));

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        var affectedRows = await command.ExecuteScalarAsync();

        return Convert.ToInt32(affectedRows) > 0;
    }
}
