using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SketchRoom.Windows
{
    /// <summary>
    /// Interaction logic for SketchGalleryWindow.xaml
    /// </summary>
    public partial class SketchGalleryWindow : Window
    {
        private string _imageFolderPath;
        private Point _origin;
        private Point _start;
        private bool _isDragging;
        public SketchGalleryWindow(string imageFolderPath)
        {
            InitializeComponent();
            _imageFolderPath = imageFolderPath;

            LoadImages();
        }

        private void LoadImages()
        {
            if (!Directory.Exists(_imageFolderPath))
                return;

            var files = Directory.GetFiles(_imageFolderPath, "*.*")
                                 .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg") || f.EndsWith(".bmp"))
                                 .ToList();

            ImagePanel.Children.Clear();

            foreach (var file in files)
            {
                var imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.UriSource = new Uri(file);
                imageSource.CacheOption = BitmapCacheOption.OnLoad; // crucial!
                imageSource.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // for fresh read
                imageSource.EndInit();
                imageSource.Freeze();

                var img = new Image
                {
                    Source = imageSource,
                    Width = 120,
                    Height = 120,
                    Margin = new Thickness(5),
                    Cursor = Cursors.Hand,
                    Tag = file
                };

                img.MouseLeftButtonUp += (s, e) => ShowPreview(file);

                var contextMenu = new ContextMenu
                {
                    Style = (Style)FindResource("DarkContextMenuStyle")
                };

                var deleteMenuItem = new MenuItem { Header = "Delete" };
                deleteMenuItem.Click += (s, e) => DeleteImage(file);
                contextMenu.Items.Add(deleteMenuItem);

                img.ContextMenu = contextMenu;

                var text = new TextBlock
                {
                    Text = System.IO.Path.GetFileName(file),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 120,
                    Margin = new Thickness(0, 4, 0, 0)
                };

                var container = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { img, text }
                };

                ImagePanel.Children.Add(container);
            }
        }

        private void ShowPreview(string path)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.EndInit();
            bmp.Freeze();

            PreviewImage.Source = bmp;

            // reset zoom & pan
            ImageScaleTransform.ScaleX = 0.8;
            ImageScaleTransform.ScaleY = 0.8;
            ImageTranslateTransform.X = 0;
            ImageTranslateTransform.Y = 0;

            PreviewBorder.Visibility = Visibility.Visible;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (PreviewBorder.Visibility == Visibility.Visible)
                    PreviewBorder.Visibility = Visibility.Collapsed;
                else
                    this.Close();
            }

            base.OnKeyDown(e);
        }

        private void ZoomScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;

            ImageScaleTransform.ScaleX *= zoomFactor;
            ImageScaleTransform.ScaleY *= zoomFactor;
        }

        private void PreviewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PreviewImage.IsMouseCaptured) return;

            _start = e.GetPosition(ZoomScrollViewer);
            _origin = new Point(ImageTranslateTransform.X, ImageTranslateTransform.Y);
            PreviewImage.CaptureMouse();
            _isDragging = true;
        }

        private void PreviewImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;

            PreviewImage.ReleaseMouseCapture();
            _isDragging = false;
        }

        private void PreviewImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Vector v = _start - e.GetPosition(ZoomScrollViewer);
            ImageTranslateTransform.X = _origin.X - v.X;
            ImageTranslateTransform.Y = _origin.Y - v.Y;
        }

        private void DeleteImage(string filePath)
        {
            var result = MessageBox.Show($"Are you sure you want to delete \"{System.IO.Path.GetFileName(filePath)}\"?",
                                         "Confirm Delete",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(filePath);
                    LoadImages(); // Reîncarcă galeria
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
