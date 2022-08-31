using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using RetroImage.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        }

        internal void Drop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("Drop");
            if (e.Data.Contains(DataFormats.FileNames))
            {
                ViewModel.InitImage(e.Data.GetFileNames().First());
            }
        }
    }
}
