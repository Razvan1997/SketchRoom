using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SketchRoom.Toolkit.Wpf.Controls
{
    /// <summary>
    /// Interaction logic for ColorPickerWindow.xaml
    /// </summary>
    public partial class ColorPickerWindow : Window
    {
        public Brush ResultColor { get; private set; }
        private bool _isUpdatingFromUserInput = false;
        private bool _isUpdatingFromHex = false;
        private bool _isMouseDown;
        private double _hue = 0;
        private double _saturation = 1;
        private double _value = 1;
        private SolidColorBrush _hueThumbFillBrush;

        public Brush SelectedColor { get; private set; } = new SolidColorBrush(Colors.Black);

        public ColorPickerWindow(Brush initialColor)
        {
            InitializeComponent();
            Loaded += OnLoaded;

            if (initialColor is SolidColorBrush solid)
                SetInitialColor(solid.Color);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                UpdateColorAreaBackground();
                UpdateCursor();
                UpdateHexBox();
                UpdateThumbHueColor(); // 🟢 <- adăugat aici
            }, DispatcherPriority.Render);
        }

        private void UpdateThumbHueColor()
        {
            if (HueSlider.Template.FindName("PART_Track", HueSlider) is Track track &&
                track.Thumb.Template.FindName("ThumbFill", track.Thumb) is SolidColorBrush brush)
            {
                var hueColor = ColorFromHSV(_hue, 1, 1);
                brush.Color = hueColor;
            }
        }

        private void SetInitialColor(Color color)
        {
            SelectedColor = new SolidColorBrush(color);
            ColorToHSV(color, out _hue, out _saturation, out _value);
            HueSlider.Value = _hue;
        }

        private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingFromHex || _isMouseDown) return;

            _hue = e.NewValue;
            UpdateColorAreaBackground();
            UpdateColorFromHSV();

            // 👉 Acum actualizăm thumb-ul aici, doar când hue se schimbă
            if (HueSlider.Template.FindName("PART_Track", HueSlider) is Track track &&
                track.Thumb.Template.FindName("ThumbFill", track.Thumb) is SolidColorBrush brush)
            {
                var hueColor = ColorFromHSV(_hue, 1, 1); // culoare hue pură
                brush.Color = hueColor;
            }
        }

        private void ColorArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = true;
            Mouse.Capture(ColorArea);
            UpdateSaturationValueFromMouse(Mouse.GetPosition(ColorArea));
        }

        private void ColorArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                UpdateSaturationValueFromMouse(Mouse.GetPosition(ColorArea));
            }
        }

        private void ColorArea_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = false;
            Mouse.Capture(null);
        }

        private void UpdateSaturationValueFromMouse(Point position)
        {
            double width = ColorArea.ActualWidth;
            double height = ColorArea.ActualHeight;

            if (width == 0 || height == 0)
                return;

            double clampedX = Math.Max(0, Math.Min(position.X, width));
            double clampedY = Math.Max(0, Math.Min(position.Y, height));

            _saturation = clampedX / width;
            _value = 1 - (clampedY / height);

            UpdateCursor();
            UpdateColorFromHSV(); // NU mai actualizăm hue!
        }

        private void UpdateColorAreaBackground()
        {
            var baseColor = ColorFromHSV(_hue, 1, 1);

            var background = new DrawingBrush
            {
                Stretch = Stretch.Fill,
                Drawing = new DrawingGroup
                {
                    Children =
            {
                // Layer 1: culoarea de bază (din hue)
                new GeometryDrawing(
                    new SolidColorBrush(baseColor),
                    null,
                    new RectangleGeometry(new Rect(0, 0, 1, 1))),

                // Layer 2: gradient alb -> transparent (saturation)
                new GeometryDrawing(
                    new LinearGradientBrush(
                        new GradientStopCollection
                        {
                            new GradientStop(Colors.White, 0),
                            new GradientStop(Color.FromArgb(0, 255, 255, 255), 1)
                        },
                        new Point(0, 0.5),
                        new Point(1, 0.5)),
                    null,
                    new RectangleGeometry(new Rect(0, 0, 1, 1))),

                // Layer 3: gradient transparent -> negru (value)
                new GeometryDrawing(
                    new LinearGradientBrush(
                        new GradientStopCollection
                        {
                            new GradientStop(Color.FromArgb(0, 0, 0, 0), 0),
                            new GradientStop(Colors.Black, 1)
                        },
                        new Point(0.5, 0),
                        new Point(0.5, 1)),
                    null,
                    new RectangleGeometry(new Rect(0, 0, 1, 1)))
            }
                }
            };

            ColorArea.Fill = background;
        }

        private void UpdateColorFromHSV()
        {
            var finalColor = ColorFromHSV(_hue, _saturation, _value);
            SelectedColor = new SolidColorBrush(finalColor);
            LivePreview.Fill = SelectedColor;
            UpdateHexBox();
        }

        private void UpdateCursor()
        {
            double x = _saturation * ColorArea.Width;
            double y = (1 - _value) * ColorArea.Height;

            // Setezi Canvas.Left/Top direct
            Canvas.SetLeft(ColorCursor, x - ColorCursor.Width / 2);
            Canvas.SetTop(ColorCursor, y - ColorCursor.Height / 2);
        }

        private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFromHex) return;

            string hex = HexBox.Text.Trim();

            if ((hex.Length == 7 || hex.Length == 9) && hex.StartsWith("#"))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    SelectedColor = new SolidColorBrush(color);

                    ColorToHSV(color, out double h, out double s, out double v);

                    _isUpdatingFromHex = true;
                    _hue = h;
                    _saturation = s;
                    _value = v;

                    UpdateCursor();
                    UpdateColorAreaBackground();
                    UpdateColorFromHSV();
                }
                catch
                {
                    // Invalid HEX input
                }
                finally
                {
                    _isUpdatingFromHex = false;
                }
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void UpdateHexBox()
        {
            if (SelectedColor is SolidColorBrush solid)
            {
                HexBox.Text = $"#{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}";
            }
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = (byte)value;
            byte p = (byte)(value * (1 - saturation));
            byte q = (byte)(value * (1 - f * saturation));
            byte t = (byte)(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromRgb(v, t, p),
                1 => Color.FromRgb(q, v, p),
                2 => Color.FromRgb(p, v, t),
                3 => Color.FromRgb(p, q, v),
                4 => Color.FromRgb(t, p, v),
                _ => Color.FromRgb(v, p, q),
            };
        }

        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            hue = color.GetHue();
            saturation = max == 0 ? 0 : 1d - (1d * min / max);
            value = max;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            base.OnPreviewKeyDown(e);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResultColor = SelectedColor;
            DialogResult = true;
            Close();
        }
    }

    public static class ColorExtensions
    {
        public static double GetHue(this Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            double hue = 0;

            if (max == min)
            {
                hue = 0;
            }
            else if (max == r)
            {
                hue = (60 * ((g - b) / (max - min)) + 360) % 360;
            }
            else if (max == g)
            {
                hue = (60 * ((b - r) / (max - min)) + 120) % 360;
            }
            else if (max == b)
            {
                hue = (60 * ((r - g) / (max - min)) + 240) % 360;
            }

            return hue;
        }
    }
}
