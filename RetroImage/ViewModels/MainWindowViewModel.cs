using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Z80andrew.RetroImage.Services;
using static Z80andrew.RetroImage.Common.Constants;
using Animation = Z80andrew.RetroImage.Models.Animation;

namespace RetroImage.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private DispatcherTimer[] _timers;

        private IBitmap _blankBitmap;

        private ImageFormatService _imageFormatService;

        private bool _animate;
        public bool Animate
        {
            get => _animate;
            set => this.RaiseAndSetIfChanged(ref _animate, value);
        }

        private string[] _imagePaths;
        public string[] ImagePaths
        {
            get => _imagePaths;
            set => this.RaiseAndSetIfChanged(ref _imagePaths, value);
        }

        private int _imageIndex;
        public int ImageIndex
        {
            get => _imageIndex;
            set => this.RaiseAndSetIfChanged(ref _imageIndex, value);
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

        public ICommand ShowNextImageCommand { get; }
        public ICommand ShowPrevImageCommand { get; }

        public MainWindowViewModel()
        {
            _timers = new DispatcherTimer[4];
            _imageFormatService = new ImageFormatService();
            Animate = true;

            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            Image<Rgba32> blankImg = (Image<Rgba32>)Image.Load(assets.Open(new Uri(@"avares://RetroImage/Assets/Images/empty.png")));
            _blankBitmap = ConvertImageToBitmap(blankImg);

            SetImagePaths(new string[] { @"D:/Temp/AtariPics/DEGAS/MAGICMTN.PC1" });
            //ImagePaths = new string[] {  @"D:/Temp/AtariPics/IFF/KINGTUT.IFF" };
            //ImagePaths = new string[] { "D:/Temp/AtariPics/TINY/DRAGON.TN1 }";

            ShowNextImageCommand = ReactiveCommand.Create(() =>
            {
                ImageIndex = ImageIndex == ImagePaths.Length - 1 ? 0 : ImageIndex + 1;
            });

            ShowPrevImageCommand = ReactiveCommand.Create(() =>
            {
                ImageIndex = ImageIndex == 0 ? ImagePaths.Length - 1 : ImageIndex - 1;
            });

            this.WhenAnyValue(model => model.ImageIndex)
                .Subscribe(index =>
                {
                    InitImage(ImagePaths[index]);
                });
        }

        internal void SetImagePaths(IEnumerable<string> paths)
        {
            var imagePaths = new List<string>();

            foreach (string path in paths)
            {
                if (File.Exists(path)) imagePaths.Add(path);

                else if (Directory.Exists(path))
                {
                    foreach (var filePath in Directory.GetFiles(path))
                    {
                        imagePaths.Add(filePath);
                    }
                }
            }

            ImagePaths = imagePaths.ToArray();

            if (ImagePaths.Length > 0) InitImage(ImagePaths[0]);
        }

        private void ResetAnimations()
        {
            AnimationLayer1Image = _blankBitmap;
            AnimationLayer2Image = _blankBitmap;
            AnimationLayer3Image = _blankBitmap;
            AnimationLayer4Image = _blankBitmap;

            foreach (var timer in _timers)
            {
                timer?.Stop();
            }
        }

        internal void InitImage(string imagePath)
        {
            var atariImage = _imageFormatService.GetImageServiceForFileExtension(imagePath)?.GetImage(imagePath);

            if (atariImage != null)
            {
                ResetAnimations();

                CurrentImageName = Path.GetFileName(imagePath);
                BaseImage = ConvertImageToBitmap(atariImage.Image);

                using (var fileStream = new FileStream(@"d:\temp\ataripics\output.png", FileMode.Create))
                {
                    var encoder = new PngEncoder();
                    encoder.BitDepth = PngBitDepth.Bit4;
                    encoder.ColorType = PngColorType.Palette;
                    encoder.CompressionLevel = PngCompressionLevel.BestCompression;
                    atariImage.Image.SaveAsPng(fileStream, encoder);
                }

                InitAnimations(atariImage.Animations);
            }
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
            foreach (var animation in animations)
            {
                Debug.WriteLine($"Setting up animation {animation.AnimationLayer}");

                if (animation.Direction != AnimationDirection.None)
                {
                    _timers[animation.AnimationLayer] = new DispatcherTimer()
                    {
                        Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(animation.Delay * 2)),
                        IsEnabled = true
                    };

                    _timers[animation.AnimationLayer].Tick += (sender, e) => AnimationTimer_Elapsed(sender, e, animation);
                }
            }
        }

        private void AnimationTimer_Elapsed(object? sender, EventArgs e, Animation animation)
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
