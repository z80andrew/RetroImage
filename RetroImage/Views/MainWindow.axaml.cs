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

                switch(Path.GetExtension(e.Data.GetFileNames().First()).ToUpper())
                {
                    case ".NEO":
                        ViewModel.CurrentImage = NEOchromeService.ReadNEOImage(e.Data.GetFileNames().First());
                        break;
                    case ".PI1":
                    case ".PI2":
                    case ".PI3":
                    case ".PC1":
                    case ".PC2":
                    case ".PC3":
                        ViewModel.CurrentImage = DegasService.ReadDegasImage(e.Data.GetFileNames().First());
                        break;
                }
            }
        }
    }
}
