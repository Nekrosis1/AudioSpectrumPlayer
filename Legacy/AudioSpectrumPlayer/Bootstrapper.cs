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

        protected override async void OnStartup(object sender, StartupEventArgs e)
        {
            //DisplayRootViewForAsync<ShellViewModel>();
            await DisplayRootViewForAsync(typeof(ShellViewModel));
            //base.OnStartup(sender, e);
        }
    }
}
