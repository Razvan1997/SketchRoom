using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SketchRoom.Models.DTO
{
    public class DrawLineDto
    {
        public string SessionCode { get; set; }
        public List<PointDto> Points { get; set; } = new();
        public string Color { get; set; } = "Black";
        public double Thickness { get; set; } = 2.0;
    }
}
