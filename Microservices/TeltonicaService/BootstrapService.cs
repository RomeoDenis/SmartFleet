using System;
using Autofac;
using MassTransit;
using SmartFleet.Core;
using SmartFleet.Core.Infrastructure.MassTransit;
using TeltonicaService.Handlers;
using TeltonicaService.Infrastructure;
using TeltonicaService.Infrastucture;

namespace TeltonicaService
{
    public class BootstrapService :IMicorService
    {
        public void StartConsumers(BusConsumerStarter busConsumer)
        {
            //registration of dependencies
            ContainerBuilder builder = new ContainerBuilder();
            var dependencyRegistrar = new DependencyRegistrar();
            dependencyRegistrar.Register(builder);

           
        }

        public void StartService()
        {
            ContainerBuilder builder = new ContainerBuilder();
            var dependencyRegistrar = new DependencyRegistrar();
            dependencyRegistrar.Register(builder);

            try
            {
                DependencyRegistrar.ResolveServiceBus().Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //  throw;
            }
        }
    }
}
