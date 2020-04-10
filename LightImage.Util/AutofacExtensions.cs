using Autofac;
using Microsoft.Extensions.Configuration;

namespace Autofac
{
    public static class AutofacExtensions
    {
        public static void Configure<T>(this ContainerBuilder builder, IConfiguration config, string section)
            where T : class, new()
        {
            var obj = new T();
            config.Bind(section, obj);
            builder.RegisterInstance(obj).AsSelf().AsImplementedInterfaces();
        }
    }
}