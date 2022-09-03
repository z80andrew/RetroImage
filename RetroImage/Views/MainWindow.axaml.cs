using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using RetroImage.ViewModels;
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

            var logScrollViewer = this.FindControl<DockPanel>("MainDockPanel");

            AddHandler(DragDrop.DropEvent, Drop);
            AddHandler(KeyDownEvent, KeyboardEvent);
        }

        private void KeyboardEvent(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right) ViewModel.ShowNextImageCommand.Execute(null);
            else if (e.Key == Key.Left) ViewModel.ShowPrevImageCommand.Execute(null);
        }

        internal void Drop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Drop");
            if (e.Data.Contains(DataFormats.FileNames))
            {
                ViewModel.SetImagePaths(e.Data.GetFileNames());
            }
        }
    }
}
