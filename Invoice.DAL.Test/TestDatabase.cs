namespace Invoice.DAL.Test;

public static class TestDatabase
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Server=LAPTOP-BIG8QIRC,1435;" +
           "Database=Accounts_Test;" +
           "User Id=sa;" +
           "Password=123456;" +
           "Encrypt=False;" +
           "TrustServerCertificate=True";
}