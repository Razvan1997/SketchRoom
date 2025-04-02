using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WalkthroughDemo
{
    public class WalkthroughService
    {
        private static List<WalkthroughStep> _steps;
        private static int _currentStepIndex;

        public static void Start(Window hostWindow)
        {
            var manager = new WalkthroughManager(hostWindow);
            _steps = manager.Steps.OrderBy(s => s.StepIndex).ToList();
            _currentStepIndex = 0;

            if (_steps.Any())
                ShowStep();
        }

        private static void ShowStep()
        {
            var step = _steps[_currentStepIndex];
            var overlay = new WalkthroughOverlayWindow();

            overlay.StepClosed += () =>
            {
                _currentStepIndex++;
                if (_currentStepIndex < _steps.Count)
                    ShowStep();
            };

            overlay.Show();
            overlay.ShowStep(step);
        }
    }
}
