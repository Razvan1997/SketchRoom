using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IWhiteBoardElement
    {
        /// <summary>
        /// Reprezentarea vizuală (UIElement) care va fi adăugată în Canvas.
        /// </summary>
        UIElement Visual { get; }

        /// <summary>
        /// Bounding box-ul elementului (folosit la selecție, snapping etc.)
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// Setează poziția elementului în Canvas.
        /// </summary>
        void SetPosition(Point position);

        /// <summary>
        /// Nume, descriere sau tooltip al elementului.
        /// </summary>
        string? Label { get; set; }
    }
}
