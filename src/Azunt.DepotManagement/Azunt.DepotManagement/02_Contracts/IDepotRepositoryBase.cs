using Azunt.Repositories;

namespace Azunt.DepotManagement;

/// <summary>
/// 창고(Depot)에 대한 기본 CRUD 기능만 정의한 리포지토리 인터페이스입니다.
/// </summary>
public interface IDepotRepositoryBase : IRepositoryBase<Depot, long>
{
}
