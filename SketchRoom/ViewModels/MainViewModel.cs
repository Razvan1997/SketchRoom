using Prism.Dialogs;
using SketchRoom.Database;
using SketchRoom.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SketchRoom.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private readonly IDialogService _dialogService;
        private readonly IRegionManager _regionManager;
        public MainViewModel(IDialogService dialogService, IRegionManager regionManager)
        {
            _dialogService = dialogService;
            _regionManager = regionManager;

            var parameters = new NavigationParameters
                    {
                        { "IsHost", true },
                        { "IsParticipant", false },
                        { "SessionCode", null }
                    };

            _regionManager.RequestNavigate("ContentRegion", "WhiteBoardView");
        }

        public void OnLoaded()
        {
            var user = SecureStorage.LoadUser();
            if (user == null)
            {
                ShowRegistration();
            }
        }


        public void ShowRegistration()
        {
            var dialog = new RegistrationDialog();
            dialog.Show();
        }
    }
}
