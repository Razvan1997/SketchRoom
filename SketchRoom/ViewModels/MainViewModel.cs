using Prism.Dialogs;
using Prism.Events;
using SketchRoom.Database;
using SketchRoom.Dialogs;
using SketchRoom.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TopBarModule.Events;
using WhiteBoardModule.Events;

namespace SketchRoom.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly IDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        public MainViewModel(IDialogService dialogService, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _regionManager = regionManager;
            eventAggregator.GetEvent<OpenSaveSketchDialogEvent>().Subscribe(OpenSaveSketchDialog);
            eventAggregator.GetEvent<OpenSettingsDialogEvent>().Subscribe(OpenSettingsDialog);
            var parameters = new NavigationParameters
                    {
                        { "IsHost", true },
                        { "IsParticipant", false },
                        { "SessionCode", null }
                    };

            eventAggregator.GetEvent<SpinnerEvent>().Subscribe(show =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (show)
                        LoadingOverlayHelper.Show();
                    else
                        LoadingOverlayHelper.Hide();
                });
            });

            _regionManager.RequestNavigate("ContentRegion", "WhiteBoardView");
        }

        public void OnLoaded()
        {
            ShowContinueDialog();

            var user = SecureStorage.LoadUser();
            if (user == null)
            {
                ShowRegistration();
            }
        }

        private void ShowContinueDialog()
        {
            var dialog = new ContinueDialog
            {
                Owner = Application.Current.MainWindow 
            };

            dialog.Show();
        }


        public void ShowRegistration()
        {
            var dialog = new RegistrationDialog();
            dialog.Show();
        }

        private void OpenSaveSketchDialog()
        {
            var dialog = new SaveSketchDialog();
            dialog.Show();
        }

        private void OpenSettingsDialog()
        {
            var dialog = new SettingsDialog();
            dialog.Show();
        }
    }
}
