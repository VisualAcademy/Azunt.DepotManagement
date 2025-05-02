using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Azunt.DepotManagement;

/// <summary>
/// DepotApp 의존성 주입 확장 메서드
/// </summary>
public static class DepotServicesRegistrationExtensions
{
    /// <summary>
    /// 선택 가능한 저장소 모드 정의
    /// </summary>
    public enum RepositoryMode
    {
        EfCore,
        Dapper,
        AdoNet
    }

    /// <summary>
    /// DepotApp 모듈의 서비스를 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="connectionString">기본 연결 문자열</param>
    /// <param name="mode">레포지토리 모드 (EfCore, Dapper, AdoNet)</param>
    /// <param name="dbContextLifetime">DbContext 수명 주기 (기본: Transient)</param>
    public static void AddDependencyInjectionContainerForDepotApp(
        this IServiceCollection services,
        string connectionString,
        RepositoryMode mode = RepositoryMode.EfCore,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Transient)
    {
        switch (mode)
        {
            case RepositoryMode.EfCore:
                // EF Core 방식 등록
                services.AddDbContext<DepotAppDbContext>(
                    options => options.UseSqlServer(connectionString),
                    dbContextLifetime);

                services.AddTransient<IDepotRepository, DepotRepository>();
                services.AddTransient<DepotAppDbContextFactory>();
                break;

            case RepositoryMode.Dapper:
                // Dapper 방식 등록
                services.AddTransient<IDepotRepository>(provider =>
                    new DepotRepositoryDapper(
                        connectionString,
                        provider.GetRequiredService<ILoggerFactory>()));
                break;

            case RepositoryMode.AdoNet:
                // ADO.NET 방식 등록
                services.AddTransient<IDepotRepository>(provider =>
                    new DepotRepositoryAdoNet(
                        connectionString,
                        provider.GetRequiredService<ILoggerFactory>()));
                break;

            default:
                throw new InvalidOperationException(
                    $"Invalid repository mode '{mode}'. Supported modes: EfCore, Dapper, AdoNet.");
        }
    }
}