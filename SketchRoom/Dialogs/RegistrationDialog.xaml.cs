using Microsoft.Win32;
using SketchRoom.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SketchRoom.Dialogs
{
    /// <summary>
    /// Interaction logic for RegistrationDialog.xaml
    /// </summary>
    public partial class RegistrationDialog : Window
    {
        public RegistrationDialog()
        {
            InitializeComponent();
        }

        private void SelectImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Selectează o poză",
                Filter = "Imagini (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    ProfileImageBrush.ImageSource = bitmap;

                    // opțional: actualizează și binding-ul ImagePath
                    if (DataContext is RegistrationDialogViewModel vm)
                    {
                        vm.ImagePath = openFileDialog.FileName;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Eroare la încărcarea imaginii: " + ex.Message);
                }
            }
        }
    }
}
