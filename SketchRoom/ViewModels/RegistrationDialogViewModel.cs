using SketchRoom.Database;
using SketchRoom.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SketchRoom.ViewModels
{
    public class RegistrationDialogViewModel : BindableBase
    {
        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        public DelegateCommand SaveCommand { get; }

        public RegistrationDialogViewModel()
        {
            SaveCommand = new DelegateCommand(OnSave);
        }

        private void OnSave()
        {
            var user = new LocalUser
            {
                FirstName = FirstName,
                LastName = LastName,
                ImageBase64 = File.Exists(ImagePath) ? Convert.ToBase64String(File.ReadAllBytes(ImagePath)) : null
            };

            SecureStorage.SaveUser(user);

            //var user2 = SecureStorage.LoadUser();
            //if (user2?.ProfileImageBytes != null)
            //{
            //    var ProfileImage = GetImageFromBytes(user2.ProfileImageBytes);
            //}
        }

        private BitmapImage GetImageFromBytes(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            using var ms = new MemoryStream(imageBytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze(); // important pentru WPF bindings
            return image;
        }
    }
}
