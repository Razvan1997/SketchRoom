using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace WhiteBoard.Core.Models
{
    public class ObservableShape
    {
        public FrameworkElement Element { get; }
        public event Action<string, object?, object?>? PropertyChanged;

        private double _lastLeft;
        private double _lastTop;
        private double _lastWidth;
        private double _lastHeight;
        private readonly EventHandler LayoutUpdatedHandler;
        public ObservableShape(FrameworkElement element)
        {
            Element = element;
            _lastLeft = Canvas.GetLeft(element);
            _lastTop = Canvas.GetTop(element);
            _lastWidth = element.Width;
            _lastHeight = element.Height;

            LayoutUpdatedHandler = (_, _) => CheckChanges();
            element.LayoutUpdated += LayoutUpdatedHandler;
        }


        private void CheckChanges()
        {
            double left = Canvas.GetLeft(Element);
            double top = Canvas.GetTop(Element);

            if (!DoubleEquals(left, _lastLeft))
            {
                PropertyChanged?.Invoke("Left", _lastLeft, left);
                _lastLeft = left;
            }

            if (!DoubleEquals(top, _lastTop))
            {
                PropertyChanged?.Invoke("Top", _lastTop, top);
                _lastTop = top;
            }

            if (!DoubleEquals(Element.Width, _lastWidth))
            {
                PropertyChanged?.Invoke("Width", _lastWidth, Element.Width);
                _lastWidth = Element.Width;
            }

            if (!DoubleEquals(Element.Height, _lastHeight))
            {
                PropertyChanged?.Invoke("Height", _lastHeight, Element.Height);
                _lastHeight = Element.Height;
            }
        }

        private bool DoubleEquals(double a, double b) => Math.Abs(a - b) < 0.01;

        public void Dispose()
        {
            Element.LayoutUpdated -= LayoutUpdatedHandler;
        }
    }
}
