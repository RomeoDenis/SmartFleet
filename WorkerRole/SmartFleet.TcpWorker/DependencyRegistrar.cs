using System.Configuration;
using Autofac;
using MassTransit;
using SmartFleet.Core.Infrastructure.MassTransit;
using SmartFleet.Core.ReverseGeoCoding;
using SmartFleet.Data;
using TeltonikaListner;

namespace SmartFleet.TcpWorker
{
    public static class DependencyRegistrar
    {
        static IContainer Container { get; set; }
        static IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ReverseGeoCodingService>();
            var bus = RabbitMqConfig.ConfigureSenderBus();
            builder.RegisterInstance(bus).As<IBusControl>();
            builder.RegisterType<TeltonikaTcpServer>();
            builder.Register(c => new RedisConnectionManager(ConfigurationManager.AppSettings["RedisUrl"],  ConfigurationManager.AppSettings["redisPass"])).As<IRedisConnectionManager>();
            builder.RegisterType<RedisCache>().As<IRedisCache>();
            return builder.Build();
        }

        public static void ResolveDependencies()
        {
            Container = BuildContainer();
            Container.Resolve<ReverseGeoCodingService>();
            Container.Resolve<IBusControl>();
            Container.Resolve<IRedisCache>();
            var listener = Container.Resolve<TeltonikaTcpServer>();
            listener.Start();

        }

    }
}
