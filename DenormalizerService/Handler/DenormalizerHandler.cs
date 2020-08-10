using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DenormalizerService.Infrastructure;
using MassTransit;
using SmartFleet.Core.Contracts.Commands;
using SmartFleet.Core.Data;
using SmartFleet.Core.Domain.Gpsdevices;
using SmartFleet.Core.Domain.Movement;
using SmartFleet.Core.Domain.Vehicles;
using SmartFleet.Core.ReverseGeoCoding;
using SmartFleet.Data;

namespace DenormalizerService.Handler
{
    public class DenormalizerHandler : 
        IConsumer<CreateTk103Gps>, 
        IConsumer<CreateNewBoxGps>, 
        IConsumer<TLGpsDataEvent>,
        IConsumer<CreateBoxCommand>,
        IConsumer<TlFuelEevents>,
        IConsumer<TLExcessSpeedEvent>,
        IConsumer<TLEcoDriverAlertEvent>,
        IConsumer<TLMilestoneVehicleEvent>
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        private readonly ReverseGeoCodingService _geoCodingService;
        private static SemaphoreSlim _semaphore;
        private SmartFleetObjectContext _db;

        public DenormalizerHandler()
        {
            new SemaphoreSlim(1, 1);
            _geoCodingService = new ReverseGeoCodingService();
            _dbContextScopeFactory = DependencyRegistrar.ResolveDbContextScopeFactory();
            _semaphore = new SemaphoreSlim(1);
        }

