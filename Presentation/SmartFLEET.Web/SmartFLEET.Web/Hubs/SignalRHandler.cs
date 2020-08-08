using System.Data.Entity;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNet.SignalR;
using SmartFleet.Core.Contracts.Commands;
using SmartFleet.Core.Data;
using SmartFleet.Core.Domain.Gpsdevices;
using SmartFleet.Core.Domain.Movement;
using SmartFleet.Core.Geofence;
using SmartFleet.Core.ReverseGeoCoding;
using SmartFleet.Data;
using SmartFleet.Service.Models;
using SmartFLEET.Web.Models.Eveents;

namespace SmartFLEET.Web.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public class SignalRHandler : Hub,
        IConsumer<CreateTk103Gps>,
        IConsumer<CreateNewBoxGps>, 
        IConsumer<TLGpsDataEvents>,
        IConsumer<TLExcessSpeedEvent>,
        IConsumer<TLEcoDriverAlertEvent>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<CreateTk103Gps> context)
        {
            if (SignalRHubManager.Clients == null)
                return;
            var reverseGeoCodingService = new ReverseGeoCodingService();
            await reverseGeoCodingService.ReverseGeoCoding(context.Message).ConfigureAwait(false);
            using (var dbContextScopeFactory = SignalRHubManager.DbContextScopeFactory.Create())
            {
                // get current gps device 
                var box = await GetSenderBox(context.Message, dbContextScopeFactory).ConfigureAwait(false);
                if (box != null)
                {
                    // set position 
                    var position = new PositionViewModel(context.Message, box.Vehicle);
                    await SignalRHubManager.Clients.Group(position.CustomerName).receiveGpsStatements(position);
                }
            }

        }

        private static async Task<Box> GetSenderBox(CreateTk103Gps message, IDbContextScope dbContextScopeFactory)
        {
            var dbContext = dbContextScopeFactory.DbContexts.Get<SmartFleetObjectContext>();
            var box = await dbContext.Boxes.Include(x => x.Vehicle).Include(x => x.Vehicle.Customer).FirstOrDefaultAsync(b =>
                b.SerialNumber == message.SerialNumber).ConfigureAwait(false);
            return box;
        }

        
        private static async Task<Box> GetSenderBoxAsync(string imei, IDbContextScope dbContextScopeFactory)
        {
            var dbContext = dbContextScopeFactory.DbContexts.Get<SmartFleetObjectContext>();
            var box = await dbContext.Boxes.Include(x => x.Vehicle).Include(x => x.Vehicle.Customer).FirstOrDefaultAsync(b =>
                b.Imei == imei).ConfigureAwait(false);
            return box;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupName"></param>
        public void Join(string groupName)
        {
            Groups.Add(Context.ConnectionId, groupName);
            if (!SignalRHubManager.Connections.ContainsKey(Context.User.Identity.Name))
                SignalRHubManager.Connections.Add(Context.User.Identity.Name, Context.ConnectionId);
            else SignalRHubManager.Connections[Context.User.Identity.Name] = Context.ConnectionId;
            SignalRHubManager.Clients = Clients;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        public override Task OnDisconnected(bool stopCalled)
        {
            SignalRHubManager.Clients = Clients;
            SignalRHubManager.Connections.Remove(Context.User.Identity.Name);
            //SignalRHubManager.Connections.Clear();
            return base.OnDisconnected(stopCalled);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<CreateNewBoxGps> context)
        {
            if (SignalRHubManager.Clients == null)
                return;
            using (var dbContextScopeFactory = SignalRHubManager.DbContextScopeFactory.Create())
            {
                // get current gps device 
                var box = await GetSenderBoxAsync(context.Message.IMEI, dbContextScopeFactory).ConfigureAwait(false);
                if (box != null)
                {
                    // set position 
                    var position = new PositionViewModel(context.Message, box.Vehicle);
                    await SignalRHubManager.Clients.Group(position.CustomerName).receiveGpsStatements(position);
                }
            }
        }

        public async Task Consume(ConsumeContext<TLGpsDataEvents> context)
        {
            if (SignalRHubManager.Clients == null)
                return;
         
            using (var dbContextScopeFactory = SignalRHubManager.DbContextScopeFactory.Create())
            {
                foreach (var @event in context.Message.Events)
                {
                    // get current gps device 
                    var box = await GetSenderBoxAsync(@event.Imei, dbContextScopeFactory).ConfigureAwait(false);
                    if (box != null)
                    {
                        // set position 
                        if (!SignalRHubManager.LastPosition.ContainsKey(box.Imei))
                            SignalRHubManager.LastPosition.Add(box.Imei, new GeofenceHelper.Position
                            {
                                Latitude = @event.Lat,
                                Longitude = @event.Long
                            });
                        var position = new PositionViewModel(@event, box.Vehicle, SignalRHubManager.LastPosition[box.Imei]);
                        await SignalRHubManager.Clients.Group(position.CustomerName).receiveGpsStatements(position);
                        SignalRHubManager.LastPosition[box.Imei] = new GeofenceHelper.Position
                        {
                            Latitude = @event.Lat,
                            Longitude = @event.Long
                        };

                    }
                }
            }
        }

        public async Task Consume(ConsumeContext<TLExcessSpeedEvent> context)
        {
            if (context.Message.CustomerId != null)
            {
                var evt = SignalRHubManager.Mapper.Map<TLVehicleEventVM>(context.Message);
                evt.SetEventMessage(context.Message.VehicleEventType);
                await SignalRHubManager.Clients.Group(context.Message.CustomerId.ToString()).receiveVehicleEvent(evt);

            }


        }

        public Task Consume(ConsumeContext<TLEcoDriverAlertEvent> context)
        {
            throw new System.NotImplementedException();
        }
    }
}