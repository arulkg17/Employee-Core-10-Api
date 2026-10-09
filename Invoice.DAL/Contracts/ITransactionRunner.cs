namespace Invoice.DAL.Contracts;

/// <summary>
/// Runs several repository calls inside ONE database transaction
/// (header + detail rows are saved together or not at all).
/// </summary>
public interface ITransactionRunner
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action);

    Task ExecuteAsync(Func<Task> action);
}
