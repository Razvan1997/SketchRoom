using SketchRoom.ViewModels;
using SketchRoom.Windows;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WalkthroughDemo;

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

            Loaded += MainWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hotkeyService = new GlobalHotkeyService(this);
            _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;
            _hotkeyService.RegisterHotkey();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.OnLoaded();
            }

            WalkthroughService.Start(this);
        }

        private void OnGlobalHotkeyPressed()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Sketches");
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
    }
}