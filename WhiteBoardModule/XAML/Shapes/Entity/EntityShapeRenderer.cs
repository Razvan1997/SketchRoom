using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoardModule.XAML.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.Entity
{
    public class EntityShapeRenderer : IShapeRenderer, IShapeEntityRenderer
    {
        private readonly bool _withBindings;
        private static readonly List<string> _sqlTypes = new() { "INT", "VARCHAR", "DATE", "BOOLEAN", "DECIMAL" };
        private Grid? _entityGrid;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointClicked;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointTargetClicked;
        private static UIElement? _lastRightClickedRow;
        private TextBox? _headerTextBox;

        public UIElement? LastRightClickedRow => _lastRightClickedRow;

        public EntityShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement Render() => CreateTableUI();

        public UIElement CreatePreview()
        {
            var preview = CreateTableUI(isPreview: true);
            return new Viewbox
            {
                Width = 100,
                Height = 100,
                Stretch = Stretch.Uniform,
                Child = preview
            };
        }

        private UIElement CreateTableUI(bool isPreview = false)
        {
            var grid = new Grid
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(2)
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // First row
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Add row

            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            var tableNameBox = new TextBox
            {
                Text = "EntityName",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)), // dark grey
                Foreground = Brushes.White,
                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = TextAlignment.Center,
                IsReadOnly = isPreview
            };
            Grid.SetRow(tableNameBox, 0);
            Grid.SetColumnSpan(tableNameBox, 2);
            grid.Children.Add(tableNameBox);
            _headerTextBox = tableNameBox;
            // First row
            AddStyledDataRow(grid, 1, "Id", "INT", isPreview);

            var border = new Border
            {
                BorderBrush = Brushes.DeepSkyBlue,
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Child = grid
            };

            border.MouseLeftButtonDown += (s, e) =>
            {
                ConnectionPointTargetClicked?.Invoke(this, new ConnectionPointEventArgs("Auto", border, e));
                e.Handled = true;
            };
            _entityGrid = grid;
            return border;
        }

        private void AddStyledDataRow(Grid grid, int rowIndex, string columnName, string columnType, bool isPreview)
        {
            var rowGrid = new Grid
            {
                Margin = new Thickness(2),
                Background = Brushes.Transparent
            };

            // Define columns
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); // 🔵 Left connector
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition());                             // Column name
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition());                             // SQL type
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); // PK
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) }); // Nullable
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); // 🔵 Right connector

            void AttachContextMenuEvents(UIElement element, Grid rowGrid)
            {
                element.PreviewMouseRightButtonDown += (s, e) =>
                {
                    _lastRightClickedRow = rowGrid;
                    e.Handled = false;
                };
            }

            // 🔵 Conector stânga
            var leftConnector = new Rectangle
            {
                Width = 8,
                Height = 12,
                Fill = Brushes.DodgerBlue,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Cross,
                Tag = "Connector"
            };

            leftConnector.MouseLeftButtonDown += (s, e) =>
            {
                ConnectionPointClicked?.Invoke(this, new ConnectionPointEventArgs("Left", leftConnector, e));
                e.Handled = true;
            };
            Grid.SetColumn(leftConnector, 0);
            rowGrid.Children.Add(leftConnector);

            // 📝 TextBox (nume coloană)
            var nameBox = new TextBox
            {
                Text = columnName,
                Margin = new Thickness(2),
                Style = (Style)Application.Current.FindResource("DarkTextBoxStyle"),
                IsReadOnly = isPreview
            };
            Grid.SetColumn(nameBox, 1);
            rowGrid.Children.Add(nameBox);
            AttachContextMenuEvents(nameBox, rowGrid);

            // 📦 ComboBox (tip SQL)
            var typeBox = new ComboBox
            {
                ItemsSource = _sqlTypes,
                SelectedItem = columnType,
                Margin = new Thickness(2),
                Style = (Style)Application.Current.FindResource("DarkComboBoxStyle"),
                IsEnabled = !isPreview
            };
            Grid.SetColumn(typeBox, 2);
            rowGrid.Children.Add(typeBox);
            AttachContextMenuEvents(typeBox, rowGrid);

            // 🔒 Checkbox PK
            var pkCheck = new CheckBox
            {
                Content = "PK",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
                IsEnabled = !isPreview
            };
            Grid.SetColumn(pkCheck, 3);
            rowGrid.Children.Add(pkCheck);
            AttachContextMenuEvents(pkCheck, rowGrid);

            // 🟨 Checkbox NULL
            var nullableCheck = new CheckBox
            {
                Content = "NULL",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
                IsEnabled = !isPreview
            };
            Grid.SetColumn(nullableCheck, 4);
            rowGrid.Children.Add(nullableCheck);
            AttachContextMenuEvents(nullableCheck, rowGrid);

            // 🔵 Conector dreapta
            var rightConnector = new Rectangle
            {
                Width = 8,
                Height = 12,
                Fill = Brushes.DodgerBlue,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Cross,
                Tag = "Connector"
            };

            rightConnector.MouseLeftButtonDown += (s, e) =>
            {
                ConnectionPointClicked?.Invoke(this, new ConnectionPointEventArgs("Right", rightConnector, e));
                e.Handled = true;
            };
            Grid.SetColumn(rightConnector, 5);
            rowGrid.Children.Add(rightConnector);

            // 🧲 Hover logic pe toată linia
            rowGrid.MouseEnter += (_, _) =>
            {
                leftConnector.Visibility = Visibility.Visible;
                rightConnector.Visibility = Visibility.Visible;
            };
            rowGrid.MouseLeave += (_, _) =>
            {
                leftConnector.Visibility = Visibility.Collapsed;
                rightConnector.Visibility = Visibility.Collapsed;
            };

            // Add the rowGrid to parent grid
            Grid.SetRow(rowGrid, rowIndex);
            Grid.SetColumnSpan(rowGrid, 2);
            grid.Children.Add(rowGrid);
        }

        public void AddRow()
        {
            if (_entityGrid == null)
                return;

            int newRow = _entityGrid.RowDefinitions.Count - 1;

            _entityGrid.RowDefinitions.Insert(newRow, new RowDefinition { Height = GridLength.Auto });
            AddStyledDataRow(_entityGrid, newRow, "", "VARCHAR", isPreview: false);
        }

        public void RemoveRowAt(UIElement targetRow)
        {
            if (_entityGrid == null || targetRow == null)
                return;

            if (_entityGrid.Children.Contains(targetRow))
            {
                _entityGrid.Children.Remove(targetRow);
            }
        }

        public void ChangeHeaderBackground(Brush newBackground)
        {
            if (_headerTextBox != null)
            {
                _headerTextBox.Background = newBackground;
            }
        }
    }
}
