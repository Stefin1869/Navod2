using Navod2.App.ViewModels;
using System.IO;
using System.Windows;

namespace Navod2.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var first = files.FirstOrDefault(f =>
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            return ext is ".xml" or ".zip" or ".pdf";
        });
        if (first is null) return;

        if (DataContext is MainViewModel vm)
            await vm.LoadFromPathAsync(first);
    }
}
