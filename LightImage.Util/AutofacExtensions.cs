using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Autofac
{
    public static class AutofacExtensions
    {
        public static void AddTestLogging(this ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>));
            builder.RegisterType<NullLoggerFactory>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }

        public static void Configure<T>(this ContainerBuilder builder, IConfiguration config, string section)
                    where T : class, new()
        {
            var obj = new T();
            config.Bind(section, obj);
            builder.RegisterInstance(obj).AsSelf().AsImplementedInterfaces();
        }
    }
}