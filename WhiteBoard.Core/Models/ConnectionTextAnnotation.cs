using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WhiteBoard.Core.Models
{
    public class ConnectionTextAnnotation
    {
        public string Text { get; set; }
        public Point Position { get; set; }
        public double Rotation { get; set; }
        public double FontSize { get; set; }
        public string FontWeight { get; set; }
        public string ForegroundHex { get; set; }
        public TextBox? TextBoxRef { get; set; }
    }
}
