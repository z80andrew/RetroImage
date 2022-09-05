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
using System.Linq;
using System.Windows.Input;
using Z80andrew.RetroImage.Models;
using Z80andrew.RetroImage.Services;
using static Z80andrew.RetroImage.Common.Constants;
using Animation = Z80andrew.RetroImage.Models.Animation;

namespace RetroImage.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private DispatcherTimer[] _timers;

        private IBitmap _blankBitmap;

        private AtariImageModel _baseAtariImage;

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

        private string _exportPath;
        public string ExportPath
        {
            get => _exportPath;
            set => this.RaiseAndSetIfChanged(ref _exportPath, value);
        }

        private int _imageViewWidth;
        public int ImageViewWidth
        {
            get => _imageViewWidth;
            set => this.RaiseAndSetIfChanged(ref _imageViewWidth, value);
        }

        private float _imageZoom;
        public float ImageZoom
        {
            get => _imageZoom;
            set => this.RaiseAndSetIfChanged(ref _imageZoom, value);
        }

        private float _prevImageZoom;
        public float PrevImageZoom
        {
            get => _prevImageZoom;
            set => this.RaiseAndSetIfChanged(ref _prevImageZoom, value);
        }

        public bool _isFullScreen;
        public bool IsFullScreen
        {
            get => _isFullScreen;
            set => this.RaiseAndSetIfChanged(ref _isFullScreen, value);
        }

        public ICommand ShowNextImageCommand { get; }
        public ICommand ShowPrevImageCommand { get; }
        public ICommand ToggleAnimationCommand { get; }
        public ICommand ExportImageCommand { get; }
        public ICommand ExportAllImagesCommand { get; }

        public MainWindowViewModel()
        {
            _timers = new DispatcherTimer[4];
            _imageFormatService = new ImageFormatService();
            Animate = true;
            ExportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "export");

            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            Image<Rgba32> blankImg = (Image<Rgba32>)Image.Load(assets.Open(new Uri(@"avares://RetroImage/Assets/Images/empty.png")));
            _blankBitmap = ConvertImageToBitmap(blankImg);

            //SetImagePaths(new string[] { "" });
            ImagePaths = new string[] {""} ;
            ImageIndex = 0;

            ShowNextImageCommand = ReactiveCommand.Create(() =>
            {
                ImageIndex = ImageIndex == ImagePaths.Length - 1 ? 0 : ImageIndex + 1;
            });

            ShowPrevImageCommand = ReactiveCommand.Create(() =>
            {
                ImageIndex = ImageIndex == 0 ? ImagePaths.Length - 1 : ImageIndex - 1;
            });

            ToggleAnimationCommand = ReactiveCommand.Create(() =>
            {
                Animate = !Animate;
            });

            ExportImageCommand = ReactiveCommand.Create(async () =>
            {
                await _baseAtariImage?.ExportImageToFile(ExportPath);
            });

            ExportAllImagesCommand = ReactiveCommand.Create(() =>
            {
                ExportAllImages(ImagePaths);
            });

            this.WhenAnyValue(model => model.ImageIndex)
                .Subscribe(index =>
                {
                    InitImage(ImagePaths[index]);
                });

            this.WhenAnyValue(model => model.ImageZoom)
                .Subscribe(index =>
                {
                    ImageViewWidth = (int)(_baseAtariImage == null ? 320 : _baseAtariImage.Width * ImageZoom);
                });

            var startupImageUri = @"avares://RetroImage/Assets/Images/MAGICMTN.PC1";

            using (var imageStream = assets.Open(new Uri(startupImageUri)))
            {
                InitImage(imageStream, startupImageUri);
            }
        }

        private async void ExportAllImages(string[] imagePaths)
        {
            foreach (string imagePath in imagePaths)
            {
                var atariImage = _imageFormatService.GetImageServiceForFilePath(imagePath)?.GetImage(imagePath);

                if (atariImage != null) await atariImage.ExportImageToFile(ExportPath);
            }
        }

        internal void SetImagePaths(IEnumerable<string> paths)
        {
            var imagePaths = new List<string>();

            var enumOptions = new EnumerationOptions()
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = true
            };

            foreach (string path in paths)
            {
                if (File.Exists(path)) imagePaths.Add(path);

                else if (Directory.Exists(path))
                {
                    foreach (var filePath in Directory.EnumerateFiles(path, "*.*", enumOptions).
                        Where(x => _imageFormatService.fileExtensionServices.Keys.Contains(Path.GetExtension(x).ToUpper())))
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

        private void InitImage(Stream imageStream, string imageName)
        {
            _baseAtariImage = _imageFormatService.GetImageServiceForFilePath(imageName)?.GetImage(imageStream, imageName);
            BaseImage = ConvertImageToBitmap(_baseAtariImage.Image);
            ImageZoom = _baseAtariImage.Width < 640 ? 2 : 1;
            InitAnimations(_baseAtariImage.Animations);
        }

        internal void InitImage(string imagePath)
        {
            _baseAtariImage = _imageFormatService.GetImageServiceForFilePath(imagePath)?.GetImage(imagePath);

            if (_baseAtariImage != null)
            {
                ResetAnimations();
                SetImageLabel(imagePath, ImageIndex, ImagePaths.Length);
                BaseImage = ConvertImageToBitmap(_baseAtariImage.Image);
                ImageZoom = _baseAtariImage.Width < 640 ? 2 : 1;

                InitAnimations(_baseAtariImage.Animations);
            }
        }

        private void SetImageLabel(string imagePath, int imageIndex, int numImages)
        {
            CurrentImageName = $"{Path.GetFileName(imagePath)} ({imageIndex+1}/{numImages})";
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
                        Interval = new TimeSpan(0, 0, 0, 0, (int)animation.Delay),
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

        internal void ModifyZoom(Zoom modification)
        {
            if (!IsFullScreen)
            {
                if (modification == Zoom.Increase) ImageZoom++;
                else if (modification == Zoom.Decrease) ImageZoom--;
            }
        }

        internal void ToggleFullScreen(bool isFullScreen, PixelRect screenBounds)
        {
            if(isFullScreen)
            {
                PrevImageZoom = ImageZoom;

                var maxWidthZoom = (float)screenBounds.Width / _baseAtariImage.Width;
                var maxHeightZoom = (float)screenBounds.Height / _baseAtariImage.Height;

                ImageZoom = Math.Min(maxWidthZoom, maxHeightZoom);
            }

            else
            {
                ImageZoom = PrevImageZoom;
            }

            IsFullScreen = isFullScreen;
        }
    }
}
