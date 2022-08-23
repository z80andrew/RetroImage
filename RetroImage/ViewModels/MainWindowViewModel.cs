using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Z80andrew.RetroImage.Services;

namespace RetroImage.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        //public string ImagePath => @"D:\Temp\DEGAS\UNCOMPRESSED\\MED\\TEST.PI2";
        public string ImagePath => @"D:\Temp\DEGAS\COMPRESSED\\MED\\STEALTHD.PC2";
        //public string ImagePath => @"D:\Temp\DEGAS\\MAGICMTN.PC1";

        private string _currentImageName;
        public string CurrentImageName
        {
            get => _currentImageName;
            set => this.RaiseAndSetIfChanged(ref _currentImageName, value);
        }

        private IBitmap _currentImage;
        public IBitmap CurrentImage
        {
            get => _currentImage;
            set => this.RaiseAndSetIfChanged(ref _currentImage, value);
        }

        public MainWindowViewModel()
        {
            CurrentImage = DegasService.ReadDegasImage(ImagePath);
            CurrentImageName = Path.GetFileName(ImagePath);
        }

    }
}
