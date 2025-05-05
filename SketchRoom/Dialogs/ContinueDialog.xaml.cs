using SketchRoom.Toolkit.Wpf.Controls;
using SketchRoom.Toolkit.Wpf.Services;
using SketchRoom.Windows;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;
using WhiteBoardModule;
using WhiteBoardModule.Events;
using WhiteBoardModule.ViewModels;
using WhiteBoardModule.Views;

namespace SketchRoom.Dialogs
{
    /// <summary>
    /// Interaction logic for ContinueDialog.xaml
    /// </summary>
    public partial class ContinueDialog : Window
    {
        private List<StackPreviewItem> _allTabs = new();
        private int _currentIndex = 0;
        private bool _hasSketches = false;
        public ContinueDialog()
        {
            InitializeComponent();
            LoadPreviews();
        }

        private async void LoadPreviews()
        {
            var service = new WhiteBoardPersistenceService(ContainerLocator.Container.Resolve<IWhiteBoardTabService>());
            var savedTabs = await service.LoadAllTabsAsync();

            var groupedByFolder = savedTabs
                .GroupBy(t => t.FolderName)
                .Select(g => g.First());

            foreach (var tab in groupedByFolder)
            {
                _allTabs.Add(new StackPreviewItem
                {
                    TabName = tab.FolderName,
                    Thumbnail = LoadThumbnailImage(tab.FolderName),
                    Shapes = tab.Shapes,
                    FolderName = tab.FolderName
                });
            }

            _hasSketches = _allTabs.Any();
            PreviewStack.ItemsSource = _allTabs;

            WelcomePanel.Visibility = _hasSketches ? Visibility.Collapsed : Visibility.Visible;
            SketchListPanel.Visibility = _hasSketches ? Visibility.Visible : Visibility.Collapsed;
        }

        private ImageSource LoadThumbnailImage(string folderName)
        {
            var folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SketchRoom", "SavedTabs", folderName);

            var thumbPath = Path.Combine(folderPath, "thumbnail.png");

            if (!File.Exists(thumbPath))
            {
                // Imagine fallback din resurse
                return new BitmapImage(new Uri("pack://application:,,,/SketchRoom;component/Resources/placeholder.png"));
            }

            // Încărcare non-locking
            var bitmap = new BitmapImage();
            using (var stream = new FileStream(thumbPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze(); // important pentru performanță și evitarea blocajelor
            return bitmap;
        }

        private async void Preview_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is StackPreviewItem selectedItem)
            {
                var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
                tabService.SetFolderName(selectedItem.FolderName);

                var folderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SketchRoom", "SavedTabs", selectedItem.FolderName);

                var jsonFiles = Directory.GetFiles(folderPath, "tab_*.json");
                Array.Sort(jsonFiles); // ordonează pentru consistență

                var restoredTabs = new List<SavedWhiteBoardModel>();

                foreach (var jsonFile in jsonFiles)
                {
                    var json = await File.ReadAllTextAsync(jsonFile);
                    var model = JsonSerializer.Deserialize<SavedWhiteBoardModel>(json);
                    if (model != null)
                        restoredTabs.Add(model);
                }

                if (restoredTabs.Count > 0)
                {
                    // Trimite tab-urile la FooterViewModel
                    var eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
                    eventAggregator.GetEvent<TabsRestoredEvent>().Publish(restoredTabs);
                }

                this.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            string name = null;

            if (WelcomePanel.Visibility == Visibility.Visible)
                name = FirstSketchNameTextBox.Text?.Trim();
            else
                name = NewSketchNameTextBox.Text?.Trim();

            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            tabService.SetFolderName(name);
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a valid sketch name.");
                return;
            }

            var basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SketchRoom", "SavedTabs");

            var sketchFolder = Path.Combine(basePath, name);

            if (Directory.Exists(sketchFolder))
            {
                MessageBox.Show("A sketch with this name already exists.");
                return;
            }

            Directory.CreateDirectory(sketchFolder);
            this.Close();
        }
    }

    public class StackPreviewItem
    {
        public string TabName { get; set; }
        public ImageSource Thumbnail { get; set; }
        public List<BPMNShapeModelWithPosition> Shapes { get; set; }
        public string FolderName { get; set; }
    }

}
