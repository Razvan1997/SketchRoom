using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Models
{
    public abstract class WhiteBoardElement : IWhiteBoardElement
    {
        /// <summary>
        /// Reprezentarea vizuală (UIElement) care va fi adăugată în Canvas.
        /// </summary>
        public abstract UIElement Visual { get; }

        /// <summary>
        /// Bounding box-ul elementului (folosit la selecție, snapping etc.)
        /// </summary>
        public abstract Rect Bounds { get; }

        /// <summary>
        /// Setează poziția de bază a elementului (opțional override dacă e relevant).
        /// </summary>
        public virtual void SetPosition(Point position) { }

        /// <summary>
        /// Poate fi folosit pentru redenumire, tooltip, context menu, etc.
        /// </summary>
        public virtual string? Label { get; set; }
    }
}
