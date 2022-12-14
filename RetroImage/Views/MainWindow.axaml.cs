using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using RetroImage.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using Z80andrew.RetroImage.Common;
using Z80andrew.RetroImage.Services;
using static Z80andrew.RetroImage.Common.Constants;

namespace RetroImage.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        DegasService degasService;
        public MainWindow()
        {
            InitializeComponent();

            PrevImageButton.PointerEnter += PointerEvent;
            NextImageButton.PointerEnter += PointerEvent;
            FullScreenToggleButton.PointerEnter += PointerEvent;
            PrevImageButton.PointerLeave += PointerEvent;
            NextImageButton.PointerLeave += PointerEvent;
            FullScreenToggleButton.PointerLeave += PointerEvent;

            ZoomInButton.Click += ZoomButton_Click;
            ZoomOutButton.Click += ZoomButton_Click;

            AddHandler(DragDrop.DropEvent, DropEvent);
            AddHandler(KeyDownEvent, KeyboardEvent);

            this.WhenActivated(d => d(ViewModel.ShowFolderDialog.RegisterHandler(WindowShowFolderDialog)));
            this.WhenActivated(d => d(ViewModel.ShowFileDialog.RegisterHandler(WindowShowFileDialog)));
        }

        private void ZoomButton_Click(object? sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button.Name == ZoomInButton.Name) ViewModel.ModifyZoom(Zoom.Increase);
            else if (button.Name == ZoomOutButton.Name) ViewModel.ModifyZoom(Zoom.Decrease);
        }

        public void ToggleFullScreenEvent(object? sender, RoutedEventArgs args)
        {
            ToggleFullScreen();
        }

        public void PointerEvent(object? sender, RoutedEventArgs args)
        {
            var button = sender as Button;

            if ((button.Name == PrevImageButton.Name || button.Name == NextImageButton.Name)
                && ViewModel.ImagePaths.Length < 2) return;

            bool mouseEntered = args.RoutedEvent.Name == "PointerEnter";
            button.Opacity = mouseEntered ? 1 : 0;
        }

        private void KeyboardEvent(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right) ViewModel.ShowNextImageCommand.Execute(null);
            else if (e.Key == Key.Left) ViewModel.ShowPrevImageCommand.Execute(null);
            else if (e.Key == Key.Up) ViewModel.ModifyZoom(Zoom.Increase);
            else if (e.Key == Key.Down) ViewModel.ModifyZoom(Zoom.Decrease);
            else if (e.Key == Key.Escape)
            {
                ToggleFullScreen();
            }
        }

        private void ToggleFullScreen()
        {
            this.CanResize = this.WindowState == WindowState.FullScreen ? false : true;

            this.WindowState = this.WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;

            if (this.WindowState != WindowState.FullScreen) this.SizeToContent = SizeToContent.WidthAndHeight;

            var screenBounds = this.Screens.ScreenFromPoint(this.Position).Bounds;
            ViewModel.ToggleFullScreen(this.WindowState == WindowState.FullScreen, screenBounds);
        }

        internal void DropEvent(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
            {
                ViewModel.SetImagePaths(e.Data.GetFileNames());
            }
        }

        private async Task WindowShowFolderDialog(InteractionContext<string, string?> interaction)
        {
            var dialog = new OpenFolderDialog
            {
                Directory = !string.IsNullOrEmpty(interaction?.Input) ? interaction.Input : AppDomain.CurrentDomain.BaseDirectory
            };

            var folderPath = await dialog.ShowAsync(this);
            interaction?.SetOutput(folderPath);
        }

        private async Task WindowShowFileDialog(InteractionContext<string, string[]> interaction)
        {
            var dialog = new OpenFileDialog();

            dialog.AllowMultiple = true;

            if (!string.IsNullOrEmpty(interaction?.Input))
                dialog.Directory = Path.GetFullPath(interaction.Input);

            else
                dialog.Directory = Constants.DefaultPath;

            var filePaths = await dialog.ShowAsync(this);
            interaction?.SetOutput(filePaths);
        }
    }
}
