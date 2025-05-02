using Dul.Articles;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Azunt.DepotManagement;

public class DepotRepositoryAdoNet : IDepotRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DepotRepositoryAdoNet> _logger;

    public DepotRepositoryAdoNet(string connectionString, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _logger = loggerFactory.CreateLogger<DepotRepositoryAdoNet>();
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<Depot> AddAsync(Depot model)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Depots (Active, CreatedAt, CreatedBy, Name, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@Active, @CreatedAt, @CreatedBy, @Name, 0)";
        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        if (result == null)
        {
            throw new InvalidOperationException("Failed to insert Depot. No ID was returned.");
        }
        model.Id = (long)result;
        return model;
    }

    public async Task<IEnumerable<Depot>> GetAllAsync()
    {
        var result = new List<Depot>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Active, CreatedAt, CreatedBy, Name FROM Depots WHERE IsDeleted = 0 ORDER BY Id DESC";

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Depot
            {
                Id = reader.GetInt64(0),
                Active = reader.IsDBNull(1) ? (bool?)null : reader.GetBoolean(1),
                CreatedAt = reader.GetDateTimeOffset(2),
                CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return result;
    }

    public async Task<Depot> GetByIdAsync(long id)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Active, CreatedAt, CreatedBy, Name FROM Depots WHERE Id = @Id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Depot
            {
                Id = reader.GetInt64(0),
                Active = reader.IsDBNull(1) ? (bool?)null : reader.GetBoolean(1),
                CreatedAt = reader.GetDateTimeOffset(2),
                CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4)
            };
        }

        return new Depot();
    }

    public async Task<bool> UpdateAsync(Depot model)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Depots SET
                Active = @Active,
                Name = @Name
            WHERE Id = @Id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Id", model.Id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Depots SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<ArticleSet<Depot, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier)
    {
        var all = await GetAllAsync();
        var filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? all
            : all.Where(m => m.Name != null && m.Name.Contains(searchQuery)).ToList();

        var paged = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new ArticleSet<Depot, int>(paged, filtered.Count());
    }

    public async Task<ArticleSet<Depot, long>> GetAllAsync<TParentIdentifier>(FilterOptions<TParentIdentifier> options)
    {
        var all = await GetAllAsync();
        var filtered = all
            .Where(m => string.IsNullOrWhiteSpace(options.SearchQuery)
                     || (m.Name != null && m.Name.Contains(options.SearchQuery)))
            .ToList();

        var paged = filtered
            .Skip(options.PageIndex * options.PageSize)
            .Take(options.PageSize)
            .ToList();

        return new ArticleSet<Depot, long>(paged, filtered.Count);
    }
}
