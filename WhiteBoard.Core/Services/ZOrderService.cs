using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Services
{
    public class ZOrderService : IZOrderService
    {
        public void BringToFront(UIElement element, Canvas canvas)
        {
            int max = canvas.Children.OfType<UIElement>()
                       .Where(e => e != element)
                       .Select(e => Panel.GetZIndex(e))
                       .DefaultIfEmpty(0)
                       .Max();

            Panel.SetZIndex(element, max + 1);
        }

        public void SendToBack(UIElement element, Canvas canvas)
        {
            int min = canvas.Children.OfType<UIElement>()
                       .Where(e => e != element)
                       .Select(e => Panel.GetZIndex(e))
                       .DefaultIfEmpty(0)
                       .Min();

            Panel.SetZIndex(element, min - 1);
        }

        public void MoveForward(UIElement element, Canvas canvas)
        {
            int current = Panel.GetZIndex(element);
            Panel.SetZIndex(element, current + 1);
        }

        public void MoveBackward(UIElement element, Canvas canvas)
        {
            int current = Panel.GetZIndex(element);
            Panel.SetZIndex(element, current - 1);
        }
    }
}
