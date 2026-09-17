using Dapper;
using Invoice.DAL.Repositories;
using Invoice.Data.Entities;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Invoice.DAL.Test;

public class UserRepositorySpDapTests
{
    private const string ConnectionString =
    "Server=LAPTOP-BIG8QIRC,1435;Database=Accounts_Test;User Id=sa;Password=123456;Encrypt=False;TrustServerCertificate=True";

private SqlConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    // ============================================================
    // TEST DATA CLEANUP
    // ============================================================

    private async Task CleanupTestDataAsync(SqlConnection connection)
    {
        await connection.ExecuteAsync(
            """
        DELETE FROM dbo.Users
        WHERE UserName IN
        (
            'TSTUSER001',
            'TSTUSER002',
            'TSTUSER003',
            'TSTUSER004',
            'TSTUSER001_UPDATED',
            'TSTUSER005',
            'NEWUSERNAME'
        )
        OR Email IN
        (
            'tstuser001@test.com',
            'tstuser002@test.com',
            'tstuser003@test.com',
            'tstuser004@test.com',
            'updateduser@test.com',
            'tstuser005@test.com',
            'newemail@test.com'
        );
        """);
    }

    // ============================================================
    // TEST DATA SEED
    // ============================================================

    private async Task SeedTestDataAsync(SqlConnection connection)
    {
        await CleanupTestDataAsync(connection);

        await connection.ExecuteAsync(
            """
        INSERT INTO dbo.Users
        (
            UserName,
            Email,
            PasswordHash,
            FirstName,
            MiddleName,
            LastName,
            DisplayName,
            PhoneNumber,
            AlternatePhone,
            AddressLine1,
            AddressLine2,
            City,
            State,
            ZipCode,
            Country,
            DateOfBirth,
            IsActive,
            IsDeleted,
            CreatedBy,
            CreatedDate,
            UpdatedBy,
            UpdatedDate
        )
        VALUES
        (
            'TSTUSER001',
            'tstuser001@test.com',
            'HASH001',
            'Test',
            'A',
            'UserOne',
            'Test User One',
            '9000000001',
            NULL,
            'Address 1',
            NULL,
            'Chennai',
            'Tamil Nadu',
            '600001',
            'India',
            '1990-01-01',
            1,
            0,
            'TEST',
            GETUTCDATE(),
            NULL,
            NULL
        ),
        (
            'TSTUSER002',
            'tstuser002@test.com',
            'HASH002',
            'Test',
            'B',
            'UserTwo',
            'Test User Two',
            '9000000002',
            NULL,
            'Address 2',
            NULL,
            'Bangalore',
            'Karnataka',
            '560001',
            'India',
            '1991-02-02',
            1,
            0,
            'TEST',
            GETUTCDATE(),
            NULL,
            NULL
        ),
        (
            'TSTUSER003',
            'tstuser003@test.com',
            'HASH003',
            'Test',
            'C',
            'UserThree',
            'Test User Three',
            '9000000003',
            NULL,
            'Address 3',
            NULL,
            'Chennai',
            'Tamil Nadu',
            '600002',
            'India',
            '1992-03-03',
            0,
            0,
            'TEST',
            GETUTCDATE(),
            NULL,
            NULL
        ),
        (
            'TSTUSER004',
            'tstuser004@test.com',
            'HASH004',
            'Test',
            'D',
            'UserFour',
            'Test User Four',
            '9000000004',
            NULL,
            'Address 4',
            NULL,
            'Madurai',
            'Tamil Nadu',
            '625001',
            'India',
            '1993-04-04',
            1,
            1,
            'TEST',
            GETUTCDATE(),
            NULL,
            NULL
        );
        """);
    }

    // ============================================================
    // HELPER
    // ============================================================

    private UserRepositorySpDap CreateRepository(SqlConnection connection)
    {
        return new UserRepositorySpDap(connection);
    }

    // ============================================================
    // 1. GET ALL
    // ============================================================
    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyNonDeletedUsers()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var result = (await repository.GetAllAsync()).ToList();

        Assert.Contains(
            result,
            x => x.UserName == "TSTUSER001");

        Assert.Contains(
            result,
            x => x.UserName == "TSTUSER002");

        Assert.Contains(
            result,
            x => x.UserName == "TSTUSER003");

