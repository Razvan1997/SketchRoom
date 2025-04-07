using SketchRoom.Models.Enums;
using SketchRoom.Models.Shapes;
using System.Collections.ObjectModel;
using WhiteBoardModule.XAML;

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

            var ellipse = new GenericShapeControl();
            ellipse.SetShape(ShapeType.Ellipse);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Ellipse",
                Type = ShapeType.Ellipse,
                ShapeContent = ellipse
            });

            var rectangle = new GenericShapeControl();
            rectangle.SetShape(ShapeType.Rectangle);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Rectangle",
                Type = ShapeType.Rectangle,
                ShapeContent = rectangle
            });

            var triangle = new GenericShapeControl();
            triangle.SetShape(ShapeType.Triangle);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Triangle",
                Type = ShapeType.Triangle,
                ShapeContent = triangle
            });

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
