using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;

namespace WalkthroughDemo
{
    public class RGBBorderAdorner : Adorner
    {
        private readonly FrameworkElement _adornedElement;
        private double _currentHue = 0;

        public RGBBorderAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _adornedElement = adornedElement as FrameworkElement;
            CompositionTarget.Rendering += OnRendering;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_adornedElement == null) return;

            var rect = new Rect(new Point(0, 0), AdornedElement.RenderSize);
            var brush = CreateHueBrush();

            drawingContext.DrawRectangle(null, new Pen(brush, 3), rect);
        }

        private Brush CreateHueBrush()
        {
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                MappingMode = BrushMappingMode.RelativeToBoundingBox,
                SpreadMethod = GradientSpreadMethod.Reflect
            };

            for (int i = 0; i <= 360; i += 60)
            {
                gradient.GradientStops.Add(new GradientStop(ColorFromHSV((_currentHue + i) % 360, 1, 1), i / 360.0));
            }

            return gradient;
        }

        private Color ColorFromHSV(double hue, double saturation, double value)
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

        private void OnRendering(object sender, EventArgs e)
        {
            _currentHue += 1;
            if (_currentHue >= 360)
                _currentHue = 0;

            InvalidateVisual();
        }
    }
}
