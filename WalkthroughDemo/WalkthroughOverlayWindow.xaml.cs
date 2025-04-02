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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WalkthroughDemo
{
    /// <summary>
    /// Interaction logic for WalkthroughOverlayWindow.xaml
    /// </summary>
    public partial class WalkthroughOverlayWindow : Window
    {
        private FrameworkElement _pulseVisual;
        public event Action StepClosed;
        public event Action SkipAllRequested;

        private WalkthroughStep _currentStep;

        public WalkthroughOverlayWindow()
        {
            InitializeComponent();
            Loaded += WalkthroughOverlayWindow_Loaded;
        }

        private void WalkthroughOverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);
        }

        public void ShowStep(WalkthroughStep step, bool isLastStep = false)
        {
            _currentStep = step;

            if (step?.TargetElement == null)
                return;

            var relativePosition = step.TargetElement.PointToScreen(new Point(0, 0));
            var targetTopLeft = this.PointFromScreen(relativePosition);
            var targetWidth = step.TargetElement.ActualWidth;
            var targetHeight = step.TargetElement.ActualHeight;

            var spotlight = new RectangleGeometry
            {
                Rect = new Rect(targetTopLeft.X, targetTopLeft.Y, targetWidth, targetHeight)
            };

            var fullScreen = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
            var combined = new CombinedGeometry(GeometryCombineMode.Exclude, fullScreen, spotlight);

            var geometryDrawing = new GeometryDrawing
            {
                Geometry = combined,
                Brush = Brushes.White
            };

            var opacityBrush = new DrawingBrush(geometryDrawing);
            OverlayBackground.OpacityMask = opacityBrush;

            FrameworkElement popupContent;

            var arrowed = new ArrowedPopup();
            arrowed.DescriptionText.Text = step.Description;

            switch (step.PopupPlacement)
            {
                case PlacementMode.Top:
                    arrowed.SetArrowRotation(180);
                    break;
                case PlacementMode.Left:
                    arrowed.SetArrowRotation(90);
                    break;
                case PlacementMode.Right:
                    arrowed.SetArrowRotation(-90);
                    break;
                case PlacementMode.Bottom:
                default:
                    arrowed.SetArrowRotation(0);
                    break;
            }

            arrowed.NextClicked += () => FadeOutAndClose();
            arrowed.SkipAllClicked += () =>
            {
                ClearVisualEffects(_currentStep);
                SkipAllRequested?.Invoke();
                this.Close();
            };

            popupContent = arrowed;

            OverlayCanvas.Children.Add(popupContent);
            popupContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double margin = 12;
            double left = targetTopLeft.X, top = targetTopLeft.Y;

            switch (step.PopupPlacement)
            {
                case PlacementMode.Top:
                    Canvas.SetLeft(popupContent, left);
                    Canvas.SetTop(popupContent, top - popupContent.DesiredSize.Height - margin);
                    break;
                case PlacementMode.Bottom:
                    Canvas.SetLeft(popupContent, left);
                    Canvas.SetTop(popupContent, top + targetHeight + margin);
                    break;
                case PlacementMode.Left:
                    Canvas.SetLeft(popupContent, left - popupContent.DesiredSize.Width - margin);
                    Canvas.SetTop(popupContent, top);
                    break;
                case PlacementMode.Right:
                default:
                    Canvas.SetLeft(popupContent, left + targetWidth + margin);
                    Canvas.SetTop(popupContent, top);
                    break;
            }

            if (Walkthrough.GetHighlightPulse(step.TargetElement))
            {
                var bounds = VisualTreeHelper.GetDescendantBounds(step.TargetElement);
                var brush = new VisualBrush(step.TargetElement)
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };

                var visualCopy = new Rectangle
                {
                    Width = bounds.Width,
                    Height = bounds.Height,
                    Fill = brush,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = false
                };

                var transform = new ScaleTransform(1.0, 1.0);
                visualCopy.RenderTransform = transform;

                OverlayCanvas.Children.Add(visualCopy);

                // Poziționează-l corect
                Canvas.SetLeft(visualCopy, targetTopLeft.X);
                Canvas.SetTop(visualCopy, targetTopLeft.Y);

                var pulseAnim = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.12,
                    Duration = TimeSpan.FromSeconds(0.8),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnim);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnim);


                // Salvează referință ca să-l ștergi mai târziu
                _pulseVisual = visualCopy;
            }

            if (Walkthrough.GetHighlightRGBBorder(step.TargetElement))
            {
                var layer = AdornerLayer.GetAdornerLayer(step.TargetElement);
                if (layer != null)
                {
                    var adorner = new RGBBorderAdorner(step.TargetElement);
                    layer.Add(adorner);
                }
            }

            if (Walkthrough.GetHighlightEffect(step.TargetElement) == HighlightEffect.Shake)
            {
                BounceEffectHelper.ApplyBounce(step.TargetElement);
            }
        }

        private void FadeOutAndClose()
        {
            ClearVisualEffects(_currentStep);

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s2, a2) =>
            {
                StepClosed?.Invoke();
                this.Close();
            };
            this.BeginAnimation(Window.OpacityProperty, fadeOut);
        }

        private void ClearVisualEffects(WalkthroughStep step)
        {
            if (step?.TargetElement == null)
                return;

            if (Walkthrough.GetHighlightPulse(step.TargetElement) &&
                step.TargetElement.RenderTransform is ScaleTransform scale)
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                step.TargetElement.RenderTransform = null;
            }

            if (_pulseVisual != null)
            {
                OverlayCanvas.Children.Remove(_pulseVisual);
                _pulseVisual = null;
            }
            if (Walkthrough.GetHighlightRGBBorder(step.TargetElement))
            {
                var layer = AdornerLayer.GetAdornerLayer(step.TargetElement);
                if (layer != null)
                {
                    var adorners = layer.GetAdorners(step.TargetElement);
                    if (adorners != null)
                    {
                        foreach (var adorner in adorners)
                        {
                            if (adorner is RGBBorderAdorner)
                                layer.Remove(adorner);
                        }
                    }
                }
            }
            BounceEffectHelper.ClearBounce(step.TargetElement);
        }
    }
}
