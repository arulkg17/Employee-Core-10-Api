using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Repositories;

public class StockRepositoryEFSp : IStockRepository
{
    private readonly AppDbContext _dbContext;

    public StockRepositoryEFSp(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ItemStockEntity>> GetAllAsync()
    {
        return await _dbContext.ItemStocks
            .FromSqlRaw("EXEC dbo.sp_Stock_GetAll")
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ItemStockEntity?> GetByItemmasterIdAsync(int itemmasterId)
    {
        var rows = await _dbContext.ItemStocks
            .FromSqlRaw(
                "EXEC dbo.sp_Stock_GetByItemmasterId @ItemmasterId",
                new SqlParameter("@ItemmasterId", itemmasterId))
            .AsNoTracking()
            .ToListAsync();

        return rows.FirstOrDefault();
    }
}
