using Autofac;
using DenormalizerService.Handler;
using MassTransit;
using SmartFleet.Core.Data;
using SmartFleet.Core.Infrastructure.MassTransit;
using SmartFleet.Core.Infrastructure.Registration;
using SmartFleet.Data.Dbcontextccope.Implementations;

namespace DenormalizerService.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        private static IContainer Container { get; set; }

        public void Register(ContainerBuilder builder)
        {

            builder.Register(context => RabbitMqConfig.InitReceiverBus<DenormalizerHandler>("Denormalizer.endpoint"))
                .SingleInstance()
                .As<IBusControl>()
                .As<IBus>();
            builder.RegisterType<DbContextScopeFactory>().As<IDbContextScopeFactory>();

            Container = builder.Build();
        }

        public static IBusControl ResolveServiceBus()
        {
            return Container.Resolve<IBusControl>();
        }
       
        public static IDbContextScopeFactory ResolveDbContextScopeFactory()
        {
            return Container.Resolve<IDbContextScopeFactory>();
        }
    }
    
}
