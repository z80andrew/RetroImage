using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using RetroImage.ViewModels;
using System;
using System.Diagnostics;
using Z80andrew.RetroImage.Services;

namespace RetroImage.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        DegasService degasService;
        public MainWindow()
        {
            InitializeComponent();

            degasService = new DegasService();

            var prevButton = this.FindControl<Button>("PrevImageButton");
            var nextButton = this.FindControl<Button>("NextImageButton");
            prevButton.PointerEnter += PointerEnterEvent;
            nextButton.PointerEnter += PointerEnterEvent;
            prevButton.PointerLeave += PointerEnterEvent;
            nextButton.PointerLeave += PointerEnterEvent;

            AddHandler(DragDrop.DropEvent, DropEvent);
            AddHandler(KeyDownEvent, KeyboardEvent);
        }

        public void PointerEnterEvent(object? sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            bool mouseEntered = args.RoutedEvent.Name == "PointerEnter";
            button.Opacity = mouseEntered ? 1 : 0;
        }

        private void KeyboardEvent(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right) ViewModel.ShowNextImageCommand.Execute(null);
            else if (e.Key == Key.Left) ViewModel.ShowPrevImageCommand.Execute(null);
            else if (e.Key == Key.Up) ViewModel.ImageZoom++;
            else if (e.Key == Key.Down) ViewModel.ImageZoom--;
            else if (e.Key == Key.Escape) this.WindowState = this.WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
        }

        internal void DropEvent(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.FileNames))
            {
                ViewModel.SetImagePaths(e.Data.GetFileNames());
            }
        }
    }
}
