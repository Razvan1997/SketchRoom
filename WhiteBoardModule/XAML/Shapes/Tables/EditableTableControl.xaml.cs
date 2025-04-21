using Prism.Events;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WhiteBoardModule.Events;

namespace WhiteBoardModule.XAML.Shapes.Tables
{
    /// <summary>
    /// Interaction logic for EditableTableControl.xaml
    /// </summary>
    public partial class EditableTableControl : UserControl
    {
        private int _rows = 3;
        private int _columns = 3;
        private string[,] _cellValues;
        private readonly IEventAggregator _eventAggregator;
        public Guid Id { get; } = Guid.NewGuid();
        public EditableTableControl()
        {
            InitializeComponent();

            _eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
            _cellValues = new string[_rows, _columns];
            ResizeCanvas.IsHitTestVisible = true;
            this.MouseMove += OnMouseMoveShowThumbs;
            InitCells();
            RenderTable();

            this.LayoutUpdated += (_, _) => UpdateThumbPositions();
        }

        private void OnMouseMoveShowThumbs(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(ResizeCanvas);
            double margin = 4;

            foreach (var thumb in ResizeCanvas.Children.OfType<Thumb>())
            {
                double left = Canvas.GetLeft(thumb);
                double top = Canvas.GetTop(thumb);
                double width = thumb.Width > 0 ? thumb.Width : RootGrid.ActualWidth;
                double height = thumb.Height > 0 ? thumb.Height : RootGrid.ActualHeight;

                Rect sensitiveArea = new(
                    left - margin,
                    top - margin,
                    width + 2 * margin,
                    height + 2 * margin
                );

                thumb.Visibility = sensitiveArea.Contains(pos)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void InitCells()
        {
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                    _cellValues[r, c] = r == 0 ? $"Header {c + 1}" : $"R{r + 1}C{c + 1}";
        }

        private void RenderTable()
        {
            ResizeCanvas.Children.Clear();
            RootGrid.Children.Clear();
            RootGrid.RowDefinitions.Clear();
            RootGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < _columns; i++)
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            for (int i = 0; i < _rows; i++)
                RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    var tb = new TextBox
                    {
                        Text = _cellValues[row, col],
                        BorderThickness = new Thickness(0.5),
                        BorderBrush = Brushes.Black,
                        Background = row == 0 ? Brushes.Black : Brushes.White,
                        Foreground = row == 0 ? Brushes.White : Brushes.Black,
                        FontSize = 14,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        IsReadOnly = false,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        ContextMenu = null
                    };

                    int rIndex = row;
                    int cIndex = col;

                    tb.ContextMenuOpening += (s, e) =>
                    {
                        var textbox = (TextBox)s;
                        textbox.ContextMenu = CreateContextMenu(rIndex, cIndex);
                        textbox.ContextMenu.IsOpen = true;
                        e.Handled = true;
                    };

                    tb.TextChanged += (_, _) => _cellValues[rIndex, cIndex] = tb.Text;

                    Grid.SetRow(tb, row);
                    Grid.SetColumn(tb, col);
                    RootGrid.Children.Add(tb);
                }
            }

            AddResizeThumbs();
        }

        private ContextMenu CreateContextMenu(int row, int col)
        {
            var menu = new ContextMenu();

            menu.Items.Add(new MenuItem { Header = "Copy", Command = ApplicationCommands.Copy });
            menu.Items.Add(new MenuItem { Header = "Paste", Command = ApplicationCommands.Paste });
            menu.Items.Add(new Separator());

            menu.Items.Add(new MenuItem
            {
                Header = "Add Row Above",
                Command = new RelayCommand(_ =>
                {
                    AddRow(row);
                    RenderTable();
                })
            });

            menu.Items.Add(new MenuItem
            {
                Header = "Add Row Below",
                Command = new RelayCommand(_ =>
                {
                    AddRow(row + 1);
                    RenderTable();
                })
            });

            menu.Items.Add(new MenuItem
            {
                Header = "Add Column Left",
                Command = new RelayCommand(_ =>
                {
                    AddColumn(col);
                    RenderTable();
                })
            });

            menu.Items.Add(new MenuItem
            {
                Header = "Add Column Right",
                Command = new RelayCommand(_ =>
                {
                    AddColumn(col + 1);
                    RenderTable();
                })
            });

            menu.Items.Add(new Separator());

            // prevenim ștergerea rândului/coloanei unice
            if (_rows > 1)
            {
                menu.Items.Add(new MenuItem
                {
                    Header = "Delete Row",
                    Command = new RelayCommand(_ =>
                    {
                        DeleteRow(row);
                        RenderTable();
                    })
                });
            }

            if (_columns > 1)
            {
                menu.Items.Add(new MenuItem
                {
                    Header = "Delete Column",
                    Command = new RelayCommand(_ =>
                    {
                        DeleteColumn(col);
                        RenderTable();
                    })
                });
            }

            return menu;
        }

