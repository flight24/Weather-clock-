using System.IO;
using System.Runtime;
using System.Windows;

namespace WeatherWidget;

public partial class App : Application
{
    public App()
    {
        var profileDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WeatherWidget");
        Directory.CreateDirectory(profileDir);
        ProfileOptimization.SetProfileRoot(profileDir);
        ProfileOptimization.StartProfile("Startup.profile");
    }
}