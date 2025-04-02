using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace WalkthroughDemo
{
    public class WalkthroughManager
    {
        private readonly FrameworkElement _rootElement;

        public List<WalkthroughStep> Steps { get; private set; } = new();

        public WalkthroughManager(FrameworkElement rootElement)
        {
            _rootElement = rootElement;
            DiscoverSteps();
        }

        private void DiscoverSteps()
        {
            Steps.Clear();
            var allElements = FindVisualChildren<FrameworkElement>(_rootElement);

            foreach (var element in allElements)
            {
                int index = Walkthrough.GetStepIndex(element);
                var popupType = Walkthrough.GetPopupType(element);
                if (index >= 0)
                {
                    Steps.Add(new WalkthroughStep
                    {
                        StepIndex = index,
                        Description = Walkthrough.GetDescription(element),
                        PopupPlacement = Walkthrough.GetPopupPlacement(element),
                        TargetElement = element,
                        PopupType = popupType,
                    });
                }
            }

            Steps = Steps.OrderBy(s => s.StepIndex).ToList();
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T tChild)
                    yield return tChild;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }
}
