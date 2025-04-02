
using System.Windows.Controls.Primitives;
using System.Windows;

namespace WalkthroughDemo
{
    public static class Walkthrough
    {
        public static readonly DependencyProperty StepIndexProperty =
            DependencyProperty.RegisterAttached("StepIndex", typeof(int), typeof(Walkthrough), new PropertyMetadata(-1));

        public static int GetStepIndex(DependencyObject obj) => (int)obj.GetValue(StepIndexProperty);
        public static void SetStepIndex(DependencyObject obj, int value) => obj.SetValue(StepIndexProperty, value);

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.RegisterAttached("Description", typeof(string), typeof(Walkthrough), new PropertyMetadata(""));

        public static string GetDescription(DependencyObject obj) => (string)obj.GetValue(DescriptionProperty);
        public static void SetDescription(DependencyObject obj, string value) => obj.SetValue(DescriptionProperty, value);

        public static readonly DependencyProperty PopupPlacementProperty =
            DependencyProperty.RegisterAttached("PopupPlacement", typeof(PlacementMode), typeof(Walkthrough), new PropertyMetadata(PlacementMode.Bottom));

        public static PlacementMode GetPopupPlacement(DependencyObject obj) => (PlacementMode)obj.GetValue(PopupPlacementProperty);
        public static void SetPopupPlacement(DependencyObject obj, PlacementMode value) => obj.SetValue(PopupPlacementProperty, value);
        public static readonly DependencyProperty PopupTypeProperty =
        DependencyProperty.RegisterAttached(
            "PopupType",
            typeof(WalkthroughPopupType),
            typeof(Walkthrough),
            new PropertyMetadata(WalkthroughPopupType.Default));

        public static void SetPopupType(DependencyObject element, WalkthroughPopupType value)
        {
            element.SetValue(PopupTypeProperty, value);
        }

        public static WalkthroughPopupType GetPopupType(DependencyObject element)
        {
            return (WalkthroughPopupType)element.GetValue(PopupTypeProperty);
        }

        public static readonly DependencyProperty HighlightPulseProperty =
    DependencyProperty.RegisterAttached("HighlightPulse", typeof(bool), typeof(Walkthrough), new PropertyMetadata(false));

        public static bool GetHighlightPulse(DependencyObject obj) => (bool)obj.GetValue(HighlightPulseProperty);
        public static void SetHighlightPulse(DependencyObject obj, bool value) => obj.SetValue(HighlightPulseProperty, value);

        public static readonly DependencyProperty HighlightRGBBorderProperty =
            DependencyProperty.RegisterAttached("HighlightRGBBorder", typeof(bool), typeof(Walkthrough), new PropertyMetadata(false));

        public static bool GetHighlightRGBBorder(DependencyObject obj) => (bool)obj.GetValue(HighlightRGBBorderProperty);
        public static void SetHighlightRGBBorder(DependencyObject obj, bool value) => obj.SetValue(HighlightRGBBorderProperty, value);

        public static readonly DependencyProperty HighlightEffectProperty =
            DependencyProperty.RegisterAttached(
                "HighlightEffect",
                typeof(HighlightEffect),
                typeof(Walkthrough),
                new PropertyMetadata(HighlightEffect.None));

        public static void SetHighlightEffect(DependencyObject element, HighlightEffect value)
        {
            element.SetValue(HighlightEffectProperty, value);
        }

        public static HighlightEffect GetHighlightEffect(DependencyObject element)
        {
            return (HighlightEffect)element.GetValue(HighlightEffectProperty);
        }

    }

}
