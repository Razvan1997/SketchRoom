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
using SketchRoom.Models.Enums;
using WhiteBoard.Core.Models;

namespace WhiteBoardModule.XAML.Shapes.States
{
    public class StateMachineShapeRenderer : IShapeRenderer, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private readonly ObservableCollection<TransitionModel> _transitions = new();
        private TextBox? _nameBox;
        private TextBox? _entryBox;
        private TextBox? _exitBox;
        private StackPanel? _transitionsPanel;
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

            _nameBox = nameBox;
            _entryBox = entryBox;
            _exitBox = exitBox;
            _transitionsPanel = transitionsPanel;

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

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe ||
                _nameBox == null || _entryBox == null || _exitBox == null || _transitionsPanel == null)
                return null;

            var extra = new Dictionary<string, string>
            {
                ["StateName"] = _nameBox.Text,
                ["EntryAction"] = _entryBox.Text,
                ["ExitAction"] = _exitBox.Text,
                ["IsStartState"] = IsStartState.ToString()
            };

            int i = 1;
            foreach (var row in _transitionsPanel.Children.OfType<DockPanel>())
            {
                var textBoxes = row.Children.OfType<TextBox>().ToList();

                if (textBoxes.Count >= 4)
                {
                    extra[$"T{i}_Trigger"] = textBoxes[0].Text;
                    extra[$"T{i}_Condition"] = textBoxes[1].Text;
                    extra[$"T{i}_Action"] = textBoxes[2].Text;
                    extra[$"T{i}_Target"] = textBoxes[3].Text;
                    i++;
                }
            }

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.StateMachineShape,
                Left = Canvas.GetLeft(fe),
                Top = Canvas.GetTop(fe),
                Width = fe.Width,
                Height = fe.Height,
                Name = fe.Name,
                Category = "States",
                SvgUri = null,
                ExtraProperties = extra
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (_nameBox == null || _entryBox == null || _exitBox == null || _transitionsPanel == null)
                return;

            _nameBox.Text = extraProperties.TryGetValue("StateName", out var name) ? name : "State";
            _entryBox.Text = extraProperties.TryGetValue("EntryAction", out var entry) ? entry : "entry /";
            _exitBox.Text = extraProperties.TryGetValue("ExitAction", out var exit) ? exit : "exit /";

            if (extraProperties.TryGetValue("IsStartState", out var isStart))
                IsStartState = bool.TryParse(isStart, out var b) && b;

            _transitions.Clear();
            _transitionsPanel.Children.Clear();

            int i = 1;
            while (extraProperties.ContainsKey($"T{i}_Trigger"))
            {
                var t = new TransitionModel
                {
                    Trigger = extraProperties[$"T{i}_Trigger"],
                    Condition = extraProperties.TryGetValue($"T{i}_Condition", out var c) ? c : "",
                    Action = extraProperties.TryGetValue($"T{i}_Action", out var a) ? a : "",
                    TargetState = extraProperties.TryGetValue($"T{i}_Target", out var tgt) ? tgt : ""
                };

                _transitions.Add(t);
                _transitionsPanel.Children.Add(CreateTransitionRow(t, _transitionsPanel));
                i++;
            }
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
