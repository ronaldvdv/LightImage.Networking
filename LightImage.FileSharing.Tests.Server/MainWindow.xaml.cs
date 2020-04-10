using LightImage.FileSharing.Tests.Shared;
using System.Windows;

namespace LightImage.FileSharing.Tests.ServerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Server _server;

        public MainWindow(Server server)
        {
            InitializeComponent();
            DataContext = server;
            _server = server;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                    _server.Add(file);
            }
        }
    }
}