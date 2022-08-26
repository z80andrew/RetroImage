using Avalonia.Media.Imaging;
using ReactiveUI;
using System.ComponentModel;
using System.IO;
using System.Timers;
using Z80andrew.RetroImage.Models;
using Z80andrew.RetroImage.Services;
using static Z80andrew.RetroImage.Common.Constants;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using Z80andrew.RetroImage.Interfaces;
using SixLabors.ImageSharp.Processing;

namespace RetroImage.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public string ImagePath => "./Assets/Images/MAGICMTN.PC1";
        public DegasService degasService;
        private IAtariImage atariImage;

        private Animation[] _animations;

        private bool _animate;
        public bool Animate
        {
            get => _animate;
            set => this.RaiseAndSetIfChanged(ref _animate, value);
        }

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
            degasService = new DegasService();

            atariImage = degasService.GetImage(ImagePath);
            CurrentImageName = Path.GetFileName(ImagePath);
            CurrentImage = ConvertImageToBitmap(atariImage.Image);

            Animate = true;
            InitAnimations(atariImage.Animations);
        }

        public IBitmap ConvertImageToBitmap(Image<Rgba32> inputImage)
        {
            IBitmap outputBitmap;

            using (var imageStream = new MemoryStream())
            {
                inputImage.Save(imageStream, PngFormat.Instance);
                imageStream.Position = 0;
                outputBitmap = new Bitmap(imageStream);
            }

            return outputBitmap;
        }

        private void InitAnimations(Animation[] animations)
        {
            foreach (var animation in animations)
            {
                if (animation.Direction != AnimationDirection.None)
                {
                    var animationTimer = new Timer()
                    {
                        Enabled = Animate,
                        Interval = animation.Delay,
                    };

                    animationTimer.Elapsed += (sender, e) => AnimationTimer_Elapsed(sender, e, animation);

                    animationTimer.Start();
                }
            }
        }

        private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e, Animation animation)
        {
            // take the 2 source images and draw them onto the image
            atariImage.Image.Mutate(o => o
                .DrawImage(atariImage.Image, new Point(0, 0), 1f) // draw the first one top left
                .DrawImage(animation.Frames[animation.FrameIndex], new Point(0, 0), 1f) // draw the second next to it
            );
            
            animation.AdvanceFrame();

            CurrentImage = ConvertImageToBitmap(atariImage.Image);
        }
    }
}
