using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SketchRoom.Models
{
    public class LocalUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? ImageBase64 { get; set; }
    }
}
