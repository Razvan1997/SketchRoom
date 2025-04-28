using Prism.Events;
using Prism.Ioc;
using SketchRoom.Models.Enums;
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
using WhiteBoard.Core.Services.Interfaces;
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
        private readonly IShapeSelectionService _selectionService;
        public EditableTableControl()
        {
            InitializeComponent();

            _eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();

            _cellValues = new string[_rows, _columns];
            InitCells();
            RenderTable();
            AddGridSplitters();

        }

        private void AddGridSplitters()
        {
            // Coloane
            for (int i = 1; i < _columns; i++)
            {
                var splitter = new GridSplitter
                {
                    Width = 5,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = Brushes.Transparent,
                };
                Grid.SetColumn(splitter, i);
                Grid.SetRowSpan(splitter, _rows);
                splitter.DragDelta += (_, _) => PublishSizeChanged();
                RootGrid.Children.Add(splitter);
            }

            // Rânduri
            for (int i = 1; i < _rows; i++)
            {
                var splitter = new GridSplitter
                {
                    Height = 5,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    Background = Brushes.Transparent,
                };
                Grid.SetRow(splitter, i);
                Grid.SetColumnSpan(splitter, _columns);
                splitter.DragDelta += (_, _) => PublishSizeChanged();
                RootGrid.Children.Add(splitter);
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
                        BorderThickness = new Thickness(0),
                        Background = Brushes.Transparent,
                        Foreground = row == 0 ? Brushes.White : Brushes.Black,
                        FontSize = 14,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        IsReadOnly = false,
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

                    var border = new Border
                    {
                        BorderThickness = new Thickness(0.5),
                        BorderBrush = Brushes.Black,
                        Background = row == 0 ? Brushes.Black : Brushes.White,
                        Child = tb,
                        Margin = new Thickness(1)
                    };

                    // click normal pe text – selectează text
                    tb.PreviewMouseLeftButtonDown += (s, e) =>
                    {
                        _selectionService.Select(ShapePart.Text, tb);
                        e.Handled = false;
                    };

                    // dublu click pe text – selectează border/fundal
                    tb.MouseDoubleClick += (s, e) =>
                    {
                        _selectionService.Select(ShapePart.Border, border);
                        e.Handled = true;
                    };

                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);
                    RootGrid.Children.Add(border);
                }
            }

            AddGridSplitters();
            PublishSizeChanged();
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

        private bool IsClickOnMargin(FrameworkElement element, Point mousePos)
        {
            const double marginWidth = 6;

            return mousePos.X < marginWidth ||
                   mousePos.X > element.ActualWidth - marginWidth ||
                   mousePos.Y < marginWidth ||
                   mousePos.Y > element.ActualHeight - marginWidth;
        }

        // Dacă nu ai un Border real, creezi unul fals doar pentru selecție logică
        private Border CreateFakeBorder(TextBox tb)
        {
            return new Border
            {
                Width = tb.ActualWidth,
                Height = tb.ActualHeight,
                Background = tb.Background,
                BorderThickness = tb.BorderThickness,
                BorderBrush = tb.BorderBrush
            };
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