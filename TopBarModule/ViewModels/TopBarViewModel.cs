using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TopBarModule.Events;

namespace TopBarModule.ViewModels
{
    public class TopBarViewModel : BindableBase
    {
        private IEventAggregator _eventAggregator;
        private IRegionManager _regionManager;

        public ICommand CloseCommand { get; }
        public ICommand MinimizeCommand { get; }
        public ICommand MaximizeCommand { get; }
        public DelegateCommand SettingsCommand { get; }

        public TopBarViewModel(IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;
            //_eventAggregator.GetEvent<ContentEvents>().Subscribe(GetContentHeader);

            CloseCommand = new DelegateCommand(OnClose);
            MinimizeCommand = new DelegateCommand(OnMinimize);
            MaximizeCommand = new DelegateCommand(OnMaximize);
            SettingsCommand = new DelegateCommand(OpenSettings);
        }

        private void OnClose()
        {
            Application.Current.MainWindow?.Close();
        }

        private void OnMinimize()
        {
            Application.Current.MainWindow!.WindowState = WindowState.Minimized;
        }

        private void OnMaximize()
        {
            var window = Application.Current.MainWindow!;
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        private void OpenSettings()
        {
            _eventAggregator.GetEvent<OpenSettingsDialogEvent>().Publish();
        }
    }
}
