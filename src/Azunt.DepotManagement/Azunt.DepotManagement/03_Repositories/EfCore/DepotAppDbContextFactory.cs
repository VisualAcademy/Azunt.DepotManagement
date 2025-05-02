using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.DepotManagement;

public class DepotAppDbContextFactory
{
    private readonly IConfiguration? _configuration;

    public DepotAppDbContextFactory() { }

    public DepotAppDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DepotAppDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DepotAppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new DepotAppDbContext(options);
    }

    public DepotAppDbContext CreateDbContext(DbContextOptions<DepotAppDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new DepotAppDbContext(options);
    }

    public DepotAppDbContext CreateDbContext()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not provided.");
        }

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("DefaultConnection is not configured properly.");
        }

        return CreateDbContext(defaultConnection);
    }
}