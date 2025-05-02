using Azunt.DepotManagement;
using Azunt.Repositories;
using Dul.Articles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Azunt.DepotManagement;

/// <summary>
/// Depot 테이블에 대한 Entity Framework Core 기반 리포지토리 구현체입니다.
/// Blazor Server 회로 유지 이슈를 피하고, 멀티테넌트 연결 문자열 지원을 위해 팩터리 사용.
/// </summary>
public class DepotRepository : IDepotRepository
{
    private readonly DepotAppDbContextFactory _factory;
    private readonly ILogger<DepotRepository> _logger;
    private readonly string? _connectionString;

    /// <summary>
    /// 기본 생성자: 팩터리를 통해 기본 연결 문자열 사용
    /// </summary>
    public DepotRepository(
        DepotAppDbContextFactory factory,
        ILoggerFactory loggerFactory)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger<DepotRepository>();
    }

    /// <summary>
    /// 명시적 연결 문자열을 사용하는 생성자 (멀티테넌시 지원)
    /// </summary>
    public DepotRepository(
        DepotAppDbContextFactory factory,
        ILoggerFactory loggerFactory,
        string connectionString)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger<DepotRepository>();
        _connectionString = connectionString;
    }

    private DepotAppDbContext CreateContext() =>
        string.IsNullOrWhiteSpace(_connectionString)
            ? _factory.CreateDbContext()
            : _factory.CreateDbContext(_connectionString);

    public async Task<Depot> AddAsync(Depot model)
    {
        await using var context = CreateContext();
        model.CreatedAt = DateTime.UtcNow;
        context.Depots.Add(model);
        await context.SaveChangesAsync();
        return model;
    }

    public async Task<IEnumerable<Depot>> GetAllAsync()
    {
        await using var context = CreateContext();
        return await context.Depots
            .OrderByDescending(m => m.Id)
            .ToListAsync();
    }

    public async Task<Depot> GetByIdAsync(long id)
    {
        await using var context = CreateContext();
        return await context.Depots
                   .SingleOrDefaultAsync(m => m.Id == id)
               ?? new Depot();
    }

    public async Task<bool> UpdateAsync(Depot model)
    {
        await using var context = CreateContext();
        context.Attach(model);
        context.Entry(model).State = EntityState.Modified;
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        await using var context = CreateContext();
        var entity = await context.Depots.FindAsync(id);
        if (entity == null) return false;
        context.Depots.Remove(entity);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<ArticleSet<Depot, int>> GetArticlesAsync<TParentIdentifier>(
        int pageIndex,
        int pageSize,
        string searchField,
        string searchQuery,
        string sortOrder,
        TParentIdentifier parentIdentifier)
    {
        await using var context = CreateContext();
        var query = context.Depots.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(m => m.Name!.Contains(searchQuery));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.Id)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ArticleSet<Depot, int>(items, totalCount);
    }

    public async Task<ArticleSet<Depot, long>> GetByAsync<TParentIdentifier>(
        FilterOptions<TParentIdentifier> options)
    {
        await using var context = CreateContext();
        var query = context.Depots.AsQueryable();

        if (!string.IsNullOrEmpty(options.SearchQuery))
        {
            query = query.Where(m => m.Name!.Contains(options.SearchQuery));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.Id)
            .Skip(options.PageIndex * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync();

        return new ArticleSet<Depot, long>(items, totalCount);
    }
}
