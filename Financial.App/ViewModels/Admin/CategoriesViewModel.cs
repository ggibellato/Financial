using System.Collections.ObjectModel;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.Presentation.App.Services;
using Microsoft.Extensions.Logging;
using static Financial.Presentation.App.Helpers.ObservableCollectionHelper;

namespace Financial.Presentation.App.ViewModels.Admin;

public class CategoriesViewModel : ViewModelBase
{
    private readonly ICategoryService _categoryService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<CategoriesViewModel> _logger;

    private bool _isLoading = true;
    private string? _error;
    private string? _actionError;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ShowContent));
            }
        }
    }

    public bool HasError => Error != null;

    public bool ShowContent => !IsLoading && !HasError;

    public string? ActionError
    {
        get => _actionError;
        private set => SetProperty(ref _actionError, value);
    }

    public ObservableCollection<CategoryDTO> Categories { get; } = [];

    public RelayCommand RetryCommand { get; }

    public RelayCommand CreateCategoryCommand { get; }

    public RelayCommand<CategoryDTO> EditCategoryCommand { get; }

    public RelayCommand<CategoryDTO> DeleteCategoryCommand { get; }

    public CategoriesViewModel(ICategoryService categoryService, IDialogService dialogService, ILogger<CategoriesViewModel> logger)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RetryCommand = new RelayCommand(async () => await RefreshAsync());
        CreateCategoryCommand = new RelayCommand(async () => await CreateCategoryAsync());
        EditCategoryCommand = new RelayCommand<CategoryDTO>(async category => await EditCategoryAsync(category));
        DeleteCategoryCommand = new RelayCommand<CategoryDTO>(async category => await DeleteCategoryAsync(category));

        _ = RefreshAsync();
    }

    private int _refreshRequestId;

    internal Task RefreshAsync() => ExecuteRefreshAsync(
        () => ++_refreshRequestId,
        id => id == _refreshRequestId,
        loading => IsLoading = loading,
        error => Error = error,
        async isCurrent =>
        {
            var categories = await Task.Run(() => _categoryService.GetCategories());

            if (!isCurrent())
            {
                return;
            }

            ReplaceAll(Categories, categories);
        },
        ex => _logger.LogError("Categories refresh failed with {ErrorType}", ex.GetType().Name));

    internal async Task CreateCategoryAsync()
    {
        var dialog = new CategoryFormDialogViewModel();
        if (!_dialogService.ShowCategoryFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _categoryService.CreateCategoryAsync(new CategoryCreateDTO
            {
                Name = dialog.Name,
                Active = dialog.Active,
                IsInvestment = dialog.IsInvestment,
                IsTithe = dialog.IsTithe,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Category create failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task EditCategoryAsync(CategoryDTO? category)
    {
        if (category is null)
        {
            return;
        }

        var dialog = new CategoryFormDialogViewModel(category.Name, category.Active, category.IsInvestment, category.IsTithe);
        if (!_dialogService.ShowCategoryFormDialog(dialog))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _categoryService.UpdateCategoryAsync(category.Id, new CategoryUpdateDTO
            {
                Name = dialog.Name,
                Active = dialog.Active,
                IsInvestment = dialog.IsInvestment,
                IsTithe = dialog.IsTithe,
            });
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Category update failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }

    internal async Task DeleteCategoryAsync(CategoryDTO? category)
    {
        if (category is null)
        {
            return;
        }

        if (category.HasReferences)
        {
            ActionError = $"\"{category.Name}\" is still used by a transaction and cannot be deleted.";
            return;
        }

        if (!_dialogService.Confirm($"\"{category.Name}\" will be permanently removed. Continue?", "Delete Category"))
        {
            return;
        }

        ActionError = null;
        try
        {
            await _categoryService.DeleteCategoryAsync(category.Id);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Category delete failed with {ErrorType}", ex.GetType().Name);
            ActionError = ex.Message;
        }
    }
}
