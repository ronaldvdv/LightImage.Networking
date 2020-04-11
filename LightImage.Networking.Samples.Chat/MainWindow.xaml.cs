using System;
using System.Windows;

namespace LightImage.Networking.Samples.Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ChatService _service;

        public MainWindow(ChatService service) : base()
        {
            InitializeComponent();
            _service = service;

            service.MessageReceived += Service_MessageReceived;
        }

        private void Log(string message)
        {
            ConversationTextBox.Text += message + Environment.NewLine;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var message = MessageTextBox.Text;
            _service.Send(message);
            Log(message);
        }

        private void Service_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var message = e.Message;
            Log(message);
        }
    }
}