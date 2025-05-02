using Dul.Articles;

namespace Azunt.DepotManagement;

/// <summary>
/// 창고(Depot)에 대한 CRUD + 검색/페이징 기능이 포함된 고급 리포지토리 인터페이스입니다.
/// </summary>
public interface IDepotRepository : IDepotBaseRepository
{
    /// <summary>
    /// 검색 필드, 쿼리, 정렬 기준을 기준으로 페이징된 결과 반환
    /// </summary>
    /// <typeparam name="TParentIdentifier">부모 식별자 타입</typeparam>
    Task<ArticleSet<Depot, int>> GetArticlesAsync<TParentIdentifier>(
        int pageIndex,
        int pageSize,
        string searchField,
        string searchQuery,
        string sortOrder,
        TParentIdentifier parentIdentifier);

    /// <summary>
    /// FilterOptions 기반 페이징 처리된 결과 반환
    /// </summary>
    /// <typeparam name="TParentIdentifier">부모 식별자 타입</typeparam>
    Task<ArticleSet<Depot, long>> GetByAsync<TParentIdentifier>(
        FilterOptions<TParentIdentifier> options);
}