using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.States
{
    public class StateMachineShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;
        private readonly ObservableCollection<TransitionModel> _transitions = new();

        public bool IsStartState { get; set; } = false;

        public StateMachineShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var preview = new StackPanel();

            preview.Children.Add(new TextBlock
            {
                Text = "State",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            preview.Children.Add(new TextBlock
            {
                Text = "entry / ...",
                Foreground = Brushes.LightGray,
                FontSize = 10,
                Margin = new Thickness(4, 0, 4, 0)
            });

            preview.Children.Add(new TextBlock
            {
                Text = "exit / ...",
                Foreground = Brushes.LightGray,
                FontSize = 10,
                Margin = new Thickness(4, 0, 4, 0)
            });

            preview.Children.Add(new TextBlock
            {
                Text = "on Event [cond] / action -> TargetState",
                Foreground = Brushes.Gray,
                FontSize = 9,
                Margin = new Thickness(6, 2, 4, 0)
            });

            return new Viewbox
            {
                Width = 100,
                Height = 100,
                Stretch = Stretch.Uniform,
                Child = new Border
                {
                    BorderBrush = Brushes.DeepSkyBlue,
                    BorderThickness = new Thickness(1.5),
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    CornerRadius = new CornerRadius(8),
                    Child = preview
                }
            };
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var nameBox = new TextBox
            {
                Text = "StateName",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                TextAlignment = TextAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(4)
            };

            var entryBox = new TextBox
            {
                Text = "entry /",
                Foreground = Brushes.LightGray,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4)
            };

            var exitBox = new TextBox
            {
                Text = "exit /",
                Foreground = Brushes.LightGray,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4)
            };

            var transitionsPanel = new StackPanel { Margin = new Thickness(4) };

            var addTransitionBtn = new Button
            {
                Content = "+ Add Transition",
                Background = Brushes.Transparent,
                Foreground = Brushes.DeepSkyBlue,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            addTransitionBtn.Click += (s, e) =>
            {
                var transition = new TransitionModel
                {
                    Trigger = "onEvent",
                    Condition = "[cond]",
                    Action = "action()",
                    TargetState = "Target"
                };
                _transitions.Add(transition);
                transitionsPanel.Children.Add(CreateTransitionRow(transition, transitionsPanel));
            };

            // Add Start State marker (visual indicator)
            var startCircle = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.DeepSkyBlue,
                Margin = new Thickness(2),
                Visibility = IsStartState ? Visibility.Visible : Visibility.Collapsed
            };

            var header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { startCircle, nameBox }
            };

            var mainStack = new StackPanel();
            mainStack.Children.Add(header);
            mainStack.Children.Add(entryBox);
            mainStack.Children.Add(exitBox);
            mainStack.Children.Add(new Rectangle { Height = 1, Fill = Brushes.Gray, Margin = new Thickness(0, 4, 0, 4) });
            mainStack.Children.Add(transitionsPanel);
            mainStack.Children.Add(addTransitionBtn);

            return new Grid
            {
                Children =
            {
                new Border
                {
                    BorderBrush = Brushes.DeepSkyBlue,
                    BorderThickness = new Thickness(1.5),
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(4),
                    Child = mainStack
                }
            }
            };
        }

        private UIElement CreateTransitionRow(TransitionModel transition, Panel parent)
        {
            var row = new DockPanel { Margin = new Thickness(2), LastChildFill = false };

            var triggerBox = new TextBox
            {
                Text = transition.Trigger,
                Width = 60,
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            var conditionBox = new TextBox
            {
                Text = transition.Condition,
                Width = 60,
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            var actionBox = new TextBox
            {
                Text = transition.Action,
                Width = 60,
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            var targetBox = new TextBox
            {
                Text = transition.TargetState,
                Width = 60,
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            var removeBtn = new Button
            {
                Content = "✖",
                Width = 22,
                Height = 22,
                Background = Brushes.Transparent,
                Foreground = Brushes.Red,
                BorderBrush = Brushes.Gray,
                Margin = new Thickness(2),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(0)
            };

            removeBtn.Click += (s, e) =>
            {
                parent.Children.Remove(row);
                _transitions.Remove(transition);
            };

            row.Children.Add(triggerBox);
            row.Children.Add(conditionBox);
            row.Children.Add(actionBox);
            row.Children.Add(new TextBlock { Text = "->", Foreground = Brushes.White, Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(targetBox);
            row.Children.Add(removeBtn);

            return row;
        }
    }

    public class TransitionModel
    {
        public string Trigger { get; set; } = "";
        public string Condition { get; set; } = "";
        public string Action { get; set; } = "";
        public string TargetState { get; set; } = "";
    }

}
