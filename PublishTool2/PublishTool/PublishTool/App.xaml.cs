using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace PublishTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            EnsureAdmin();

            base.OnStartup(e);
        }
        public static void EnsureAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath,
                    Verb = "runas",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                Environment.Exit(0);
            }
            
        }

    }

}
