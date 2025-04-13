using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface ITextInteractiveShape : IInteractiveShape
    {
        /// <summary>
        /// Dă focus pe controlul text pentru editare imediată.
        /// </summary>
        void FocusText();

        /// <summary>
        /// Textul asociat formei.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Controlul intern TextBox (pentru stilizare externă).
        /// </summary>
        TextBox EditableText { get; }
        void SetPreferences();

    }
}
