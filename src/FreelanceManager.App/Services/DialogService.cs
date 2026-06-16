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

    public Task<bool> ShowDialogAsync(object viewModel)
        => throw new NotSupportedException("Implemented in a later task.");
}
