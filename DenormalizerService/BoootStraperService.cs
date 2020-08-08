using Autofac;
using DenormalizerService.Infrastructure;
using MassTransit;
using SmartFleet.Core;
using SmartFleet.Core.Infrastructure.MassTransit;

namespace DenormalizerService
{
    public class BoootStraperService :IMicorService
    {
       // private IBusControl _bus;

        

        public void StartService()
        {
            ContainerBuilder builder = new ContainerBuilder();
            var dependencyRegistrar = new DependencyRegistrar();
            dependencyRegistrar.Register(builder);
            DependencyRegistrar.ResolveServiceBus().Start();
            
        }
    }
}