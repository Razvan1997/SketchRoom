using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DrawingStateService.States
{
    public class DrawingService
    {
        private readonly List<Polyline> _recentLines = new();
        public IReadOnlyList<Polyline> RecentLines => _recentLines.AsReadOnly();

        public Polyline StartNewLine(Point startPoint, Brush color, double thickness = 2)
        {
            var line = new Polyline
            {
                Stroke = color,
                StrokeThickness = thickness
            };
            line.Points.Add(startPoint);
            return line;
        }

        public void AddPointToLine(Polyline line, Point point)
        {
            line.Points.Add(point);
        }

        public void FinishLine(Polyline line)
        {
            if (line != null)
                _recentLines.Add(line);
        }

        public void ClearRecentLines()
        {
            _recentLines.Clear();
        }

        public void RemoveLineFromCanvas(UIElementCollection canvasChildren, Polyline line)
        {
            if (line != null)
                canvasChildren.Remove(line);
        }
    }
}
