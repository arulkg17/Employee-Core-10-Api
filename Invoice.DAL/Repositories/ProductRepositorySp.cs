using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore;
namespace Invoice.DAL.Repositories;

public class ProductSPRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductSPRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(ProductEntity product)
    {
        var idParam = new SqlParameter("@Id", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };

        var result = await _context.Database.ExecuteSqlRawAsync(
            "EXEC @Id = sp_Product_Insert @Name, @Price, @Stock",
            idParam,
            new SqlParameter("@Name", product.Name),
            new SqlParameter("@Price", product.Price),
            new SqlParameter("@Stock", product.Stock));

        return (int)idParam.Value;
    }

    public async Task<IEnumerable<ProductEntity>> GetAllAsync()
    {
        return await _context.Products
            .FromSqlRaw("EXEC sp_Product_GetAll")
            .ToListAsync();
    }

    public async Task<ProductEntity?> GetByIdAsync(int id)
    {
        return await _context.Products
            .FromSqlRaw("EXEC sp_Product_GetById @Id",
                new SqlParameter("@Id", id))
           .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(ProductEntity product)
    {
        var affectedRows = await _context.Database
            .SqlQuery<int>($@"
            EXEC sp_Product_Update
                @Id={product.Id},
                @Name={product.Name},
                @Price={product.Price},
                @Stock={product.Stock},
                @RowVersion={product.RowVersion}")
            .FirstAsync();

        return affectedRows > 0; // false = concurrency conflict
    }

    public async Task<bool> DeleteAsync(int id, byte[] rowVersion)
    {
        var affectedRows = await _context.Database
    .SqlQuery<int>($@"
            EXEC sp_Product_Delete
                @Id={id},
                @RowVersion={rowVersion}")
    .FirstAsync();
        return affectedRows > 0;
    }
}
