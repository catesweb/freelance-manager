using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FreelanceManager.App.ViewModels;

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
}
