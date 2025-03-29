using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhiteBoardModule.Events
{
    public class SessionContext
    {
        public bool IsHost { get; set; }
        public bool IsParticipant => !IsHost;
        public string SessionCode { get; set; }
    }

    public class SessionContextEvent : PubSubEvent<SessionContext> { }
}
