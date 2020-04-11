using Autofac;
using LightImage.Networking.Discovery;
using LightImage.Networking.Services;
using Microsoft.Extensions.Configuration;
using NetMQ;
using System.Windows;

namespace LightImage.Networking.Samples.Chat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IContainer _container;
        private IDiscoveryNode _discovery;

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            _container.Dispose();
            NetMQConfig.Cleanup();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var config = new ConfigurationBuilder().Build();
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DiscoveryModule(config));
            builder.RegisterModule(new NetworkingServicesModule(config));
            builder.RegisterType<ChatService>().AsSelf().As<IService>().SingleInstance();
            builder.RegisterType<ChatShim>().AsSelf().SingleInstance();
            builder.AddTestLogging();
            _container = builder.Build();
            _discovery = _container.Resolve<IDiscoveryNode>();
            _discovery.Join(1);
            var service = _container.Resolve<ChatService>();
            var wnd = new MainWindow(service);
            wnd.Show();
        }
    }
}