        Assert.DoesNotContain(
            result,
            x => x.UserName == "TSTUSER004");
    }

    // ============================================================
    // 2. GET BY ID
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenIdExists()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var userId = await connection.ExecuteScalarAsync<int>(
            """
        SELECT Id
        FROM dbo.Users
        WHERE UserName = 'TSTUSER001'
        """);

        var repository = CreateRepository(connection);

        var result = await repository.GetByIdAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result!.Id);
        Assert.Equal("TSTUSER001", result.UserName);
        Assert.Equal("tstuser001@test.com", result.Email);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("UserOne", result.LastName);
        Assert.Equal("Test User One", result.DisplayName);
        Assert.True(result.IsActive);
        Assert.False(result.IsDeleted);
    }

    // ============================================================
    // 3. GET BY ID - NOT FOUND
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var result = await repository.GetByIdAsync(999999);

        Assert.Null(result);
    }

    // ============================================================
    // 4. GET BY ID - DELETED USER
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserIsDeleted()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var userId = await connection.ExecuteScalarAsync<int>(
            """
        SELECT Id
        FROM dbo.Users
        WHERE UserName = 'TSTUSER004'
        """);

        var repository = CreateRepository(connection);

        var result = await repository.GetByIdAsync(userId);

        Assert.Null(result);
    }

    // ============================================================
    // 5. GET BY USERNAME
    // ============================================================

    [Fact]
    public async Task GetByUserNameAsync_ShouldReturnUser_WhenUserNameExists()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var result = await repository.GetByUserNameAsync("TSTUSER001");

        Assert.NotNull(result);
        Assert.Equal("TSTUSER001", result!.UserName);
        Assert.Equal("tstuser001@test.com", result.Email);
        Assert.Equal("HASH001", result.PasswordHash);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("UserOne", result.LastName);
        Assert.Equal("Test User One", result.DisplayName);
        Assert.True(result.IsActive);
        Assert.False(result.IsDeleted);
    }

    // ============================================================
    // 6. GET BY USERNAME - NOT FOUND
    // ============================================================

    [Fact]
    public async Task GetByUserNameAsync_ShouldReturnNull_WhenUserNameDoesNotExist()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var result =
            await repository.GetByUserNameAsync("DOES_NOT_EXIST");

        Assert.Null(result);
    }

    // ============================================================
    // 7. GET BY EMAIL
    // ============================================================

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenEmailExists()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var result =
            await repository.GetByEmailAsync("tstuser001@test.com");

        Assert.NotNull(result);

        Assert.Equal("TSTUSER001", result!.UserName);
        Assert.Equal("tstuser001@test.com", result.Email);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("UserOne", result.LastName);
        Assert.Equal("Test User One", result.DisplayName);
        Assert.True(result.IsActive);
        Assert.False(result.IsDeleted);
    }

    // ============================================================
    // 8. GET BY EMAIL - NOT FOUND
    // ============================================================

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailDoesNotExist()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var result =
            await repository.GetByEmailAsync("doesnotexist@test.com");

        Assert.Null(result);
    }

    // ============================================================
    // 9. INSERT
    // ============================================================

    [Fact]
    public async Task InsertAsync_ShouldInsertUser()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var user = new UserEntity
        {
            UserName = "TSTUSER005",
            Email = "tstuser005@test.com",
            PasswordHash = "HASH005",
            FirstName = "Inserted",
            MiddleName = "Test",
            LastName = "User",
            DisplayName = "Inserted Test User",
            PhoneNumber = "9000000005",
            AlternatePhone = "9111111115",
            AddressLine1 = "Inserted Address",
            AddressLine2 = "Address 2",
            City = "Coimbatore",
            State = "Tamil Nadu",
            ZipCode = "641001",
            Country = "India",
            DateOfBirth = new DateTime(1995, 5, 5),
            IsActive = true,
            CreatedBy = "UNITTEST"
        };

        var newId = await repository.InsertAsync(user);

        Assert.True(newId > 0);

        var inserted = await connection.QuerySingleAsync<UserEntity>(
            """
        SELECT *
        FROM dbo.Users
        WHERE Id = @Id
        """,
            new { Id = newId });

        Assert.Equal("TSTUSER005", inserted.UserName);
        Assert.Equal("tstuser005@test.com", inserted.Email);
        Assert.Equal("HASH005", inserted.PasswordHash);
        Assert.Equal("Inserted", inserted.FirstName);
        Assert.Equal("User", inserted.LastName);
        Assert.Equal("Inserted Test User", inserted.DisplayName);
        Assert.Equal("Coimbatore", inserted.City);
        Assert.True(inserted.IsActive);
        Assert.False(inserted.IsDeleted);
        Assert.Equal("UNITTEST", inserted.CreatedBy);
        Assert.NotEqual(default, inserted.CreatedDate);

        await connection.ExecuteAsync(
            "DELETE FROM dbo.Users WHERE Id = @Id",
            new { Id = newId });
    }

    // ============================================================
    // 10. INSERT - DUPLICATE USERNAME
    // ============================================================

    [Fact]
    public async Task InsertAsync_ShouldThrow_WhenUsernameAlreadyExists()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var user = new UserEntity
        {
            UserName = "TSTUSER001",
            Email = "newemail@test.com",
            PasswordHash = "HASH999",
            FirstName = "Duplicate",
            LastName = "User",
            DisplayName = "Duplicate User",
            PhoneNumber = "9999999999",
            AddressLine1 = "Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            CreatedBy = "UNITTEST"
        };

        await Assert.ThrowsAsync<SqlException>(
            () => repository.InsertAsync(user));
    }

    // ============================================================
    // 11. INSERT - DUPLICATE EMAIL
    // ============================================================

    [Fact]
    public async Task InsertAsync_ShouldThrow_WhenEmailAlreadyExists()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var user = new UserEntity
        {
            UserName = "NEWUSERNAME",
            Email = "tstuser001@test.com",
            PasswordHash = "HASH999",
            FirstName = "Duplicate",
            LastName = "Email",
            DisplayName = "Duplicate Email",
            PhoneNumber = "9999999999",
            AddressLine1 = "Address",
            City = "Chennai",
            State = "Tamil Nadu",
            ZipCode = "600001",
            Country = "India",
            IsActive = true,
            CreatedBy = "UNITTEST"
        };

        await Assert.ThrowsAsync<SqlException>(
            () => repository.InsertAsync(user));
    }

    // ============================================================
    // 12. UPDATE
    // ============================================================

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var userId = await connection.ExecuteScalarAsync<int>(
            """
        SELECT Id
        FROM dbo.Users
        WHERE UserName = 'TSTUSER001'
        """);

        var repository = CreateRepository(connection);

        var user = new UserEntity
        {
            UserName = "TSTUSER001",
            Email = "updateduser@test.com",
            PasswordHash = "UPDATED_HASH",
            FirstName = "Updated",
            MiddleName = "Middle",
            LastName = "User",
            DisplayName = "Updated User",
            PhoneNumber = "9222222222",
            AlternatePhone = "9333333333",
            AddressLine1 = "Updated Address",
            AddressLine2 = "Updated Address 2",
            City = "Coimbatore",
            State = "Tamil Nadu",
            ZipCode = "641001",
            Country = "India",
            DateOfBirth = new DateTime(1990, 10, 10),
            IsActive = false,
            UpdatedBy = "UNITTEST"
        };

        var result = await repository.UpdateAsync(userId, user);

        // Repository currently returns ExecuteAsync result.
        // The actual database state is the source of truth.
        var updated = await connection.QuerySingleAsync<dynamic>(
            """
        SELECT
            UserName,
            Email,
            FirstName,
            MiddleName,
            LastName,
            DisplayName,
            PhoneNumber,
            AlternatePhone,
            AddressLine1,
            AddressLine2,
            City,
            State,
            ZipCode,
            Country,
            DateOfBirth,
            IsActive,
            IsDeleted,
            UpdatedBy,
            UpdatedDate
        FROM dbo.Users
        WHERE Id = @Id
        """,
            new { Id = userId });

        Assert.Equal("TSTUSER001", updated.UserName);
        Assert.Equal("updateduser@test.com", updated.Email);
        Assert.Equal("Updated", updated.FirstName);
        Assert.Equal("Middle", updated.MiddleName);
        Assert.Equal("User", updated.LastName);
        Assert.Equal("Updated User", updated.DisplayName);
        Assert.Equal("9222222222", updated.PhoneNumber);
        Assert.Equal("9333333333", updated.AlternatePhone);
        Assert.Equal("Updated Address", updated.AddressLine1);
        Assert.Equal("Updated Address 2", updated.AddressLine2);
        Assert.Equal("Coimbatore", updated.City);
        Assert.Equal("Tamil Nadu", updated.State);
        Assert.Equal("641001", updated.ZipCode);
        Assert.Equal("India", updated.Country);
        Assert.False(updated.IsActive);
        Assert.False(updated.IsDeleted);
        Assert.Equal("UNITTEST", updated.UpdatedBy);
        Assert.NotNull(updated.UpdatedDate);

        _ = result;
    }

    // ============================================================
    // 13. DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteUser()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var userId = await connection.ExecuteScalarAsync<int>(
            """
        SELECT Id
        FROM dbo.Users
        WHERE UserName = 'TSTUSER001'
        """);

        var repository = CreateRepository(connection);

        await repository.DeleteAsync(userId);

        var deleted = await connection.QuerySingleAsync<dynamic>(
            """
        SELECT
            IsActive,
            IsDeleted,
            UpdatedBy,
            UpdatedDate
        FROM dbo.Users
        WHERE Id = @Id
        """,
            new { Id = userId });

        Assert.False((bool)deleted.IsActive);
        Assert.True((bool)deleted.IsDeleted);

        // Current repository does not pass UpdatedBy,
        // so this value depends on the stored procedure execution.
        Assert.NotNull(deleted.UpdatedDate);
    }

    // ============================================================
    // 14. GET PAGED
    // ============================================================

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedUsers()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var filter = new Invoice.DTOs.UserFilterDto
        {
            UserName = "TSTUSER",
            PageNumber = 1,
            PageSize = 2
        };

        var result = await repository.GetPagedAsync(filter);

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());

        Assert.Contains(
            result.Data,
            x => x.UserName == "TSTUSER001");

        Assert.Contains(
            result.Data,
            x => x.UserName == "TSTUSER002");
    }

    // ============================================================
    // 15. PAGING - SECOND PAGE
    // ============================================================

    [Fact]
    public async Task GetPagedAsync_ShouldReturnSecondPage()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var filter = new Invoice.DTOs.UserFilterDto
        {
            UserName = "TSTUSER",
            PageNumber = 2,
            PageSize = 2
        };

        var result = await repository.GetPagedAsync(filter);

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalRecords);
        Assert.Single(result.Data);

        var user = result.Data.First();

        Assert.Equal("TSTUSER003", user.UserName);
    }

    // ============================================================
    // 16. PAGING - USERNAME FILTER
    // ============================================================

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByUserName()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var filter = new Invoice.DTOs.UserFilterDto
        {
            UserName = "TSTUSER002",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await repository.GetPagedAsync(filter);

        Assert.Equal(1, result.TotalRecords);
        Assert.Single(result.Data);

        Assert.Equal(
            "TSTUSER002",
            result.Data.First().UserName);
    }

    // ============================================================
    // 17. PAGING - EMAIL FILTER
    // ============================================================

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByEmail()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var filter = new Invoice.DTOs.UserFilterDto
        {
            Email = "tstuser003@test.com",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await repository.GetPagedAsync(filter);

        Assert.Equal(1, result.TotalRecords);
        Assert.Single(result.Data);

        Assert.Equal(
            "tstuser003@test.com",
            result.Data.First().Email);
    }

    // ============================================================
    // 18. PAGING - ACTIVE FILTER
    // ============================================================

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByActiveStatus()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var repository = CreateRepository(connection);

        var filter = new Invoice.DTOs.UserFilterDto
        {
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await repository.GetPagedAsync(filter);

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());

        Assert.All(
            result.Data,
            x => Assert.True(x.IsActive));
    }

    // ============================================================
    // 19. UPDATE LAST LOGIN
    // ============================================================

    [Fact]
    public async Task UpdateLastLoginAsync_ShouldUpdateLastLoginDate()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var userId = await connection.ExecuteScalarAsync<int>(
            """
        SELECT Id
        FROM dbo.Users
        WHERE UserName = 'TSTUSER001'
        """);

        var repository = CreateRepository(connection);

        await repository.UpdateLastLoginAsync(userId);

        var lastLoginDate = await connection.ExecuteScalarAsync<DateTime?>(
            """
        SELECT LastLoginDate
        FROM dbo.Users
        WHERE Id = @Id
        """,
            new { Id = userId });

        Assert.NotNull(lastLoginDate);
    }

    // ============================================================
    // 20. UPDATE LAST LOGIN - INACTIVE USER
    // ============================================================

    [Fact]
    public async Task UpdateLastLoginAsync_ShouldNotUpdateInactiveUser()
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await SeedTestDataAsync(connection);

        var userId = await connection.ExecuteScalarAsync<int>(
            """
        SELECT Id
        FROM dbo.Users
        WHERE UserName = 'TSTUSER003'
        """);

        var repository = CreateRepository(connection);

        await repository.UpdateLastLoginAsync(userId);

        var lastLoginDate = await connection.ExecuteScalarAsync<DateTime?>(
            """
        SELECT LastLoginDate
        FROM dbo.Users
        WHERE Id = @Id
        """,
            new { Id = userId });

        Assert.Null(lastLoginDate);
    }
}
