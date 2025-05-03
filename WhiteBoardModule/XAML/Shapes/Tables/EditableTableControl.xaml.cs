using SketchRoom.Models.Enums;
using SketchRoom.Toolkit.Wpf.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;
using WhiteBoardModule.Events;
using WhiteBoardModule.XAML.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.Tables
{
    /// <summary>
    /// Interaction logic for EditableTableControl.xaml
    /// </summary>
    public partial class EditableTableControl : UserControl, ITableShapeRender
    {
        private readonly List<Image> _overlayImages = new();
        private int _rows = 3;
        private int _columns = 3;
        private string[,] _cellValues;
        private readonly IEventAggregator _eventAggregator;
        public Guid Id { get; } = Guid.NewGuid();
        private readonly IShapeSelectionService _selectionService;
        private int? _lastRowClicked;
        private int? _lastColumnClicked;
        public int? GetLastRowClicked() => _lastRowClicked;
        public int? GetLastColumnClicked() => _lastColumnClicked;
        private Canvas? OverlayCanvas;

        private bool _isDraggingImage = false;
        private Point _dragStart;
        private UIElement? _draggedImage = null;
        public EditableTableControl()
        {
            InitializeComponent();

            _eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();

            _cellValues = new string[_rows, _columns];
            InitCells();
            RenderTable();
            AddGridSplitters();
            if (OverlayCanvas == null)
            {
                OverlayCanvas = new Canvas
                {
                    IsHitTestVisible = false,
                    Background = Brushes.Transparent
                };

                Panel.SetZIndex(OverlayCanvas, 1000);
                (this.Content as Grid)?.Children.Add(OverlayCanvas);
            }

            OverlayCanvas.MouseLeftButtonUp += OverlayCanvas_MouseLeftButtonUp;
            OverlayCanvas.MouseMove += OverlayCanvas_MouseMove;            
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingImage && _draggedImage != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPos = e.GetPosition(OverlayCanvas);
                var delta = currentPos - _dragStart;

                double left = Canvas.GetLeft(_draggedImage) + delta.X;
                double top = Canvas.GetTop(_draggedImage) + delta.Y;

                Canvas.SetLeft(_draggedImage, left);
                Canvas.SetTop(_draggedImage, top);

                _dragStart = currentPos;
            }
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var currentBoard = tabService.GetWhiteBoard(tabService.CurrentTab?.Id ?? Guid.Empty) as WhiteBoardControl;

            if (_isDraggingImage && _draggedImage is Border border && border.Child is FrameworkElement child)
            {
                _draggedImage.ReleaseMouseCapture();
                _isDraggingImage = false;

                Point posInTable = e.GetPosition(this);
                if (posInTable.X < 0 || posInTable.Y < 0 || posInTable.X > ActualWidth || posInTable.Y > ActualHeight)
                {
                    OverlayCanvas?.Children.Remove(border);

                    if (currentBoard != null)
                    {
                        var canvas = currentBoard.FindName("DrawingCanvas") as Canvas;
                        if (canvas != null)
                        {
                            Point dropPos = e.GetPosition(canvas);
                            currentBoard._dropService.MoveOverlayImageToWhiteBoard(child, dropPos);
                        }
                    }
                }

                _draggedImage = null;
            }
        }

        public void AddOverlayElement(UIElement element, Point relativePosition)
        {
            var container = new Border
            {
                Child = element,
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeAll,
                Tag = "OverlayImage"
            };

            Canvas.SetLeft(container, relativePosition.X);
            Canvas.SetTop(container, relativePosition.Y);
            Panel.SetZIndex(container, 1001);

            // eveniment pentru dublu-click
            container.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    _isDraggingImage = true;
                    _dragStart = e.GetPosition(OverlayCanvas);
                    _draggedImage = container;
                    container.CaptureMouse();
                    e.Handled = true;
                }
            };

            OverlayCanvas.Children.Add(container);
        }

        private void AddGridSplitters()
        {
            for (int i = 1; i < _columns; i++)
            {
                var splitter = new GridSplitter
                {
                    Width = 5,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = Brushes.Transparent
                };
                Grid.SetColumn(splitter, i + 1);
                Grid.SetRowSpan(splitter, _rows);
                Grid.SetRow(splitter, 1);
                splitter.DragDelta += (_, _) => PublishSizeChanged();
                RootGrid.Children.Add(splitter);
            }

            // 🟢 Splittere interne între rânduri
            for (int i = 1; i < _rows; i++)
            {
                var splitter = new GridSplitter
                {
                    Height = 5,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    Background = Brushes.Transparent
                };
                Grid.SetRow(splitter, i + 1);
                Grid.SetColumnSpan(splitter, _columns);
                Grid.SetColumn(splitter, 1);
                splitter.DragDelta += (_, _) => PublishSizeChanged();
                RootGrid.Children.Add(splitter);
            }

            // 🟣 Margine: Top
            var topSplitter = new GridSplitter
            {
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = Brushes.Transparent
            };
            Grid.SetRow(topSplitter, 0);
            Grid.SetColumn(topSplitter, 1);
            Grid.SetColumnSpan(topSplitter, _columns);
            topSplitter.DragDelta += (_, _) => PublishSizeChanged();
            RootGrid.Children.Add(topSplitter);

            // 🟣 Margine: Bottom
            var bottomSplitter = new GridSplitter
            {
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent
            };
            Grid.SetRow(bottomSplitter, _rows + 1);
            Grid.SetColumn(bottomSplitter, 1);
            Grid.SetColumnSpan(bottomSplitter, _columns);
            bottomSplitter.DragDelta += (_, _) => PublishSizeChanged();
            RootGrid.Children.Add(bottomSplitter);

            // 🟣 Margine: Left
            var leftSplitter = new GridSplitter
            {
                Width = 5,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            Grid.SetColumn(leftSplitter, 0);
            Grid.SetRow(leftSplitter, 1);
            Grid.SetRowSpan(leftSplitter, _rows);
            leftSplitter.DragDelta += (_, _) => PublishSizeChanged();
            RootGrid.Children.Add(leftSplitter);

            // 🟣 Margine: Right
            var rightSplitter = new GridSplitter
            {
                Width = 5,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            Grid.SetColumn(rightSplitter, _columns + 1);
            Grid.SetRow(rightSplitter, 1);
            Grid.SetRowSpan(rightSplitter, _rows);
            rightSplitter.DragDelta += (_, _) => PublishSizeChanged();
            RootGrid.Children.Add(rightSplitter);
        }

        private void InitCells()
        {
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                    _cellValues[r, c] = r == 0 ? $"Header {c + 1}" : $"R{r + 1}C{c + 1}";
        }

        private void RenderTable()
        {
            RootGrid.Children.Clear();
            RootGrid.RowDefinitions.Clear();
            RootGrid.ColumnDefinitions.Clear();

            // 🟡 1. Adăugăm marginile externe
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) }); // Top
            for (int i = 0; i < _rows; i++)
                RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) }); // Bottom

            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) }); // Left
            for (int i = 0; i < _columns; i++)
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) }); // Right

            // 🟡 2. Adăugăm celulele de tabel
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    int rIndex = row;
                    int cIndex = col;

                    var border = new Border
                    {
                        BorderThickness = new Thickness(0.5),
                        BorderBrush = Brushes.Black,
                        Background = row == 0 ? Brushes.Black : Brushes.White,
                        Margin = new Thickness(1)
                    };

                    border.PreviewMouseRightButtonDown += (s, e) =>
                    {
                        _lastRowClicked = rIndex;
                        _lastColumnClicked = cIndex;
                    };
                    border.ContextMenuOpening += (s, e) =>
                    {
                        _lastRowClicked = rIndex;
                        _lastColumnClicked = cIndex;
                    };

                    border.MouseLeftButtonDown += (s, e) =>
                    {
                        if (e.ClickCount == 2 && OverlayCanvas != null)
                        {
                            var pos = e.GetPosition(OverlayCanvas);
                            foreach (var child in OverlayCanvas.Children.OfType<FrameworkElement>())
                            {
                                if (child.Tag?.ToString() == "OverlayImage")
                                {
                                    var bounds = new Rect(Canvas.GetLeft(child), Canvas.GetTop(child), child.ActualWidth, child.ActualHeight);
                                    if (bounds.Contains(pos))
                                    {
                                        child.CaptureMouse();
                                        _isDraggingImage = true;
                                        _draggedImage = child;
                                        _dragStart = pos;
                                        e.Handled = true;
                                        break;
                                    }
                                }
                            }
                        }
                    };

                    Grid.SetRow(border, row + 1);
                    Grid.SetColumn(border, col + 1);
                    RootGrid.Children.Add(border);
                }
            }

            AddGridSplitters();
            PublishSizeChanged();
        }

        private void DeleteRowInternal(int index)
        {
            var newValues = new string[_rows - 1, _columns];

            for (int r = 0, nr = 0; r < _rows; r++)
            {
                if (r == index) continue;

                for (int c = 0; c < _columns; c++)
                    newValues[nr, c] = _cellValues[r, c];

                nr++;
            }

            _rows--;
            _cellValues = newValues;
        }

        private void DeleteColumnInternal(int index)
        {
            var newValues = new string[_rows, _columns - 1];

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0, nc = 0; c < _columns; c++)
                {
                    if (c == index) continue;
                    newValues[r, nc++] = _cellValues[r, c];
                }
            }

            _columns--;
            _cellValues = newValues;
        }

        private void AddRow(int index)
        {
            var newValues = new string[_rows + 1, _columns];
            for (int r = 0; r < index; r++)
                for (int c = 0; c < _columns; c++)
                    newValues[r, c] = _cellValues[r, c];

            for (int c = 0; c < _columns; c++)
                newValues[index, c] = $"R{index + 1}C{c + 1}";

            for (int r = index; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                    newValues[r + 1, c] = _cellValues[r, c];

            _rows++;
            _cellValues = newValues;
        }

        private void AddColumn(int index)
        {
            var newValues = new string[_rows, _columns + 1];
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < index; c++)
                    newValues[r, c] = _cellValues[r, c];

                newValues[r, index] = r == 0 ? $"Header {index + 1}" : $"R{r + 1}C{index + 1}";

                for (int c = index; c < _columns; c++)
                    newValues[r, c + 1] = _cellValues[r, c];
            }

            _columns++;
            _cellValues = newValues;
        }

        public void AddRowAbove(int currentRow)
        {
            AddRow(currentRow);
            RenderTable();
        }

        public void AddRowBelow(int currentRow)
        {
            AddRow(currentRow + 1);
            RenderTable();
        }

        public void AddColumnLeft(int currentColumn)
        {
            AddColumn(currentColumn);
            RenderTable();
        }

        public void AddColumnRight(int currentColumn)
        {
            AddColumn(currentColumn + 1);
            RenderTable();
        }

        public void DeleteRow(int currentRow)
        {
            if (_rows > 1)
            {
                DeleteRowInternal(currentRow);
                RenderTable();
            }
        }

        public void DeleteColumn(int currentColumn)
        {
            if (_columns > 1)
            {
                DeleteColumnInternal(currentColumn);
                RenderTable();
            }
        }

        public void ChangeHeaderBackground(Brush color)
        {
            foreach (var child in RootGrid.Children.OfType<Border>())
            {
                // Rândul logic 0 => Grid.Row == 1 (datorită marginilor)
                if (Grid.GetRow(child) == 1)
                {
                    child.Background = color;
                }
            }
        }

        public void ChangeBorderColor(Brush color)
        {
            foreach (var child in RootGrid.Children.OfType<Border>())
            {
                if (_lastRowClicked.HasValue && _lastColumnClicked.HasValue)
                {
                    int row = Grid.GetRow(child) - 1;
                    int col = Grid.GetColumn(child) - 1;

                    if (row == _lastRowClicked && col == _lastColumnClicked)
                    {
                        child.Background = color;
                        break;
                    }
                }
            }
        }

        private void PublishSizeChanged()
        {
            double totalWidth = RootGrid.ColumnDefinitions
                .Skip(1)
                .Take(_columns)
                .Sum(cd => cd.Width.IsAbsolute ? cd.Width.Value : 0);

            double totalHeight = RootGrid.RowDefinitions
                .Skip(1)
                .Take(_rows)
                .Sum(rd => rd.Height.IsAbsolute ? rd.Height.Value : 0);

            var info = new TableResizeInfo
            {
                NewSize = new Size(totalWidth, totalHeight),
                SourceId = this.Id
            };

            _eventAggregator.GetEvent<TableResizedEvent>().Publish(info);
        }

    
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

       
    }
}