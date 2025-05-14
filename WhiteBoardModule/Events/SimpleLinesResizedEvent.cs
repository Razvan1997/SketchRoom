using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoardModule.Events
{
    public class SimpleLinesResizedEvent : PubSubEvent<SimpleLinesResizeInfo> { }

    public class SimpleLinesResizeInfo
    {
        public Guid SourceId { get; set; }
        public Size NewSize { get; set; }
        public double VerticalOffset { get; set; }
        public bool IsLastRowExpanded { get; set; } // 🆕
    }
}
