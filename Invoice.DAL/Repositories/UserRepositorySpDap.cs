using Dapper;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using System.Data;

namespace Invoice.DAL.Repositories;

public class UserRepositorySpDap : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepositorySpDap(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync()
    {
        return await _connection.QueryAsync<UserEntity>(
            "dbo.sp_User_GetAll",
            commandType: CommandType.StoredProcedure);
    }

    public async Task<UserEntity?> GetByIdAsync(int id)
    {
        return await _connection.QueryFirstOrDefaultAsync<UserEntity>(
            "dbo.sp_User_GetById",
            new
            {
                Id = id
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<UserEntity?> GetByUserNameAsync(
        string userName)
    {
        return await _connection.QueryFirstOrDefaultAsync<UserEntity>(
            "dbo.sp_User_GetByUserName",
            new
            {
                UserName = userName
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<UserEntity?> GetByEmailAsync(
        string email)
    {
        return await _connection.QueryFirstOrDefaultAsync<UserEntity>(
            "dbo.sp_User_GetByEmail",
            new
            {
                Email = email
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> InsertAsync(UserEntity user)
    {
        var parameters = new
        {
            user.UserName,
            user.Email,
            user.PasswordHash,
            user.FirstName,
            user.MiddleName,
            user.LastName,
            user.DisplayName,
            user.PhoneNumber,
            user.AlternatePhone,
            user.AddressLine1,
            user.AddressLine2,
            user.City,
            user.State,
            user.ZipCode,
            user.Country,
            user.DateOfBirth,
            user.IsActive,
            user.CreatedBy
        };

        return await _connection.ExecuteScalarAsync<int>(
            "dbo.sp_User_Insert",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> UpdateAsync(
        int id,
        UserEntity user)
    {
        var parameters = new DynamicParameters();

        parameters.Add("Id", id);
        parameters.Add("UserName", user.UserName);
        parameters.Add("Email", user.Email);
        parameters.Add("PasswordHash", user.PasswordHash);
        parameters.Add("FirstName", user.FirstName);
        parameters.Add("MiddleName", user.MiddleName);
        parameters.Add("LastName", user.LastName);
        parameters.Add("DisplayName", user.DisplayName);
        parameters.Add("PhoneNumber", user.PhoneNumber);
        parameters.Add("AlternatePhone", user.AlternatePhone);
        parameters.Add("AddressLine1", user.AddressLine1);
        parameters.Add("AddressLine2", user.AddressLine2);
        parameters.Add("City", user.City);
        parameters.Add("State", user.State);
        parameters.Add("ZipCode", user.ZipCode);
        parameters.Add("Country", user.Country);
        parameters.Add("DateOfBirth", user.DateOfBirth);
        parameters.Add("IsActive", user.IsActive);
        parameters.Add("UpdatedBy", user.UpdatedBy);

        var result = await _connection.ExecuteAsync(
            "dbo.sp_User_Update",
            parameters,
            commandType: CommandType.StoredProcedure);

        return result > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var result = await _connection.ExecuteAsync(
            "dbo.sp_User_Delete",
            new
            {
                Id = id
            },
            commandType: CommandType.StoredProcedure);

        return result > 0;
    }

    public async Task<PagedResultDto<UserEntity>> GetPagedAsync(
        UserFilterDto filter)
    {
        using var multi = await _connection.QueryMultipleAsync(
            "dbo.sp_User_GetPaged",
            new
            {
                UserName = filter.UserName,
                Email = filter.Email,
                FirstName = filter.FirstName,
                LastName = filter.LastName,
                IsActive = filter.IsActive,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            },
            commandType: CommandType.StoredProcedure);

        var data = (await multi.ReadAsync<UserEntity>()).ToList();

        var totalRecords =
            await multi.ReadFirstOrDefaultAsync<int>();

        return new PagedResultDto<UserEntity>
        {
            Data = data,
            TotalRecords = totalRecords
        };
    }

    public async Task<bool> UpdateLastLoginAsync(int id)
    {
        var result = await _connection.ExecuteAsync(
            "dbo.sp_User_UpdateLastLogin",
            new
            {
                Id = id
            },
            commandType: CommandType.StoredProcedure);

        return result > 0;
    }
}
