using Invoice.BAL.Contracts;
using Invoice.DAL.Contracts;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.AspNetCore.Identity;

namespace Invoice.BAL.Services;

public class UserServiceSpDap : IUserService
{
    private readonly IUserRepository _repository;

    private readonly PasswordHasher<UserEntity> _passwordHasher;

    public UserServiceSpDap(
        IUserRepository repository)
    {
        _repository = repository;

        _passwordHasher =
            new PasswordHasher<UserEntity>();
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync()
    {
        var users = await _repository.GetAllAsync();

        var result = users.Select(MapToDto);

        return new ApiResponse<IEnumerable<UserDto>>
        {
            Success = true,
            Message = "Users retrieved successfully.",
            Data = result
        };
    }

    public async Task<ApiResponse<UserDto>> GetByIdAsync(
        int id)
    {
        var user = await _repository.GetByIdAsync(id);

        if (user == null)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "User not found."
            };
        }

        return new ApiResponse<UserDto>
        {
            Success = true,
            Message = "Users retrieved successfully.",
            Data = MapToDto(user)
        };
    }

    public async Task<ApiResponse<UserDto>> CreateAsync(
        UserCreateDto dto)
    {
        var existingUser =
            await _repository.GetByUserNameAsync(dto.UserName);

        if (existingUser != null)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Username already exists."
            };
        }

        var existingEmail =
            await _repository.GetByEmailAsync(dto.Email);

        if (existingEmail != null)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Email already exists."
            };
        }

        var entity = new UserEntity
        {
            UserName = dto.UserName,
            Email = dto.Email,
            FirstName = dto.FirstName,
            MiddleName = dto.MiddleName,
            LastName = dto.LastName,
            DisplayName = dto.DisplayName,
            PhoneNumber = dto.PhoneNumber,
            AlternatePhone = dto.AlternatePhone,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            State = dto.State,
            ZipCode = dto.ZipCode,
            Country = dto.Country,
            DateOfBirth = dto.DateOfBirth,
            IsActive = dto.IsActive,
            IsDeleted = false,
            CreatedBy = dto.CreatedBy,
            CreatedDate = DateTime.UtcNow
        };

        entity.PasswordHash =
            _passwordHasher.HashPassword(
                entity,
                dto.Password);

        var id =
            await _repository.InsertAsync(entity);

        entity.Id = id;

        return new ApiResponse<UserDto>
        {
            Success = true,
            Message = "User created successfully.",
            Data = MapToDto(entity)
        };
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(
        int id,
        UserUpdateDto dto)
    {
        var existing =
            await _repository.GetByIdAsync(id);

        if (existing == null)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "User not found."
            };
        }

        existing.UserName = dto.UserName;
        existing.Email = dto.Email;
        existing.FirstName = dto.FirstName;
        existing.MiddleName = dto.MiddleName;
        existing.LastName = dto.LastName;
        existing.DisplayName = dto.DisplayName;
        existing.PhoneNumber = dto.PhoneNumber;
        existing.AlternatePhone = dto.AlternatePhone;
        existing.AddressLine1 = dto.AddressLine1;
        existing.AddressLine2 = dto.AddressLine2;
        existing.City = dto.City;
        existing.State = dto.State;
        existing.ZipCode = dto.ZipCode;
        existing.Country = dto.Country;
        existing.DateOfBirth = dto.DateOfBirth;
        existing.IsActive = dto.IsActive;
        existing.UpdatedBy = dto.UpdatedBy;
        existing.UpdatedDate = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            existing.PasswordHash =
                _passwordHasher.HashPassword(
                    existing,
                    dto.Password);
        }

        var updated =
            await _repository.UpdateAsync(id, existing);

        if (!updated)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Unable to update user."
            };
        }

        existing.Id = id;

        return new ApiResponse<UserDto>
        {
            Success = true,
            Message = "User updated successfully.",
            Data = MapToDto(existing)
        };
    }

    public async Task<ApiResponse<bool>> DeleteAsync(
        int id)
    {
        var user =
            await _repository.GetByIdAsync(id);

        if (user == null)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "User not found."
            };
        }

        var result =
            await _repository.DeleteAsync(id);

        return new ApiResponse<bool>
        {
            Success = result,
            Message = result
                ? "User deleted successfully."
                : "Unable to delete user.",
            Data = result
        };
    }

    public async Task<ApiResponse<PagedResultDto<UserDto>>>
     GetPagedAsync(UserFilterDto filter)
    {
        var result =
            await _repository.GetPagedAsync(filter);

        return new ApiResponse<PagedResultDto<UserDto>>
        {
            Success = true,
            Message = "Users retrieved successfully.",
            Data = new PagedResultDto<UserDto>
            {
                Data = result.Data
                    .Select(MapToDto)
                    .ToList(),

                TotalRecords = result.TotalRecords
            }
        };
    }

    public async Task<UserDto?> ValidateUserAsync(
        string userName,
        string password)
    {
        var user =
            await _repository.GetByUserNameAsync(userName);

        if (user == null)
            return null;

        if (!user.IsActive || user.IsDeleted)
            return null;

        var verification =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

        if (verification ==
            PasswordVerificationResult.Failed)
        {
            return null;
        }

        await _repository.UpdateLastLoginAsync(user.Id);

        return MapToDto(user);
    }

    private static UserDto MapToDto(
        UserEntity entity)
    {
        return new UserDto
        {
            Id = entity.Id,
            UserName = entity.UserName,
            Email = entity.Email,
            FirstName = entity.FirstName,
            MiddleName = entity.MiddleName,
            LastName = entity.LastName,
            DisplayName = entity.DisplayName,
            PhoneNumber = entity.PhoneNumber,
            AlternatePhone = entity.AlternatePhone,
            AddressLine1 = entity.AddressLine1,
            AddressLine2 = entity.AddressLine2,
            City = entity.City,
            State = entity.State,
            ZipCode = entity.ZipCode,
            Country = entity.Country,
            DateOfBirth = entity.DateOfBirth,
            IsActive = entity.IsActive,
            LastLoginDate = entity.LastLoginDate
        };
    }
}