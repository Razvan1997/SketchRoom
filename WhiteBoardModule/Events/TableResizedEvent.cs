using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoardModule.Events
{
    public class TableResizeInfo
    {
        public Size NewSize { get; set; }
        public Guid SourceId { get; set; }
    }
    public class TableResizedEvent : PubSubEvent<TableResizeInfo> { }
}
