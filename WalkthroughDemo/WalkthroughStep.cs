using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows;

namespace WalkthroughDemo
{
    public class WalkthroughStep
    {
        public int StepIndex { get; set; }
        public string Description { get; set; }
        public PlacementMode PopupPlacement { get; set; }
        public FrameworkElement TargetElement { get; set; }
        public WalkthroughPopupType PopupType { get; set; } = WalkthroughPopupType.Default;
    }
}
