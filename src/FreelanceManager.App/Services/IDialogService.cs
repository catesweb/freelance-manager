using System.Threading.Tasks;

namespace FreelanceManager.App.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel");
    Task<bool> ShowDialogAsync(object viewModel);   // resolves true if the dialog saved/accepted
}
