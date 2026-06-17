using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FreelanceManager.App.Views.Dialogs;

namespace FreelanceManager.App.Services;

public class DialogService : IDialogService
{
    private Window? Owner =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public async Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        if (Owner is null) return false;
        var dlg = new ConfirmDialog
        {
            TitleText = title, MessageText = message,
            ConfirmText = confirmText, CancelText = cancelText
        };
        return await dlg.ShowDialog<bool>(Owner);
    }

    public async Task<bool> ShowDialogAsync(object viewModel)
    {
        if (Owner is null) return false;
        Window dlg = viewModel switch
        {
            ViewModels.ClientEditViewModel => new ClientEditDialog(),
            _ => throw new System.NotSupportedException($"No dialog for {viewModel.GetType().Name}")
        };
        dlg.DataContext = viewModel;
        return await dlg.ShowDialog<bool>(Owner);
    }
}
