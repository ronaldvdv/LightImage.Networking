using Autofac;
using LightImage.Networking.Services;
using NetMQ;
using System.Windows;
using LightImage.Networking.Discovery;
using Microsoft.Extensions.Configuration;
using LightImage.Networking.FileSharing.Tests.Shared;

namespace LightImage.Networking.FileSharing.Tests.ClientApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IContainer _container;
        private IDiscoveryNode _discovery;
        private FileShareService _service;
        private ListSubscriberService _subscriber;

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
            builder.AddTestLogging();
            builder.RegisterModule(new DiscoveryModule(config));
            builder.RegisterModule(new NetworkingServicesModule(config));
            builder.RegisterModule(new FileShareModule(config));
            builder.RegisterType<ListSubscriberService>().AsSelf().As<IService>().SingleInstance();
            builder.RegisterType<Client>().AsSelf().SingleInstance();
            _container = builder.Build();
            _discovery = _container.Resolve<IDiscoveryNode>();
            _discovery.Join(1);
            _service = _container.Resolve<FileShareService>();
            _subscriber = _container.Resolve<ListSubscriberService>();
            var client = _container.Resolve<Client>();
            var wnd = new MainWindow(client);
            wnd.Show();
        }
    }
}