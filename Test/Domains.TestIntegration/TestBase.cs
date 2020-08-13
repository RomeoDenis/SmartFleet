using Autofac;
using AutoMapper;
using MediatR;
using SmartFleet.Core.Data;
using SmartFleet.Customer.Domain;
using SmartFleet.Customer.Domain.Common.DomainMapping;
using SmartFleet.Data.Dbcontextccope.Implementations;
using SmartFleet.MobileUnit.Domain;
using SmartFleet.MobileUnit.Domain.Common;

namespace Domains.TestIntegration
{
    public class TestBase
    {
        private static IContainer Container;
       public static void RegisterDependencies()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<DbContextScopeFactory>().As<IDbContextScopeFactory>();
            builder.RegisterType<AmbientDbContextLocator>().As<IAmbientDbContextLocator>();

            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                // cfg.AddProfile(new SmartFleetAdminMappings());
                cfg.AddProfile(new CustomerDomainMapping());
                cfg.AddProfile(new MobileUnitDomainMapping());
            });
            var mapper = mapperConfiguration.CreateMapper();
            builder.RegisterInstance(mapper).As<IMapper>();
            builder
                .RegisterType<Mediator>()
                .As<IMediator>()
                .InstancePerLifetimeScope();
            builder.Register<ServiceFactory>(context =>
            {
                var c = context.Resolve<IComponentContext>();
                return t => c.Resolve(t);
            });
            RegisterDomains(builder);
            Container = builder.Build();
        }

        private static void RegisterDomains(ContainerBuilder builder)
        {
            var customerDomain = new CustomerDomainDependencyRegistrar();
            var mobileUnitDependencyRegistrar = new MobileUnitDependencyRegistrar();
            customerDomain.Register(builder);
            mobileUnitDependencyRegistrar.Register(builder);
        }
    }
}
