using System;
using GreenPipes;
using MassTransit;
using MassTransit.RabbitMqTransport;

namespace SmartFleet.Core.Infrastructure.MassTransit
{
    public class MassTransitConfig
    {
       static string url = "amqp://vnxetefp:a3QEf_C9qBdLbYphS65Iki6JkI6qxzZ7@woodpecker.rmq.cloudamqp.com/vnxetefp";

        /// <summary>
        /// Configures the receive bus.
        /// </summary>
        /// <param name="registration">The registration.</param>
        /// <returns></returns>
        public static IBusControl ConfigureReceiveBus(Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> registration)
        {
            return Bus.Factory.CreateUsingRabbitMq(configure =>
            {
                
                IRabbitMqHost host = configure.Host(
                    new Uri(url.Replace("amqp://", "rabbitmq://")),
                    hst =>
                    {
                        hst.Username("vnxetefp");
                        hst.Password("a3QEf_C9qBdLbYphS65Iki6JkI6qxzZ7");
                    });

                //configure.ReceiveEndpoint(host, null, e =>
                //{
                //    e.UseRetry(retryConfig =>
                //        retryConfig.Interval(10, TimeSpan.FromMilliseconds(200)));
                //    e.Durable = true;
                //    e.PrefetchCount = 4;
                //    e.UseRateLimit(20, TimeSpan.FromMinutes(1));

                //});
                registration?.Invoke(configure, host);
            });
        }

        /// <summary>
        /// Configures the sender bus.
        /// </summary>
        /// <returns></returns>
        public static IBusControl ConfigureSenderBus()
        {
            return Bus.Factory.CreateUsingRabbitMq(configure =>
            {

                IRabbitMqHost host = configure.Host(
                    new Uri(url.Replace("amqp://", "rabbitmq://")),
                    hst =>
                    {
                        hst.Username("vnxetefp");
                        hst.Password("a3QEf_C9qBdLbYphS65Iki6JkI6qxzZ7");
                    });


            });
        }

    }
}
