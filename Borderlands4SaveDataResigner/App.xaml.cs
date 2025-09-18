using System.Windows;

namespace Borderlands4SaveDataResigner;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Check if command line arguments are provided for console mode
        if (e.Args.Length > 0)
        {
            // Run console mode and exit
            var exitCode = Program.RunConsoleMode(e.Args);
            Shutdown(exitCode);
            return;
        }
        
        // Continue with normal WPF startup
        base.OnStartup(e);
    }
}