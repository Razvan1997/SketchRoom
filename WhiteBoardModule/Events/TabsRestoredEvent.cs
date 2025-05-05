using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteBoard.Core.Models;

namespace WhiteBoardModule.Events
{
    public class TabsRestoredEvent : PubSubEvent<List<SavedWhiteBoardModel>> { }
}
