using SketchRoom.Database;
using SketchRoom.Services;
using SketchRoom.Toolkit.Wpf.Controls;
using SketchRoom.Toolkit.Wpf.Services;
using SketchRoom.ViewModels;
using SketchRoom.Windows;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WalkthroughDemo;
using WhiteBoard.Core.Services.Interfaces;

namespace SketchRoom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WalkthroughManager _manager;
        private GlobalHotkeyService? _hotkeyService;
        public MainWindow()
        {
            InitializeComponent();
            //var uri = new Uri("pack://application:,,,/Resources/DarkCursor.cur");
            //this.Cursor = new Cursor(Application.GetResourceStream(uri).Stream);

            string description = WalkthroughTexts.ShapesControlDescription;
            Walkthrough.SetDescription(UserInteractions, description);
            Loaded += MainWindow_Loaded;
            this.Closing += Window_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var settings = SettingsStorage.Load();

            _hotkeyService = new GlobalHotkeyService(this);
            _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;
            _hotkeyService.RegisterHotkey("CTRL", settings.Hotkey2);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.OnLoaded();
            }

            //WalkthroughService.Start(this);
        }

        private void OnGlobalHotkeyPressed()
        {
            var settings = SettingsStorage.Load();

            var folder = string.IsNullOrWhiteSpace(settings.GhostPreviewPath)
                ? ""
                : settings.GhostPreviewPath;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var gallery = new SketchGalleryWindow(folder);
                gallery.ShowDialog();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotkeyService?.Dispose();
            base.OnClosed(e);
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true; // oprește închiderea temporar

            try
            {
                var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();

                if (string.IsNullOrWhiteSpace(tabService.GetFolderName()))
                    tabService.SetFolderName("UnnamedSketch_" + DateTime.Now.Ticks);

                var persistence = new WhiteBoardPersistenceService(tabService);
                await persistence.SaveAllTabsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la salvare: " + ex.Message);
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Closing -= Window_Closing;
                this.Close();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}