        private void DeleteRow(int index)
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

        private void DeleteColumn(int index)
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

        private void AddResizeThumbs()
        {
            ResizeCanvas.Children.Clear();

            for (int i = 1; i < _columns; i++)
            {
                var thumb = new Thumb
                {
                    Width = 6,
                    Height = 0, // va fi setat din UpdateThumbPositions
                    Background = Brushes.Transparent,
                    Cursor = Cursors.SizeWE,
                    Opacity = 0.001,
                    Tag = $"col:{i - 1}"
                };
                thumb.IsHitTestVisible = true;
                thumb.DragDelta += (s, e) =>
                {
                    var colIndex = int.Parse(((Thumb)s).Tag.ToString().Split(':')[1]);
                    var colDef = RootGrid.ColumnDefinitions[colIndex];
                    double newWidth = Math.Max(30, colDef.Width.Value + e.HorizontalChange);
                    colDef.Width = new GridLength(newWidth);

                    PublishSizeChanged();
                };

                ResizeCanvas.Children.Add(thumb);
            }

            for (int i = 1; i < _rows; i++)
            {
                var thumb = new Thumb
                {
                    Height = 6,
                    Width = 0, // va fi setat din UpdateThumbPositions
                    Background = Brushes.Transparent,
                    Cursor = Cursors.SizeNS,
                    Opacity = 0.001,
                    Tag = $"row:{i - 1}"
                };
                thumb.IsHitTestVisible = true;
                thumb.DragDelta += (s, e) =>
                {
                    var rowIndex = int.Parse(((Thumb)s).Tag.ToString().Split(':')[1]);
                    var rowDef = RootGrid.RowDefinitions[rowIndex];
                    double newHeight = Math.Max(20, rowDef.Height.Value + e.VerticalChange);
                    rowDef.Height = new GridLength(newHeight);

                    PublishSizeChanged();
                };
                ResizeCanvas.Children.Add(thumb);
            }
        }

        private void PublishSizeChanged()
        {
            double totalWidth = RootGrid.ColumnDefinitions.Sum(cd => cd.Width.IsAbsolute ? cd.Width.Value : 0);
            double totalHeight = RootGrid.RowDefinitions.Sum(rd => rd.Height.IsAbsolute ? rd.Height.Value : 0);

            var info = new TableResizeInfo
            {
                NewSize = new Size(totalWidth, totalHeight),
                SourceId = this.Id
            };

            _eventAggregator.GetEvent<TableResizedEvent>().Publish(info);
        }

        private void UpdateThumbPositions()
        {
            if (ResizeCanvas.Children.Count == 0 || RootGrid.ActualWidth == 0 || RootGrid.ActualHeight == 0)
                return;

            GeneralTransform transform = RootGrid.TransformToVisual(ResizeCanvas);
            Rect bounds = transform.TransformBounds(new Rect(0, 0, RootGrid.ActualWidth, RootGrid.ActualHeight));

            double x = 0;
            int thumbIndex = 0;

            // THUMB-URI VERTICALE (coloane)
            for (int i = 1; i < _columns; i++)
            {
                x += RootGrid.ColumnDefinitions[i - 1].ActualWidth;

                var thumb = (Thumb)ResizeCanvas.Children[thumbIndex++];
                thumb.Height = bounds.Height;
                Canvas.SetLeft(thumb, bounds.Left + x - thumb.Width / 2);
                Canvas.SetTop(thumb, bounds.Top);
            }

            double y = 0;

            // THUMB-URI ORIZONTALE (rânduri)
            for (int i = 1; i < _rows; i++)
            {
                y += RootGrid.RowDefinitions[i - 1].ActualHeight;

                var thumb = (Thumb)ResizeCanvas.Children[thumbIndex++];
                thumb.Width = bounds.Width;
                Canvas.SetLeft(thumb, bounds.Left);
                Canvas.SetTop(thumb, bounds.Top + y - thumb.Height / 2);
            }
        }

        private void AttachResizeAdorner()
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(RootGrid);
            if (adornerLayer == null)
            {
                this.Loaded += (_, _) =>
                {
                    var layer = AdornerLayer.GetAdornerLayer(RootGrid);
                    if (layer != null)
                    {
                        layer.Add(new ResizeAdorner(RootGrid));
                    }
                };
            }
            else
            {
                adornerLayer.Add(new ResizeAdorner(RootGrid));
            }
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