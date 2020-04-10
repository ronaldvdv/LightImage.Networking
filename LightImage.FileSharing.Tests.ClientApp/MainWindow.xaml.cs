using LightImage.FileSharing.Tests.Shared;
using System.Windows;

namespace LightImage.FileSharing.Tests.ClientApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Client _client;

        public MainWindow(Client client)
        {
            InitializeComponent();
            DataContext = _client = client;
        }
    }
}