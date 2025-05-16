using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteBoard.Core.Models;

namespace WhiteBoardModule.Events
{
    public class TabsRestoredEvent : PubSubEvent<TabsRestoredPayload> { }

    public class TabsRestoredPayload
    {
        public List<SavedWhiteBoardModel> Tabs { get; set; } = [];
        public string FolderName { get; set; } = string.Empty;
    }
}
