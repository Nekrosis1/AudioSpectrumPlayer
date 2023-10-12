using AudioSpectrumPlayer.ViewModels;
using Caliburn.Micro;
using System.Windows;

namespace AudioSpectrumPlayer
{
    public class Bootstrapper : BootstrapperBase
    {
        public Bootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewForAsync<ShellViewModel>();
            //base.OnStartup(sender, e);
        }
    }
}
