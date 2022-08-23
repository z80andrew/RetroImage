using Avalonia.Controls;
using Avalonia.Input;
using System.Diagnostics;
using System;
using System.Linq;
using Avalonia.ReactiveUI;
using RetroImage.ViewModels;
using Z80andrew.RetroImage.Services;
using System.IO;

namespace RetroImage.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();

            var logScrollViewer = this.FindControl<DockPanel>("MainDockPanel");

            AddHandler(DragDrop.DropEvent, Drop);
        }

        private void Drop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Drop");
            if (e.Data.Contains(DataFormats.FileNames))
            {
                ViewModel.CurrentImageName = Path.GetFileName(e.Data.GetFileNames().First());
                ViewModel.CurrentImage = DegasService.ReadDegasImage(e.Data.GetFileNames().First());
            }
        }
    }
}
