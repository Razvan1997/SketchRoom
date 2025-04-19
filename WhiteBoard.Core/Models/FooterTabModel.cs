using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WhiteBoard.Core.Helpers;

namespace WhiteBoard.Core.Models
{
    public class FooterTabModel :BindableModel
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public Guid Id { get; } = Guid.NewGuid(); // pentru identificare unică

        public UserControl? WhiteBoard { get; set; }
    }
}
