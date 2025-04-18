using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WhiteBoard.Core.Models
{
    public class CurvedPathResult
    {
        public PathGeometry Geometry { get; set; }
        public Point LastLineStart { get; set; }
        public Point LastLineEnd { get; set; }
    }
}
