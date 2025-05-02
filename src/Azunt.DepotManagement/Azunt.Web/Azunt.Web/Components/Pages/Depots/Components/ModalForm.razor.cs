using Microsoft.AspNetCore.Components;
using Azunt.DepotManagement;

namespace Azunt.Web.Components.Pages.Depots.Components;

public partial class ModalForm : ComponentBase
{
    #region Properties
    /// <summary>
    /// (글쓰기/글수정)모달 다이얼로그를 표시할건지 여부 
    /// </summary>
    public bool IsShow { get; set; } = false;
    #endregion

    #region Public Methods
    /// <summary>
    /// 폼 보이기 
    /// </summary>
    public void Show() => IsShow = true;

    /// <summary>
    /// 폼 닫기
    /// </summary>
    public void Hide()
    {
        IsShow = false;
        StateHasChanged(); // 상태 강제 갱신
    }
    #endregion

    #region Parameters

    [Parameter]
    public string UserName { get; set; } = "";

    /// <summary>
    /// 폼의 제목 영역
    /// </summary>
    [Parameter]
    public RenderFragment EditorFormTitle { get; set; } = null!;

    /// <summary>
    /// 부모에서 전달받은 모델
    /// </summary>
    [Parameter]
    public Depot ModelSender { get; set; } = null!;

    /// <summary>
    /// 수정용으로 내부에서 사용하는 모델
    /// </summary>
    public Depot ModelEdit { get; set; } = null!;

    /// <summary>
    /// 생성 완료 시 호출될 콜백 (Action)
    /// </summary>
    [Parameter]
    public Action CreateCallback { get; set; } = null!;

    /// <summary>
    /// 수정 완료 시 호출될 콜백 (EventCallback)
    /// </summary>
    [Parameter]
    public EventCallback<bool> EditCallback { get; set; }

    [Parameter]
    public string ParentKey { get; set; } = "";

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// 파라미터 전달 시 모델 복사
    /// </summary>
    protected override void OnParametersSet()
    {
        if (ModelSender != null)
        {
            ModelEdit = new Depot
            {
                Id = ModelSender.Id,
                Name = ModelSender.Name,
                Active = ModelSender.Active,
                CreatedAt = ModelSender.CreatedAt,
                CreatedBy = ModelSender.CreatedBy
                // 필요한 필드 추가 복사
            };
        }
        else
        {
            ModelEdit = new Depot();
        }
    }

    #endregion

    #region Injectors

    /// <summary>
    /// 리포지토리 클래스에 대한 참조 
    /// </summary>
    [Inject]
    public IDepotRepository RepositoryReference { get; set; } = null!;

    #endregion

    #region Event Handlers

    /// <summary>
    /// Submit 버튼 클릭 시 Create 또는 Edit 동작 수행
    /// </summary>
    protected async void CreateOrEditClick()
    {
        ModelSender.Active = true;
        ModelSender.Name = ModelEdit.Name;
        ModelSender.CreatedBy = UserName ?? "Anonymous";

        if (ModelSender.Id == 0)
        {
            // Create
            ModelSender.CreatedAt = DateTime.UtcNow;
            await RepositoryReference.AddAsync(ModelSender);
            CreateCallback?.Invoke();
        }
        else
        {
            // Edit
            await RepositoryReference.UpdateAsync(ModelSender);
            await EditCallback.InvokeAsync(true);
        }
    }

    #endregion
}