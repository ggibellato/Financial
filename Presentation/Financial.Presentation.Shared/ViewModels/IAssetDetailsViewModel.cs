using System.Threading.Tasks;
using Financial.Application.DTOs;

namespace Financial.Presentation.Shared.ViewModels;

public interface IAssetDetailsViewModel
{
    void LoadAssetDetails(AssetDetailsDTO details);
    void Clear();
    Task EnsureTodayInfoLoadedAsync();
}
