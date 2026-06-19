using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.Views;

public partial class InvoicesView : UserControl
{
    public InvoicesView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is InvoicesViewModel vm)
                vm.SavePdfPathProvider = SavePdfAsync;
        };
    }

    private async System.Threading.Tasks.Task<string?> SavePdfAsync(string suggestedName)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return null;
        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedName,
            DefaultExtension = "pdf",
            FileTypeChoices = new[] { new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } }
        });
        return file?.Path.LocalPath;
    }

    private void OnSetInvoiceStatus(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: InvoiceStatus status } menuItem) return;
        if (DataContext is not InvoicesViewModel vm) return;
        if (menuItem.DataContext is InvoiceRow row) vm.Selected = row;
        vm.SetStatusCommand.Execute(status);
    }
}
