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
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Z80andrew.RetroImage.Common;
using Z80andrew.RetroImage.Exntensions;
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
        private float _zoomStep = 0.25f;
        private PixelRect MinImageBounds = new PixelRect(0, 0, 320, 200);
        private PixelRect _screenBounds;
        private string _fileDialogFolder;

        #region properties

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

        private int _imageViewHeight;
        public int ImageViewHeight
        {
            get => _imageViewHeight;
            set => this.RaiseAndSetIfChanged(ref _imageViewHeight, value);
        }

        private float _imageZoom;
        public float ImageZoom
        {
            get => _imageZoom;
            set => this.RaiseAndSetIfChanged(ref _imageZoom, value);
        }

        private string _zoomText;
        public string ZoomText
        {
            get => _zoomText;
            set => this.RaiseAndSetIfChanged(ref _zoomText, value);
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

        private string[] _selectedFiles;
        public string[] SelectedFiles
        {
            get => _selectedFiles;
            set => this.RaiseAndSetIfChanged(ref _selectedFiles, value);
        }

        private string _selectedFolder;
        public string SelectedFolder
        {
            get => _selectedFolder;
            set => this.RaiseAndSetIfChanged(ref _selectedFolder, value);
        }

        #endregion

        public ReactiveCommand<Unit, Unit> OpenFilesCommand { get; }
        public Interaction<string, string[]> ShowFileDialog { get; }
        public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; }
        public Interaction<string, string?> ShowFolderDialog { get; }

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

            IsFullScreen = false;
            ImageViewWidth = 1;
            ImageViewHeight = 1;

            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            Image<Rgba32> blankImg = (Image<Rgba32>)Image.Load(assets.Open(new Uri(@"avares://RetroImage/Assets/Images/empty.png")));
            _blankBitmap = ConvertImageToBitmap(blankImg);

            ImagePaths = new string[] { "" };
            ImageIndex = 0;

            OpenFilesCommand = ReactiveCommand.CreateFromTask(OpenFilesAsync);
            ShowFileDialog = new Interaction<string, string[]>();

            OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
            ShowFolderDialog = new Interaction<string, string?>();

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
                    InitImageFromPath(ImagePaths[index]);
                });

            this.WhenAnyValue(model => model.ImageZoom)
                .Subscribe(index =>
                {
                    ImageViewWidth = (int)(_baseAtariImage == null ? MinImageBounds.Width : _baseAtariImage.Width * ImageZoom);
                    ImageViewHeight = (int)(_baseAtariImage == null ? MinImageBounds.Height : _baseAtariImage.RenderHeight * ImageZoom);
                    ZoomText = $"{ImageZoom * 100}%";
                });

            this.WhenAnyValue(model => model.SelectedFolder)
                .Subscribe(path =>
                {
                    SetImagePaths(new List<string> { path });
                });

            this.WhenAnyValue(model => model.SelectedFiles)
                .Subscribe(paths =>
                {
                    SetImagePaths(paths?.ToList());
                });

            var startupImageUri = @"avares://RetroImage/Assets/Images/MAGICMTN.PC1";

            using (var imageStream = assets.Open(new Uri(startupImageUri)))
            {
                InitImageFromStream(imageStream, startupImageUri);
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
            if (paths == null) return;

            ImageIndex = 0;

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
                    foreach (var filePath in Directory.EnumerateFiles(path, "*.*", enumOptions))
                    {
                        imagePaths.Add(filePath);
                    }
                }
            }

            ImagePaths = imagePaths.Where(x => _imageFormatService.fileExtensionServices.Keys.Contains(Path.GetExtension(x).ToUpper())).ToArray();

            if (ImagePaths.Length > 0) InitImageFromPath(ImagePaths[0]);
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

        private void InitImageFromStream(Stream imageStream, string imageName)
        {
            _baseAtariImage = _imageFormatService.GetImageServiceForFilePath(imageName)?.GetImage(imageStream, imageName);
            InitImage(_baseAtariImage, imageName);
        }

        internal void InitImageFromPath(string imagePath)
        {
            _baseAtariImage = _imageFormatService.GetImageServiceForFilePath(imagePath)?.GetImage(imagePath);
            InitImage(_baseAtariImage, imagePath);
        }

        private void InitImage(AtariImageModel image, string imagePath)
        {
            if (image != null)
            {
                ResetAnimations();
                SetImageLabel(imagePath, ImageIndex, ImagePaths.Length);
                BaseImage = ConvertImageToBitmap(_baseAtariImage.Image);

                ImageZoom = 0;

                if (IsFullScreen)
                {
                    ImageZoom = GetZoomForBounds(_screenBounds);
                    PrevImageZoom = GetZoomForBounds(MinImageBounds.Mutiply(2)); // Image changed while in fullscreen, so invalidate previous zoom value
                }

                else ImageZoom = GetZoomForBounds(MinImageBounds.Mutiply(2));

                InitAnimations(_baseAtariImage.Animations);
            }
        }

        private void SetImageLabel(string imagePath, int imageIndex, int numImages)
        {
            CurrentImageName = $"{Path.GetFileName(imagePath)} ({imageIndex + 1}/{numImages})";
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
                if (modification == Zoom.Increase) ImageZoom += _zoomStep;
                else if (modification == Zoom.Decrease)
                {
                    ImageZoom -= _zoomStep;
                    if (ImageViewWidth < MinImageBounds.Width || ImageViewHeight < MinImageBounds.Height) ImageZoom += _zoomStep;
                }
            }
        }

        internal void ToggleFullScreen(bool isFullScreen, PixelRect screenBounds)
        {
            if (isFullScreen)
            {
                PrevImageZoom = ImageZoom;
                _screenBounds = screenBounds;
                ImageZoom = GetZoomForBounds(_screenBounds);
            }

            else
            {
                ImageZoom = PrevImageZoom;
            }

            IsFullScreen = isFullScreen;
        }

        private float GetZoomForBounds(PixelRect bounds)
        {
            var maxWidthZoom = (float)bounds.Width / _baseAtariImage.Width;
            var maxHeightZoom = (float)bounds.Height / _baseAtariImage.RenderHeight;

            return Math.Min(maxWidthZoom, maxHeightZoom);
        }

        private async Task OpenFolderAsync()
        {
            var currentFolder = String.IsNullOrEmpty(_fileDialogFolder) ? Constants.DefaultPath : _fileDialogFolder;
            var folderName = await ShowFolderDialog.Handle(currentFolder);

            if (folderName is not null)
            {
                SelectedFolder = folderName;
            }
        }

        private async Task OpenFilesAsync()
        {
            var fileNames = await ShowFileDialog.Handle(_fileDialogFolder);

            if (fileNames.Any())
            {
                SelectedFiles = fileNames;
                _fileDialogFolder = Path.GetDirectoryName(fileNames.First());
            }
        }
    }
}
