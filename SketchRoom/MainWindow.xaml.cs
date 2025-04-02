using SketchRoom.ViewModels;
using System.Windows;
using WalkthroughDemo;

namespace SketchRoom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WalkthroughManager _manager;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.OnLoaded();
            }

            WalkthroughService.Start(this);
        }
    }
}