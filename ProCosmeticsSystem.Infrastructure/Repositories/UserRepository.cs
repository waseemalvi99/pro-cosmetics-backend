using Dapper;
using ProCosmeticsSystem.Application.DTOs.Auth;
using ProCosmeticsSystem.Application.DTOs.Common;
using ProCosmeticsSystem.Application.Interfaces;
using ProCosmeticsSystem.Infrastructure.Persistence;

namespace ProCosmeticsSystem.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _db;

    public UserRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search)
    {
        using var conn = _db.CreateConnection();

        var whereClause = string.IsNullOrWhiteSpace(search)
            ? ""
            : "WHERE u.FullName LIKE @Search OR u.Email LIKE @Search";

        var countSql = $"SELECT COUNT(*) FROM AspNetUsers u {whereClause}";
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { Search = $"%{search}%" });

        var sql = $@"SELECT u.Id, u.FullName, u.Email, u.IsActive, u.CreatedAt
                     FROM AspNetUsers u
                     {whereClause}
                     ORDER BY u.CreatedAt DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var users = await conn.QueryAsync<UserDto>(sql, new
        {
            Search = $"%{search}%",
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        return new PagedResult<UserDto>
        {
            Items = users.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UserDto>(
            "SELECT Id, FullName, Email, IsActive, CreatedAt FROM AspNetUsers WHERE Id = @Id",
            new { Id = userId });
    }
}
