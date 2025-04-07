using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IInteractiveShape
    {
        /// <summary>
        /// Eveniment declanșat la click pentru a gestiona selecția sau alte acțiuni.
        /// </summary>
        event MouseButtonEventHandler ShapeClicked;

        /// <summary>
        /// Eveniment declanșat când se face click pe un punct de conexiune (ex: Top, Right, Bottom, Left).
        /// </summary>
        event EventHandler<string>? ConnectionPointClicked;
        /// <summary>
        /// Returnează UIElement-ul vizual pentru a fi adăugat pe Canvas.
        /// </summary>
        UIElement Visual { get; }

        /// <summary>
        /// Marchează forma ca selectată (ex: afișează thumb-uri de resize).
        /// </summary>
        void Select();

        /// <summary>
        /// Dezactivează starea de selecție (ex: ascunde thumb-uri de resize).
        /// </summary>
        void Deselect();

        /// <summary>
        /// Setează poziția pe Canvas.
        /// </summary>
        void SetPosition(Point position);

        bool EnableConnectors { get; set; }

        void SetShape(ShapeType shape);
    }
}
