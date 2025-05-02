using Microsoft.AspNetCore.Components;
using Azunt.DepotManagement;

namespace Azunt.Web.Components.Pages.Depots.Components;

public partial class ModalForm : ComponentBase
{
    #region Properties
    /// <summary>
    /// (�۾���/�ۼ���)��� ���̾�α׸� ǥ���Ұ��� ���� 
    /// </summary>
    public bool IsShow { get; set; } = false;
    #endregion

    #region Public Methods
    /// <summary>
    /// �� ���̱� 
    /// </summary>
    public void Show() => IsShow = true;

    /// <summary>
    /// �� �ݱ�
    /// </summary>
    public void Hide()
    {
        IsShow = false;
        StateHasChanged(); // ���� ���� ����
    }
    #endregion

    #region Parameters

    [Parameter]
    public string UserName { get; set; } = "";

    /// <summary>
    /// ���� ���� ����
    /// </summary>
    [Parameter]
    public RenderFragment EditorFormTitle { get; set; } = null!;

    /// <summary>
    /// �θ𿡼� ���޹��� ��
    /// </summary>
    [Parameter]
    public Depot ModelSender { get; set; } = null!;

    /// <summary>
    /// ���������� ���ο��� ����ϴ� ��
    /// </summary>
    public Depot ModelEdit { get; set; } = null!;

    /// <summary>
    /// ���� �Ϸ� �� ȣ��� �ݹ� (Action)
    /// </summary>
    [Parameter]
    public Action CreateCallback { get; set; } = null!;

    /// <summary>
    /// ���� �Ϸ� �� ȣ��� �ݹ� (EventCallback)
    /// </summary>
    [Parameter]
    public EventCallback<bool> EditCallback { get; set; }

    [Parameter]
    public string ParentKey { get; set; } = "";

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// �Ķ���� ���� �� �� ����
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
                // �ʿ��� �ʵ� �߰� ����
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
    /// �������丮 Ŭ������ ���� ���� 
    /// </summary>
    [Inject]
    public IDepotRepository RepositoryReference { get; set; } = null!;

    #endregion

    #region Event Handlers

    /// <summary>
    /// Submit ��ư Ŭ�� �� Create �Ǵ� Edit ���� ����
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