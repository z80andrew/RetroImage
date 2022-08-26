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
using Z80andrew.RetroImage.Interfaces;
using System.Diagnostics;
using System;

namespace RetroImage.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private Timer[] _timers;
        public string ImagePath => @"D:/Temp/AtariPics/DEGAS/MAGICMTN.PC1";
        public DegasService degasService;

        private bool _isAnimationLayer1Visible;
        public bool IsAnimationLayer1Visible
        {
            get => _isAnimationLayer1Visible;
            set => this.RaiseAndSetIfChanged(ref _isAnimationLayer1Visible, value);
        }

        private bool _isAnimationLayer2Visible;
        public bool IsAnimationLayer2Visible
        {
            get => _isAnimationLayer2Visible;
            set => this.RaiseAndSetIfChanged(ref _isAnimationLayer2Visible, value);
        }

        private bool _isAnimationLayer3Visible;
        public bool IsAnimationLayer3Visible
        {
            get => _isAnimationLayer3Visible;
            set => this.RaiseAndSetIfChanged(ref _isAnimationLayer3Visible, value);
        }

        private bool _isAnimationLayer4Visible;
        public bool IsAnimationLayer4Visible
        {
            get => _isAnimationLayer1Visible;
            set => this.RaiseAndSetIfChanged(ref _isAnimationLayer4Visible, value);
        }

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

        private IBitmap _baseImage;
        public IBitmap BaseImage
        {
            get => _baseImage;
            set => this.RaiseAndSetIfChanged(ref _baseImage, value);
        }

        private IBitmap _animationLayer1Image;
        public IBitmap AnimationLayer1Image
        {
            get => _animationLayer1Image;
            set => this.RaiseAndSetIfChanged(ref _animationLayer1Image, value);
        }

        private IBitmap _animationLayer2Image;
        public IBitmap AnimationLayer2Image
        {
            get => _animationLayer2Image;
            set => this.RaiseAndSetIfChanged(ref _animationLayer2Image, value);
        }

        private IBitmap _animationLayer3Image;
        public IBitmap AnimationLayer3Image
        {
            get => _animationLayer3Image;
            set => this.RaiseAndSetIfChanged(ref _animationLayer3Image, value);
        }

        private IBitmap _animationLayer41mage;
        public IBitmap AnimationLayer4Image
        {
            get => _animationLayer41mage;
            set => this.RaiseAndSetIfChanged(ref _animationLayer41mage, value);
        }

        public MainWindowViewModel()
        {
            _timers = new Timer[4];
            degasService = new DegasService();
            InitImage(degasService, ImagePath);
            Animate = true;
        }

        private void ResetTimers()
        {
            foreach (var timer in _timers)
            {
                timer?.Dispose();
            }
        }

        public void InitImage(IAtariImageService imageService, string imagePath)
        {
            ResetTimers();
            var atariImage = imageService.GetImage(imagePath);
            CurrentImageName = Path.GetFileName(imagePath);
            BaseImage = ConvertImageToBitmap(atariImage.Image);
            InitAnimations(atariImage.Animations);
        }

        public IBitmap ConvertImageToBitmap(Image<Rgba32> inputImage)
        {
            IBitmap outputBitmap;

            using (var imageStream = new MemoryStream())
            {
                var imageFormat = PngFormat.Instance;
                inputImage.Save(imageStream, imageFormat);
                imageStream.Position = 0;
                outputBitmap = new Bitmap(imageStream);
            }

            return outputBitmap;
        }

        private void InitAnimations(Animation[] animations)
        {
            foreach(var animation in animations)
            {
                Debug.WriteLine($"Setting up animation {animation.AnimationLayer}");

                if (animation.Direction != AnimationDirection.None)
                {
                    _timers[animation.AnimationLayer] = new Timer()
                    {
                        Enabled = Animate,
                        Interval = animation.Delay,
                    };

                    _timers[animation.AnimationLayer].Elapsed += (sender, e) => AnimationTimer_Elapsed(sender, e, animation);
                    _timers[animation.AnimationLayer].Start();
                }
            }

            IsAnimationLayer1Visible = animations.Length > 0 ? true : false;
            IsAnimationLayer2Visible = animations.Length > 1 ? true : false;
            IsAnimationLayer3Visible = animations.Length > 2 ? true : false;
            IsAnimationLayer4Visible = animations.Length > 3 ? true : false;
        }

        private void AnimationTimer_Elapsed(object? sender, ElapsedEventArgs e, Animation animation)
        {
            if (Animate)
            {
                var imageBitmap = ConvertImageToBitmap(animation.Frames[animation.FrameIndex]);

                switch (animation.AnimationLayer)
                {
                    case 0:
                        AnimationLayer1Image = imageBitmap;
                        break;
                    case 1:
                        AnimationLayer2Image = imageBitmap;
                        break;
                    case 2:
                        AnimationLayer3Image = imageBitmap;
                        break;
                    case 3:
                        AnimationLayer4Image = imageBitmap;
                        break;
                }

                animation.AdvanceFrame();
            }
        }
    }
}
