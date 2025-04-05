using SketchRoom.Models.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteBoardModule.ViewModels
{
    public class LeftToolsActionsViewModel : BindableBase
    {
        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                SetProperty(ref _searchQuery, value);
                FilterShapes();
            }
        }

        public ObservableCollection<BPMNShapeModel> AllShapes { get; } = new();
        public ObservableCollection<BPMNShapeModel> FilteredShapes { get; } = new();

        public LeftToolsActionsViewModel()
        {
            LoadShapes();
            FilterShapes();
        }

        private void LoadShapes()
        {
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Rectangle",
                SvgUri = new Uri("pack://application:,,,/WhiteBoardModule;component/SVG/rectangle.svg")
            });
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Ellipse",
                ShapeContent = new WhiteBoardModule.XAML.EllipseShape()
            });
            // Repetă pentru celelalte
        }

        private void FilterShapes()
        {
            FilteredShapes.Clear();
            var filtered = AllShapes
                .Where(s => string.IsNullOrWhiteSpace(SearchQuery) || s.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

            foreach (var shape in filtered)
                FilteredShapes.Add(shape);
        }

        public void OnShapeDragStarted(object shape)
        {
            if (shape is BPMNShapeModel model)
            {
                // TODO: Inițiază procesul de plasare pe whiteboard sau comunică cu serviciul de tool-uri
                Console.WriteLine($"Drag started: {model.Name}");
            }
        }
    }
}
