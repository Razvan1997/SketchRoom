using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SketchRoom.Models.DTO
{
    public class CursorPositionDto
    {
        public string SessionCode { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string HostImageBase64 { get; set; }
    }
}
