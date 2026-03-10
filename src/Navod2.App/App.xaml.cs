using Navod2.App.ViewModels;
using Navod2.Core.Services;
using System.IO;
using System.Windows;

namespace Navod2.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Navod2");
        Directory.CreateDirectory(dataDir);

        var forbiddenWordService = new ForbiddenWordService(dataDir);
        var grammarCheckService = new GrammarCheckService();
        var mainVm = new MainViewModel(forbiddenWordService, grammarCheckService);

        var window = new MainWindow { DataContext = mainVm };
        window.Show();
    }
}
