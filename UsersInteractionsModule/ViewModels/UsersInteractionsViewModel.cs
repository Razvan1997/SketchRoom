using SketchRoom.Models.Enums;
using SketchRoom.Models.Shapes;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WhiteBoardModule.XAML;

namespace UsersInteractionsModule.ViewModels
{
    public class UsersInteractionsViewModel : BindableBase
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
        public ObservableCollection<ShapeCategoryGroup> GroupedFilteredShapes { get; } = new();

        private readonly IRegionManager _regionManager;
        //public ICommand CreateSketchRoomCommand { get; }
        //public ICommand ParticipateSketchRoomCommand { get; }

        public UsersInteractionsViewModel(IRegionManager regionManager)
        {
            //CreateSketchRoomCommand = new DelegateCommand(OnCreateSketchRoom);
            //ParticipateSketchRoomCommand = new DelegateCommand(OnParticipateToSketchRoom);
            _regionManager = regionManager;

            LoadShapes();
            FilterShapes();
        }

        //private void OnCreateSketchRoom()
        //{
        //    _regionManager.RequestNavigate("ContentRegion", "LobbyView");
        //}

        //private void OnParticipateToSketchRoom()
        //{
        //    _regionManager.RequestNavigate("ContentRegion", "ParticipationView");
        //}

        public void OnShapeDragStarted(object shape)
        {
            if (shape is BPMNShapeModel model)
            {
                // TODO: Inițiază procesul de plasare pe whiteboard sau comunică cu serviciul de tool-uri
                Console.WriteLine($"Drag started: {model.Name}");
            }
        }

        private void LoadShapes()
        {
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Rectangle",
                SvgUri = new Uri("pack://application:,,,/WhiteBoardModule;component/SVG/rectangle.svg"),
                Category = "General"
            });

            var ellipse = new GenericShapeControl();
            ellipse.SetShapePreview(ShapeType.Ellipse);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Ellipse",
                Type = ShapeType.Ellipse,
                ShapeContent = ellipse,
                Category = "General"
            });

            var rectangle = new GenericShapeControl();
            rectangle.SetShapePreview(ShapeType.Rectangle);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Rectangle",
                Type = ShapeType.Rectangle,
                ShapeContent = rectangle,
                Category = "General"
            });

            var triangle = new GenericShapeControl();
            triangle.SetShapePreview(ShapeType.Triangle);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Triangle",
                Type = ShapeType.Triangle,
                ShapeContent = triangle,
                Category = "General"
            });

            var shapeText = new GenericShapeControl();
            shapeText.SetShapePreview(ShapeType.ShapeText);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "ShapeText",
                Type = ShapeType.ShapeText,
                ShapeContent = shapeText,
                Category = "General"
            });

            var tableShape = new GenericShapeControl();
            tableShape.SetShapePreview(ShapeType.TableShape);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "Table3x3",
                Type = ShapeType.TableShape,
                ShapeContent = tableShape,
                Category = "Tables"
            });

            var connectorLabelShape = new GenericShapeControl();
            connectorLabelShape.SetShapePreview(ShapeType.ConnectorShapeLabel);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "ConnectorShapeLabel",
                Type = ShapeType.ConnectorShapeLabel,
                ShapeContent = connectorLabelShape,
                Category = "Connectors"
            });

            var connectorDoubleLabelShape = new GenericShapeControl();
            connectorDoubleLabelShape.SetShapePreview(ShapeType.ConnectorDoubleShapeLabel);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "ConnectorDoubleLabelShape",
                Type = ShapeType.ConnectorDoubleShapeLabel,
                ShapeContent = connectorDoubleLabelShape,
                Category = "Connectors"
            });

            var horizontalContainer = new GenericShapeControl();
            horizontalContainer.SetShapePreview(ShapeType.ContainerHorizontalShape);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "HorizontalContainer",
                Type = ShapeType.ContainerHorizontalShape,
                ShapeContent = horizontalContainer,
                Category = "Containers"
            });

            var entityShape = new GenericShapeControl();
            entityShape.SetShapePreview(ShapeType.EntityShape);
            AllShapes.Add(new BPMNShapeModel
            {
                Name = "EntityShape",
                Type = ShapeType.EntityShape,
                ShapeContent = entityShape,
                Category = "Entity"
            });

        }

        private void FilterShapes()
        {
            GroupedFilteredShapes.Clear();

            var grouped = AllShapes
                .Where(s => string.IsNullOrWhiteSpace(SearchQuery) || s.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .GroupBy(s => s.Category)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                GroupedFilteredShapes.Add(new ShapeCategoryGroup
                {
                    Category = group.Key,
                    Items = new ObservableCollection<BPMNShapeModel>(group),
                    IsInitiallyExpanded = group.Key == "General"
                });
            }
        }
    }
}
