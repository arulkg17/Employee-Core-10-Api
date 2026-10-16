using Invoice.DAL.Contracts;
using Invoice.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Repositories;

public class EfTransactionRunner : ITransactionRunner
{
    private readonly AppDbContext _dbContext;

    public EfTransactionRunner(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        // Already inside a transaction: just join it.
        if (_dbContext.Database.CurrentTransaction is not null)
            return await action();

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync();

            var result = await action();

            await transaction.CommitAsync();

            return result;
        });
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        await ExecuteAsync<bool>(async () =>
        {
            await action();
            return true;
        });
    }
}
