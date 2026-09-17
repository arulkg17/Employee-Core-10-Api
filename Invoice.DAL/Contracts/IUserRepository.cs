using Invoice.Data.Entities;
using Invoice.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Invoice.DAL.Contracts;

public interface IUserRepository
{
    Task<IEnumerable<UserEntity>> GetAllAsync();
    Task<UserEntity?> GetByIdAsync(int id);
    Task<UserEntity?> GetByUserNameAsync(string userName);
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<int> InsertAsync(UserEntity user);
    Task<bool> UpdateAsync(int id, UserEntity user);
    Task<bool> DeleteAsync(int id);
    Task<PagedResultDto<UserEntity>> GetPagedAsync(UserFilterDto filter);
    Task<bool> UpdateLastLoginAsync(int id);
}