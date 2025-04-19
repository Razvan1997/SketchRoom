using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.Containers
{
    public class HorizontalPoolLineTwoRender : IShapeRenderer
    {
        private readonly bool _withBindings;

        public HorizontalPoolLineTwoRender(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var previewGrid = new Grid
            {
                Width = 60,
                Height = 60,
                Background = Brushes.Transparent
            };

            // 3 lane-uri
            for (int i = 0; i < 3; i++)
                previewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Coloane: Pool + Lane
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Pool label
            var poolLabel = new TextBlock
            {
                Text = "Pool",
                FontSize = 6,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                LayoutTransform = new RotateTransform(-90),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var poolBorder = new Border
            {
                Background = Brushes.Black,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Width = 10,
                Child = poolLabel
            };

            Grid.SetColumn(poolBorder, 0);
            Grid.SetRowSpan(poolBorder, 3);
            previewGrid.Children.Add(poolBorder);

            for (int i = 0; i < 3; i++)
            {
                var laneLabel = new TextBlock
                {
                    Text = $"Lane {i + 1}",
                    FontSize = 6,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    LayoutTransform = new RotateTransform(-90),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var laneBorder = new Border
                {
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    Width = 10,
                    Child = laneLabel
                };

                Grid.SetRow(laneBorder, i);
                Grid.SetColumn(laneBorder, 1);
                previewGrid.Children.Add(laneBorder);

                var contentBorder = new Border
                {
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.Transparent
                };

                Grid.SetRow(contentBorder, i);
                Grid.SetColumn(contentBorder, 2);
                previewGrid.Children.Add(contentBorder);
            }

            return new Viewbox
            {
                Width = 48,
                Height = 48,
                Stretch = Stretch.Uniform,
                Child = previewGrid
            };
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            // 3 lane-uri
            for (int i = 0; i < 3; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Coloane: Pool, Lane labels, Content
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Pool TextBox (rotit)
            var poolBox = new TextBox
            {
                Text = "Pool",
                FontWeight = FontWeights.Bold,
                FontSize = preferences.FontSize,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                TextAlignment = TextAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                LayoutTransform = new RotateTransform(-90),
                IsReadOnly = false
            };

            var poolBorder = new Border
            {
                Background = Brushes.Black,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                Width = 30,
                Child = poolBox
            };

            Grid.SetColumn(poolBorder, 0);
            Grid.SetRowSpan(poolBorder, 3);
            grid.Children.Add(poolBorder);

            for (int i = 0; i < 3; i++)
            {
                var laneBox = new TextBox
                {
                    Text = $"Lane {i + 1}",
                    FontWeight = preferences.FontWeight,
                    FontSize = preferences.FontSize,
                    Foreground = Brushes.White,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    TextAlignment = TextAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    LayoutTransform = new RotateTransform(-90),
                    IsReadOnly = false
                };

                var laneBorder = new Border
                {
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    Width = 30,
                    Child = laneBox
                };

                Grid.SetRow(laneBorder, i);
                Grid.SetColumn(laneBorder, 1);
                grid.Children.Add(laneBorder);

                var contentBorder = new Border
                {
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.Transparent
                };

                Grid.SetRow(contentBorder, i);
                Grid.SetColumn(contentBorder, 2);
                grid.Children.Add(contentBorder);
            }

            return grid;
        }
    }
}
