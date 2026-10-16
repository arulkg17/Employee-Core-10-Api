using AutoMapper;
using Invoice.BAL.Mapper;
using Invoice.DAL.Contracts;
using Microsoft.Extensions.Logging;

namespace Invoice.BAL.Test;

/// <summary>Runs the action directly (no database) and records how often a transaction was requested.</summary>
public class FakeTransactionRunner : ITransactionRunner
{
    public int Calls { get; private set; }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        Calls++;
        return await action();
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        Calls++;
        await action();
    }
}

public static class TestMapper
{
    public static IMapper Create()
    {
        using var loggerFactory = LoggerFactory.Create(builder => { });

        var configuration = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile<PurchaseOrderProfile>();
                cfg.AddProfile<ReceiptProfile>();
                cfg.AddProfile<SalesInvoiceProfile>();
                cfg.AddProfile<StockProfile>();
            },
            loggerFactory);

        return configuration.CreateMapper();
    }
}
