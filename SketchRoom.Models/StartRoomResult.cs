using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SketchRoom.Models
{
    public class StartRoomResult
    {
        public bool Success { get; set; }
        public List<Participant> Participants { get; set; }
    }
}
