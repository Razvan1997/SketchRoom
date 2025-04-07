using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using WhiteBoardModule.ViewModels;

namespace WhiteBoardModule.Views
{
    /// <summary>
    /// Interaction logic for WhiteBoardView.xaml
    /// </summary>
    public partial class WhiteBoardView : UserControl
    {
        private bool _isLeftToolsVisible = false;
        private TranslateTransform _leftToolsTransform;
        public WhiteBoardView()
        {
            InitializeComponent();

            this.Loaded += WhiteBoardView_Loaded;
            _leftToolsTransform = new TranslateTransform { X = 0 }; // pleacă din afara ecranului
            LeftToolsView.RenderTransform = _leftToolsTransform;
            LeftToolsView.Visibility = Visibility.Collapsed;
        }

        private void WhiteBoardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not WhiteBoardViewModel vm)
                return;

            vm.SetControlAdapter(Whiteboard);

            Whiteboard.LineDrawn += vm.OnLineDrawn;
            Whiteboard.MouseMoved += vm.OnMouseMoved;
            Whiteboard.LivePointDrawn += vm.OnDrawPointLive;
        }

        private void CollapseSvg_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isLeftToolsVisible)
            {
                LeftToolsView.Visibility = Visibility.Visible;

                var storyboard = new Storyboard();

                var slideIn = new DoubleAnimation
                {
                    From = 200, // din dreapta, în afara panoului
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    EasingFunction = new ElasticEase
                    {
                        Oscillations = 1,
                        Springiness = 4,
                        EasingMode = EasingMode.EaseOut
                    }
                };
                Storyboard.SetTarget(slideIn, _leftToolsTransform);
                Storyboard.SetTargetProperty(slideIn, new PropertyPath(TranslateTransform.XProperty));

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300),
                };
                Storyboard.SetTarget(fadeIn, LeftToolsView);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath(UIElement.OpacityProperty));

                LeftToolsView.Opacity = 0;

                storyboard.Children.Add(slideIn);
                storyboard.Children.Add(fadeIn);
                storyboard.Begin();
                _isLeftToolsVisible = true;
            }
            else
            {
                var storyboard = new Storyboard();

                var slideOut = new DoubleAnimation
                {
                    From = 0,
                    To = -50,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(slideOut, _leftToolsTransform);
                Storyboard.SetTargetProperty(slideOut, new PropertyPath(TranslateTransform.XProperty));

                var fadeOut = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(250),
                };
                Storyboard.SetTarget(fadeOut, LeftToolsView);
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));

                storyboard.Children.Add(slideOut);
                storyboard.Children.Add(fadeOut);

                storyboard.Completed += (s, a) =>
                {
                    LeftToolsView.Visibility = Visibility.Collapsed;
                };

                storyboard.Begin();
                _isLeftToolsVisible = false;
            }
        }
    }
}
