using Prism.Ioc;
using SketchRoom.Dialogs;
using SketchRoom.Services;
using SketchRoom.ViewModels;
using System.Windows;

namespace SketchRoom
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            var window = new MainWindow
            {
                DataContext = Container.Resolve<MainViewModel>()
            };
            return window;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<MainViewModel>();

            ViewModelLocationProvider.Register<RegistrationDialog, RegistrationDialogViewModel>();

            containerRegistry.RegisterSingleton<WhiteboardHubClient>();
        }

        protected override void OnInitialized()
        {
            var mainWindow = (Window)Shell;
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<TopBarModule.Modules.TopBarModule>();
            moduleCatalog.AddModule<UsersInteractionsModule.Modules.UsersInteractionsModule>();
            moduleCatalog.AddModule<WhiteBoardModule.Modules.WhiteBoardModule>();
            moduleCatalog.AddModule<LobbyHostingModule.Modules.LobbyHostingModule>();
            moduleCatalog.AddModule<ParticipationModule.Modules.ParticipationModule>();
        }
    }
}
