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
        DegasService degasService;
        public MainWindow()
        {
            InitializeComponent();

            degasService = new DegasService();

            var logScrollViewer = this.FindControl<DockPanel>("MainDockPanel");

            AddHandler(DragDrop.DropEvent, Drop);
        }

        private void Drop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Drop");
            if (e.Data.Contains(DataFormats.FileNames))
            {
                switch(Path.GetExtension(e.Data.GetFileNames().First()).ToUpper())
                {
                    case ".NEO":
                        ViewModel.InitImage(new NEOchromeService(), e.Data.GetFileNames().First());
                        break;
                    case ".PI1":
                    case ".PI2":
                    case ".PI3":
                    case ".PC1":
                    case ".PC2":
                    case ".PC3":
                    case ".PIC":
                        ViewModel.InitImage(new DegasService(), e.Data.GetFileNames().First());
                        break;
                    case ".DOO":
                        ViewModel.InitImage(new DoodleService(), e.Data.GetFileNames().First());
                        break;
                    case ".IFF":
                        ViewModel.InitImage(new IFFService(), e.Data.GetFileNames().First());
                        break;
                    case ".TNY":
                    case ".TN1":
                    case ".TN2":
                    case ".TN3":
                        ViewModel.InitImage(new TinyService(), e.Data.GetFileNames().First());
                        break;
                }
            }
        }
    }
}
