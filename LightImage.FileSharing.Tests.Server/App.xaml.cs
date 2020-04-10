using Autofac;
using LightImage.FileSharing.Tests.Shared;
using LightImage.Networking.Discovery;
using LightImage.Networking.Services;
using Microsoft.Extensions.Configuration;
using NetMQ;
using System.Windows;

namespace LightImage.FileSharing.Tests.ServerApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IContainer _container;
        private IDiscoveryNode _discovery;
        private ListPublisherService _publisher;
        private FileShareService _service;

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
            builder.RegisterModule(new FileShareModule(config));
            builder.RegisterModule(new NetworkingServicesModule(config));
            builder.RegisterType<ListPublisherService>().AsSelf().As<IService>().SingleInstance();
            builder.RegisterType<Server>().AsSelf().SingleInstance();
            _container = builder.Build();
            _discovery = _container.Resolve<IDiscoveryNode>();
            _discovery.Join(1);
            _service = _container.Resolve<FileShareService>();
            _publisher = _container.Resolve<ListPublisherService>();
            var server = _container.Resolve<Server>();
            var wnd = new MainWindow(server);
            wnd.Show();
        }
    }
}