        public async Task Consume(ConsumeContext<CreateTk103Gps> context)
        {
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                Box box;
                using (DbContextTransaction scope = _db.Database.BeginTransaction())
                {
                     box = await _db.Boxes.FirstOrDefaultAsync(x => x.SerialNumber == context.Message.SerialNumber)
                        .ConfigureAwait(false);
                    scope.Commit();

                }

                if (box == null)
                {
                    box = new Box();
                    box.Id = Guid.NewGuid();
                    box.BoxStatus = BoxStatus.Prepared;
                    box.CreationDate = DateTime.UtcNow;
                    box.LastGpsInfoTime = context.Message.TimeStampUtc;

                    box.Icci = String.Empty;
                    box.PhoneNumber = String.Empty;
                    box.Vehicle = null;
                    box.Imei = context.Message.IMEI;
                    box.SerialNumber = context.Message.SerialNumber;
                    _db.Boxes.Add(box);
                }

                if (box.BoxStatus == BoxStatus.WaitInstallation)
                    box.BoxStatus = BoxStatus.Prepared;
                box.LastGpsInfoTime = context.Message.TimeStampUtc;
                var address = await _geoCodingService.ExecuteQueryAsync(context.Message.Latitude, context.Message.Longitude).ConfigureAwait(false);
                Position position = new Position
                {
                    Box_Id = box.Id,
                    Altitude = 0,
                    Direction = 0,

                    Lat = context.Message.Latitude,
                    Long = context.Message.Longitude,
                    Speed = context.Message.Speed,
                    Id = Guid.NewGuid(),
                    Priority = 0,
                    Satellite = 0,
                    Timestamp = context.Message.TimeStampUtc,
                    Address = address.display_name,
                    MotionStatus = (int)context.Message.Speed > 2 ? MotionStatus.Moving : MotionStatus.Stopped
                };
                _db.Positions.Add(position);
                await contextFScope.SaveChangesAsync().ConfigureAwait(false);

            }
        }

        public async Task Consume(ConsumeContext<CreateNewBoxGps> context)
        {
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                Box box;
                using (DbContextTransaction scope = _db.Database.BeginTransaction())
                {
                    box = await _db.Boxes.FirstOrDefaultAsync(x => x.SerialNumber == context.Message.SerialNumber)
                        .ConfigureAwait(false);
                    scope.Commit();

                }
                if (box == null)
                {
                    box = new Box();
                    box.Id = Guid.NewGuid();
                    box.BoxStatus = BoxStatus.Prepared;
                    box.CreationDate = DateTime.UtcNow;
                    box.LastGpsInfoTime = context.Message.TimeStampUtc;
                    box.Icci = String.Empty;
                    box.PhoneNumber = String.Empty;
                    box.Vehicle = null;
                    box.Imei = context.Message.IMEI;
                    box.SerialNumber = context.Message.SerialNumber;
                    _db.Boxes.Add(box);

                }
                if (box.BoxStatus == BoxStatus.WaitInstallation)
                    box.BoxStatus = BoxStatus.Prepared;
                box.LastGpsInfoTime = context.Message.TimeStampUtc;
                var address = await _geoCodingService.ExecuteQueryAsync(context.Message.Latitude, context.Message.Longitude).ConfigureAwait(false);
                Position position = new Position
                {
                    Box_Id = box.Id,
                    Altitude = 0,
                    Direction = 0,
                    Lat = context.Message.Latitude,
                    Long = context.Message.Longitude,
                    Speed = context.Message.Speed,
                    Id = Guid.NewGuid(),
                    Priority = 0,
                    Satellite = 0,
                    Timestamp = context.Message.TimeStampUtc,
                    Address = address.display_name,
                    MotionStatus = (int)context.Message.Speed > 2 ? MotionStatus.Moving : MotionStatus.Stopped
                };
                _db.Positions.Add(position);
                await contextFScope.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        private async Task<Box> GetModemDeviceAsync(TLGpsDataEvent context)
        {
           
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                var box = await _db.Boxes.SingleOrDefaultAsync(b => b.Imei == context.Imei).ConfigureAwait(false);
               return box;
            }

        }
        public async Task Consume(ConsumeContext<TLGpsDataEvent> context)
        {
           
            try
            {
                var box = await GetModemDeviceAsync(context.Message).ConfigureAwait(false);
                if (box != null)
                {
                    using (var contextFScope = _dbContextScopeFactory.Create())
                    {
                        _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();

                        var position = new Position();
                        position.Box_Id = box?.Id;
                        position.Altitude = context.Message.Altitude;
                        position.Direction = context.Message.Direction;
                        position.Lat = context.Message.Lat;
                        position.Long = context.Message.Long;
                        position.Speed = context.Message.Speed;
                        position.Address = context.Message.Address;
                        position.Id = Guid.NewGuid();
                        position.Priority = context.Message.Priority;
                        position.Satellite = context.Message.Satellite;
                        position.Timestamp = context.Message.DateTimeUtc;
                        position.MotionStatus =  context.Message.Speed > 0.0 ? MotionStatus.Moving : MotionStatus.Stopped;
                        box.LastGpsInfoTime = context.Message.DateTimeUtc;
                        _db.Positions.Add(position);
                       await contextFScope.SaveChangesAsync().ConfigureAwait(false);
                       _semaphore.Release();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                //_semaphore.Release();
                throw;
            }
            //_semaphore.Release();
        }

        public async Task Consume(ConsumeContext<CreateBoxCommand> context)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();

                var existingBox = await _db.Boxes.FirstOrDefaultAsync(b => b.Imei == context.Message.Imei).ConfigureAwait(false);
              if(existingBox!=null)
                  return;
                var box = new Box();
                box.Id = Guid.NewGuid();
                box.BoxStatus = BoxStatus.WaitPreparation;
                box.CreationDate = DateTime.UtcNow;
                box.Icci = String.Empty;
                box.PhoneNumber = String.Empty;
                box.Vehicle = null;
                box.Imei = context.Message.Imei;
                box.SerialNumber = String.Empty;
                try
                {
                    _db.Boxes.Add(box);
                    await contextFScope.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                    _semaphore.Release();
                    throw;
                }
            }

            _semaphore.Release();
        }

        public async Task Consume(ConsumeContext<TlFuelEevents> context)
        {
           
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                var fuelRecordMsg = context.Message.Events.OrderBy(x => x.DateTimeUtc).Last();

                var lastRecord = await _db.FuelConsumptions.OrderByDescending(x => x.DateTimeUtc)
                    // ReSharper disable once TooManyChainedReferences
                    .FirstOrDefaultAsync(x => x.VehicleId == fuelRecordMsg.VehicleId).ConfigureAwait(false);
                var fuelConsumption = SetFuelConsumptionObject(fuelRecordMsg);
                // ReSharper disable once ComplexConditionExpression
                if (lastRecord != null && fuelRecordMsg.MileStoneCalculated || lastRecord==null )
                {
                    if (lastRecord != null)
                        fuelConsumption.Milestone += lastRecord.Milestone;
                }
                else if (!fuelRecordMsg.MileStoneCalculated)
                    fuelConsumption.TotalFuelConsumed = fuelRecordMsg.FuelConsumption;

                _db.FuelConsumptions.Add(fuelConsumption);
                await contextFScope.SaveChangesAsync().ConfigureAwait(false);
                
            }
        }

        private static FuelConsumption SetFuelConsumptionObject(TLFuelMilstoneEvent context)
        {
            var entity = new FuelConsumption();

            entity.VehicleId = context.VehicleId;
            entity.FuelUsed = context.FuelConsumption;
            entity.FuelLevel = context.FuelLevel;
            entity.Milestone = context.Milestone;
            entity.CustomerId = context.CustomerId;
            entity.DateTimeUtc = context.DateTimeUtc;
            return entity;
        }

        public async Task Consume(ConsumeContext<TLExcessSpeedEvent> context)
        {
            
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                _db.VehicleAlerts.Add(new VehicleAlert
                {
                    Id = Guid.NewGuid(),
                    VehicleEvent =  context.Message.VehicleEventType,
                    Speed =Convert.ToInt32( context.Message.Speed),
                    EventUtc = context.Message.EventUtc,
                    CustomerId = (Guid) context.Message.CustomerId,
                    VehicleId = context.Message.VehicleId
                });
                await contextFScope.SaveChangesAsync().ConfigureAwait(false);

            }
           
        }

        public async Task Consume(ConsumeContext<TLEcoDriverAlertEvent> context)
        {
           
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                _db.VehicleAlerts.Add(new VehicleAlert
                {
                    Id = Guid.NewGuid(),
                    VehicleEvent = context.Message.VehicleEventType,
                    EventUtc = context.Message.EventUtc,
                    CustomerId = (Guid)context.Message.CustomerId,
                    VehicleId = context.Message.VehicleId
                });
                await contextFScope.SaveChangesAsync().ConfigureAwait(false);

            }
           
        }

        public async Task Consume(ConsumeContext<TLMilestoneVehicleEvent> context)
        {
          
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                var vehicle = await _db.Vehicles.FindAsync(context.Message.VehicleId).ConfigureAwait(false);
                if (vehicle != null)
                {
                    vehicle.Milestone += context.Message.Milestone;
                    vehicle.MileStoneUpdateUtc = context.Message.EventUtc;
                    await contextFScope.SaveChangesAsync().ConfigureAwait(false);

                }
            }
            
        }
    }
}
