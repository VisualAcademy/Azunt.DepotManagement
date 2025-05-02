using Dapper;
using Dul.Articles;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.DepotManagement;

public class DepotRepositoryDapper : IDepotRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DepotRepositoryDapper> _logger;

    public DepotRepositoryDapper(string connectionString, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _logger = loggerFactory.CreateLogger<DepotRepositoryDapper>();
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<Depot> AddAsync(Depot model)
    {
        const string sql = @"
            INSERT INTO Depots (Active, CreatedAt, CreatedBy, Name, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@Active, @CreatedAt, @CreatedBy, @Name, 0)";

        model.CreatedAt = DateTimeOffset.UtcNow;

        using var conn = GetConnection();
        model.Id = await conn.ExecuteScalarAsync<long>(sql, model);
        return model;
    }

    public async Task<IEnumerable<Depot>> GetAllAsync()
    {
        const string sql = @"
            SELECT Id, Active, CreatedAt, CreatedBy, Name 
            FROM Depots 
            WHERE IsDeleted = 0 
            ORDER BY Id DESC";

        using var conn = GetConnection();
        return await conn.QueryAsync<Depot>(sql);
    }

    public async Task<Depot> GetByIdAsync(long id)
    {
        const string sql = @"
            SELECT Id, Active, CreatedAt, CreatedBy, Name 
            FROM Depots 
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        return await conn.QuerySingleOrDefaultAsync<Depot>(sql, new { Id = id }) ?? new Depot();
    }

    public async Task<bool> UpdateAsync(Depot model)
    {
        const string sql = @"
            UPDATE Depots SET
                Active = @Active,
                Name = @Name
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        var affected = await conn.ExecuteAsync(sql, model);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        const string sql = @"
            UPDATE Depots SET IsDeleted = 1 
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        var affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<ArticleSet<Depot, int>> GetArticlesAsync<TParentIdentifier>(
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

    public async Task<ArticleSet<Depot, long>> GetByAsync<TParentIdentifier>(FilterOptions<TParentIdentifier> options)
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
