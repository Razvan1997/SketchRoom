using SketchRoom.Toolkit.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Helpers;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Text.Json.Serialization;

namespace SketchRoom.Toolkit.Wpf.Services
{
    public class WhiteBoardPersistenceService
    {
        private readonly string _basePath;
        private readonly IWhiteBoardTabService _tabService;

        public WhiteBoardPersistenceService(IWhiteBoardTabService tabService)
        {
            _tabService = tabService;
            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SketchRoom", "SavedTabs");
            Directory.CreateDirectory(_basePath);
        }

        public async Task SaveAllTabsAsync()
        {
            var folder = Path.Combine(_basePath, _tabService.GetFolderName());
            Directory.CreateDirectory(folder);

            // 🧹 Șterge toate fișierele tab_*.json din folderul actual
            foreach (var file in Directory.GetFiles(folder, "tab_*.json"))
            {
                File.Delete(file);
            }

            bool isFirst = true;

            foreach (var tab in _tabService.AllTabs)
            {
                if (_tabService.GetWhiteBoard(tab.Id) is WhiteBoardControl whiteboard)
                {
                    await SaveTabInternalAsync(tab, whiteboard, folder, saveThumbnail: isFirst);
                    isFirst = false; 
                }
            }
        }

        public async Task SaveTabAsync(Guid tabId, WhiteBoardControl whiteboard)
        {
            var tab = _tabService.AllTabs.FirstOrDefault(t => t.Id == tabId);
            if (tab == null)
                return;

            var folder = Path.Combine(_basePath, _tabService.GetFolderName());
            Directory.CreateDirectory(folder);

            await SaveTabInternalAsync(tab, whiteboard, folder, saveThumbnail: false);
        }

        private async Task SaveTabInternalAsync(FooterTabModel tab, WhiteBoardControl whiteboard, string folder, bool saveThumbnail)
        {
            var model = new SavedWhiteBoardModel
            {
                TabId = tab.Id,
                TabName = tab.Name,
                FolderName = _tabService.GetFolderName(),
                Shapes = new List<BPMNShapeModelWithPosition>(),
                Connections = new List<BPMNConnectionExportModel>()
            };

            foreach (var child in whiteboard.DrawingCanvas.Children.OfType<FrameworkElement>())
            {
                if (child.Tag?.ToString() == "Connector" && child is Canvas canvas)
                {
                    var connection = whiteboard._connections
                        .FirstOrDefault(c => c.Visual == canvas);
                    if (connection != null)
                    {
                        var export = connection.Export();
                        if (export != null)
                            model.Connections.Add(export);
                    }

                    continue;
                }

                if (child.Tag?.ToString() != "interactive")
                    continue;

                if (whiteboard._dropService.TryGetShapeWrapper(child, out var wrapper))
                {
                    var shape = wrapper.ExportData();
                    if (shape != null)
                        model.Shapes.Add(shape);
                }
            }

            if (model.Shapes.Count == 0)
                return;

            var fileName = $"tab_{tab.Id}.json";
            var filePath = Path.Combine(folder, fileName);


            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            try
            {
                var json = JsonSerializer.Serialize(model, options);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Eroare la scrierea fișierului: {ex.Message}");
            }

            if (saveThumbnail)
            {
                var thumbnailPath = Path.Combine(folder, "thumbnail.png");
                SaveThumbnailImage(whiteboard, thumbnailPath);
            }
        }

        private void SaveThumbnailImage(WhiteBoardControl whiteboard, string filePath)
        {
            var canvas = whiteboard.DrawingCanvas;

            double width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 800;
            double height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 600;

            // Scalare (de ex. 2x pentru claritate mai mare)
            double scale = 2.0;

            double scaledWidth = width * scale;
            double scaledHeight = height * scale;

            var dpi = 96d * scale;

            var rtb = new RenderTargetBitmap(
                (int)scaledWidth,
                (int)scaledHeight,
                dpi,
                dpi,
                PixelFormats.Pbgra32);

            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                // Aplică o scalare pentru a mări imaginea
                ctx.PushTransform(new ScaleTransform(scale, scale));

                // Desenează controlul într-un brush vizual
                var vb = new VisualBrush(canvas);
                ctx.DrawRectangle(vb, null, new Rect(new Point(0, 0), new Size(width, height)));

                ctx.Pop();
            }

            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            encoder.Save(stream);
        }

        public async Task<List<SavedWhiteBoardModel>> LoadAllTabsAsync()
        {
            var list = new List<SavedWhiteBoardModel>();

            if (!Directory.Exists(_basePath))
                return list;

            foreach (var dir in Directory.GetDirectories(_basePath))
            {
                var jsonFiles = Directory.GetFiles(dir, "tab_*.json");
                foreach (var jsonPath in jsonFiles)
                {
                    var json = await File.ReadAllTextAsync(jsonPath);
                    var model = JsonSerializer.Deserialize<SavedWhiteBoardModel>(json);
                    if (model != null)
                        list.Add(model);
                }
            }

            return list;
        }
    }
}
