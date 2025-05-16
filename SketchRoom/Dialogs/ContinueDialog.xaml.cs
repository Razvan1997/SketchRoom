using FooterModule.ViewModels;
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
using System.Windows.Threading;
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
        public bool ShowCancelButton { get; set; }
        public ContinueDialog()
        {
            InitializeComponent();
            LoadPreviews();

            Loaded += (_, _) =>
            {
                CancelButton.Visibility = ShowCancelButton
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };
        }

        private async void LoadPreviews()
        {
            LoadingOverlayHelper.Show(); 

            await Task.Delay(100);

            List<SavedWhiteBoardModel> savedTabs = new();

            try
            {
                var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
                var service = new WhiteBoardPersistenceService(tabService);

                savedTabs = await Task.Run(() => service.LoadAllTabsSync());
            }
            catch (Exception ex)
            {
                
            }

            _allTabs.Clear();

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

            LoadingOverlayHelper.Hide(); 
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
                var ea = ContainerLocator.Container.Resolve<IEventAggregator>();

                // ✅ 1. Verificăm dacă există conținut pe tab-urile curente
                bool hasContent = tabService.AllTabs.Any(tab =>
                    tabService.GetWhiteBoard(tab.Id) is WhiteBoardControl wb &&
                    HasUnsavedData(wb));

                if (hasContent)
                {
                    var dialog = new ConfirmationDialog(
                        "Unsaved changes",
                        "You have unsaved sketches. Do you want to save them before previewing another sketch?");
                    bool? result = dialog.ShowDialog();

                    if (result == true && dialog.IsConfirmed)
                    {
                        // ✅ Salvează tab-urile curente
                        var persistence = new WhiteBoardPersistenceService(tabService);
                        await persistence.SaveAllTabsAsync();
                    }

                    // ✅ Oricum, trimite ClearAllTabsEvent
                    ea.GetEvent<ClearAllTabsEvent>().Publish();
                }
                else
                {
                    ea.GetEvent<ClearAllTabsEvent>().Publish();
                }

                // 🔄 Loading UI
                LoadingOverlayHelper.Show();
                await Task.Delay(150); // pentru UX fluent

                // ✅ 2. Încarcă datele din folderul selectat
                var restoredTabs = await Task.Run(() =>
                {
                    var folderPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SketchRoom", "SavedTabs", selectedItem.FolderName);

                    var jsonFiles = Directory.GetFiles(folderPath, "tab_*.json");
                    Array.Sort(jsonFiles);

                    var loaded = new List<SavedWhiteBoardModel>();

                    foreach (var jsonFile in jsonFiles)
                    {
                        try
                        {
                            var json = File.ReadAllText(jsonFile);
                            var model = JsonSerializer.Deserialize<SavedWhiteBoardModel>(json);
                            if (model != null)
                                loaded.Add(model);
                        }
                        catch { }
                    }

                    return loaded;
                });

                // ✅ 3. Publică TabsRestoredEvent
                if (restoredTabs.Count > 0)
                {
                    ea.GetEvent<TabsRestoredEvent>().Publish(new TabsRestoredPayload
                    {
                        Tabs = restoredTabs,
                        FolderName = selectedItem.FolderName
                    });
                }

                this.Close();
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private async void Create_Click(object sender, RoutedEventArgs e)
        {
            string name = WelcomePanel.Visibility == Visibility.Visible
        ? FirstSketchNameTextBox.Text?.Trim()
        : NewSketchNameTextBox.Text?.Trim();

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

            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();

            var ea = ContainerLocator.Container.Resolve<IEventAggregator>();

            bool hasContent = tabService.AllTabs.Any(tab =>
                tabService.GetWhiteBoard(tab.Id) is WhiteBoardControl wb &&
                HasUnsavedData(wb));

            if (hasContent)
            {
                var dialog = new ConfirmationDialog(
                    "Unsaved changes",
                    "You have unsaved sketches. Do you want to save them before continuing?");
                bool? result = dialog.ShowDialog();

                if (result == true && dialog.IsConfirmed)
                {
                    var persistence = new WhiteBoardPersistenceService(tabService);
                    await persistence.SaveAllTabsAsync();
                }
                tabService.SetFolderName(name);
                ea.GetEvent<ClearAllTabsEvent>().Publish();
            }
            else
            {
                tabService.SetFolderName(name);
                ea.GetEvent<ClearAllTabsEvent>().Publish();
            }

            Directory.CreateDirectory(sketchFolder);
            this.Close();
        }
        private bool HasUnsavedData(WhiteBoardControl whiteboard)
        {
            foreach (var child in whiteboard.DrawingCanvasPublic.Children.OfType<FrameworkElement>())
            {
                if (child.Tag?.ToString() == "Connector" && child is Canvas canvas)
                {
                    var connection = whiteboard._connections
                        .FirstOrDefault(c => c.Visual == canvas);
                    if (connection?.Export() != null)
                        return true;
                }

                // 🟦 Dacă e formă interactivă validă
                if (child.Tag?.ToString() == "interactive" &&
                    whiteboard._dropService.TryGetShapeWrapper(child, out var wrapper))
                {
                    if (wrapper.ExportData() != null)
                        return true;
                }
            }

            return false;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
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
