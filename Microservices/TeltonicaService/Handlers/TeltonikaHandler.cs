using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MassTransit;
using SmartFleet.Core.Contracts.Commands;
using SmartFleet.Core.Data;
using SmartFleet.Core.Domain.Gpsdevices;
using SmartFleet.Core.Domain.Movement;
using SmartFleet.Core.Domain.Vehicles;
using SmartFleet.Core.Geofence;
using SmartFleet.Core.ReverseGeoCoding;
using SmartFleet.Data;
using TeltonicaService.Infrastructure;
using TeltonicaService.Infrastucture;

namespace TeltonicaService.Handlers
{

    public class TeltonikaHandler : IConsumer<TLGpsDataEvents>
    {
        private SmartFleetObjectContext _db;
        private IMapper _mappe;
        private readonly ReverseGeoCodingService _reverseGeoCodingService;
        public IDbContextScopeFactory DbContextScopeFactory { get; }
        private static SemaphoreSlim _semaphore;

        public TeltonikaHandler()
        {
            _semaphore = new SemaphoreSlim(1, 4);
            DbContextScopeFactory = DependencyRegistrar.ResolveDbContextScopeFactory();
            _reverseGeoCodingService = DependencyRegistrar.ResolveGeoCodeService();
            InitMapper();
        }

        private void InitMapper()
        {
            var mapperConfiguration = new MapperConfiguration(cfg => { cfg.AddProfile(new TeltonikaMapping()); });
            _mappe = mapperConfiguration.CreateMapper();
        }

        private async Task<Box> Getbox(CreateTeltonikaGps context)
        {
            using (var contextFScope = DbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                return await _db.Boxes.Include(x => x.Vehicle).SingleOrDefaultAsync(b => b.Imei == context.Imei);
            }
        }

        private async Task<PositionQuery> GetLastPostion(Guid boxId)
        {
            using (var contextFScope = DbContextScopeFactory.Create())
            {
                _db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                return await _db.Positions.OrderByDescending(x => x.Timestamp).Where(x => x.Box_Id == boxId).Select(p=>new PositionQuery(){VehicleId= p.Box.VehicleId, Lat = p.Lat, Long= p.Long}).FirstOrDefaultAsync();
            }
        }

        public async Task Consume(ConsumeContext<TLGpsDataEvents> context)
        {

            try
            {
                await _semaphore.WaitAsync();
                var box = await Getbox(context.Message.Events.LastOrDefault());
                List<TLEcoDriverAlertEvent> ecoDriveEvents = new List<TLEcoDriverAlertEvent>();
                List<TLGpsDataEvent> gpsDataEvents = new List<TLGpsDataEvent>();
                List<TLFuelMilstoneEvent> tlFuelMilstoneEvents = new List<TLFuelMilstoneEvent>();
                List<TLExcessSpeedEvent> speedEvents = new List<TLExcessSpeedEvent>();
                foreach (var teltonikaGps in context.Message.Events)
                {
                    if (box == null) continue;
                    // envoi des données GPs
                    var gpsDataEvent = _mappe.Map<TLGpsDataEvent>(teltonikaGps);
                    gpsDataEvent.BoxId = box.Id;
                    Trace.WriteLine(gpsDataEvent.DateTimeUtc + " lat:" + gpsDataEvent.Lat + " long:" + gpsDataEvent.Long);
                    // await context.Publish(gpsDataEvent);
                    gpsDataEvents.Add(gpsDataEvent);

                    if (box.Vehicle == null) continue;
                    InitAllIoElements(teltonikaGps);
                    var canInfo = ProceedTNCANFilters(teltonikaGps);
                    if (canInfo != default(TLFuelMilstoneEvent))
                    {
                        canInfo.VehicleId = box.Vehicle.Id;
                        if (box.Vehicle.CustomerId.HasValue)
                            canInfo.CustomerId = box.Vehicle.CustomerId.Value;
                        // calcul de la distance par rapport au dernier point GPS
                        if (canInfo.Milestone <= 0)
                        {
                            var positionQuery = await GetLastPostion(box.Id);
                            if (positionQuery != null)
                            {
                                var distance = GetGpsDistance(gpsDataEvents);
                                Trace.TraceInformation($"time :{gpsDataEvent.DateTimeUtc} Ditance: " + distance);
                                canInfo.Milestone =distance;
                                canInfo.MileStoneCalculated =true;
                                if (distance > 0 && box.VehicleId!=null)
                                {
                                    await context.Publish(new TLMilestoneVehicleEvent
                                    {
                                        Milestone = distance,
                                        VehicleId = box.VehicleId.Value,
                                        EventUtc = teltonikaGps.Timestamp
                                    });
                                }
                            } 
                        }
                        // await context.Publish(canInfo);
                        tlFuelMilstoneEvents.Add(canInfo);
                    }

                    // ReSharper disable once ComplexConditionExpression
                    if (box.Vehicle.SpeedAlertEnabled && box.Vehicle.MaxSpeed <= teltonikaGps.Speed
                        || teltonikaGps.Speed > 85)
                    {
                        var alertExeedSpeed = ProceedTLSpeedingAlert(teltonikaGps, box.Vehicle.Id,
                            box.Vehicle.CustomerId);
                        // await context.Publish(alertExeedSpeed);
                        speedEvents.Add(alertExeedSpeed);
                    }

                    var ecoDriveEvent = ProceedEcoDriverEvents(teltonikaGps, box.Vehicle.Id,
                        box.Vehicle.CustomerId);
                    if (ecoDriveEvent != default(TLEcoDriverAlertEvent))
                        //ecoDriveEvents.Add(ecoDriveEvent);
                        await context.Publish(ecoDriveEvent);
                }

                if (speedEvents.Any())
                   await context.Publish(speedEvents.OrderBy(x => x.EventUtc).LastOrDefault());
                if (gpsDataEvents.Any())
                {
                    await GeoReverseCodeGpsData(gpsDataEvents);
                    foreach (var finalgpsDataEvent in gpsDataEvents)
                        await context.Publish(finalgpsDataEvent);
                }

                if (tlFuelMilstoneEvents.Any())
                {
                    var events = new TlFuelEevents
                    {
                        Id = Guid.NewGuid(),
                        Events = tlFuelMilstoneEvents,
                        //TlGpsDataEvents = gpsDataEvents
                    };
                    await context.Publish(events);

                }
                _semaphore.Release();

            }
            catch (Exception e)
            {
                Trace.TraceWarning(e.Message + " details:" + e.StackTrace);
                //throw;
            }

        }

        private static double GetGpsDistance(List<TLGpsDataEvent> gpsDataEvents)
        {
            var distance = 0.0;
            var firstPos = gpsDataEvents.First();
            foreach (var p in gpsDataEvents.Skip(1))
            {
                distance += Math.Round(GeofenceHelper.CalculateDistance(firstPos.Lat, firstPos.Long, p.Lat, p.Long), 2);
                firstPos = p;
            }

            return distance;
        }

        private async Task GeoReverseCodeGpsData(List<TLGpsDataEvent> gpsRessult)
        {
            foreach (var gpSdata in gpsRessult)
                gpSdata.Address = await _reverseGeoCodingService.ReverseGoecode(gpSdata.Lat, gpSdata.Long);

        }
        double CalculateDistance(double lat1, double log,double lat2 ,double  log2) 
        {
            var p1 = new GeofenceHelper.Position();
            p1.Latitude = lat1;
            p1.Longitude =log;
            var p2 = new GeofenceHelper.Position();
            p2.Latitude = lat2;
            p2.Longitude = log2;

           return Math.Round(GeofenceHelper.HaversineFormula(p1, p2, GeofenceHelper.DistanceType.Kilometers), 2);

        }

        private static void InitAllIoElements(CreateTeltonikaGps context)
        {
            context.AllIoElements = new Dictionary<TNIoProperty, long>();
            foreach (var ioElement in context.IoElements_1B)
                context.AllIoElements.Add((TNIoProperty)ioElement.Key, ioElement.Value);
            foreach (var ioElement in context.IoElements_2B)
                context.AllIoElements.Add((TNIoProperty)ioElement.Key, ioElement.Value);
            foreach (var ioElement in context.IoElements_4B)
                context.AllIoElements.Add((TNIoProperty)ioElement.Key, ioElement.Value);
            foreach (var ioElement in context.IoElements_8B)
                context.AllIoElements.Add((TNIoProperty)ioElement.Key, ioElement.Value);
        }

        private TLFuelMilstoneEvent ProceedTNCANFilters(CreateTeltonikaGps data)
        {
            var fuelLevel = default(UInt32?);
            var milestone = default(UInt32?);
            var fuelUsed = default(UInt32?);
            if (data.AllIoElements != null &&
                data.AllIoElements.ContainsKey(TNIoProperty.High_resolution_total_vehicle_distance_X))
                milestone = Convert.ToUInt32(data.AllIoElements[TNIoProperty.High_resolution_total_vehicle_distance_X]);

            if (data.AllIoElements != null && data.AllIoElements.ContainsKey(TNIoProperty.Engine_total_fuel_used))
                fuelUsed = Convert.ToUInt32(data.AllIoElements[TNIoProperty.Engine_total_fuel_used]);

            if (data.AllIoElements != null && data.AllIoElements.ContainsKey(TNIoProperty.Fuel_level_1_X))
                fuelLevel = Convert.ToUInt32(data.AllIoElements[TNIoProperty.Fuel_level_1_X]);
            // ReSharper disable once ComplexConditionExpression
            if (fuelLevel != default(UInt32) && fuelLevel>0 && fuelUsed>0)
                return new TLFuelMilstoneEvent
                {
                    FuelConsumption = Convert.ToInt32(fuelUsed),
                    Milestone = Convert.ToInt32(milestone),
                    DateTimeUtc = data.Timestamp,
                    FuelLevel = Convert.ToInt32(fuelLevel)
                };
            return default(TLFuelMilstoneEvent);
        }

        private TLExcessSpeedEvent ProceedTLSpeedingAlert(CreateTeltonikaGps data, Guid vehicleId, Guid? customerId)
        {
            return new TLExcessSpeedEvent
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                VehicleId = vehicleId,
                VehicleEventType = VehicleEvent.EXCESS_SPEED,
                EventUtc = data.Timestamp,
                Latitude = (float?)data.Lat,
                Longitude = (float?)data.Long,
                Address = data.Address,
                Speed = data.Speed
            };


        }


        private TLEcoDriverAlertEvent ProceedEcoDriverEvents(CreateTeltonikaGps data, Guid vehicleId, Guid? customerId)
        {
            var @event = default(TLEcoDriverAlertEvent);
            if (data.DataEventIO == (int)TNIoProperty.Engine_speed_X)
                @event = new TLEcoDriverAlertEvent
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    VehicleId = vehicleId,
                    VehicleEventType = VehicleEvent.EXCESS_ENGINE_SPEED,
                    EventUtc = data.Timestamp,
                    Latitude = (float?)data.Lat,
                    Longitude = (float?)data.Long,
                    Address = data.Address
                    //Speed = data.Speed
                };
            else  if(data.AllIoElements.ContainsKey(TNIoProperty.ECO_driving_type))
            {
                switch (Convert.ToByte(data.AllIoElements[TNIoProperty.ECO_driving_type]))
                {
                    case 1:
                        if (Convert.ToByte(data.AllIoElements[TNIoProperty.ECO_driving_value]) > 31)
                            @event = new TLEcoDriverAlertEvent
                            {
                                Id = Guid.NewGuid(),
                                CustomerId = customerId,
                                VehicleId = vehicleId,
                                VehicleEventType = VehicleEvent.EXCESS_ACCELERATION,
                                EventUtc = data.Timestamp,
                                Latitude = (float?)data.Lat,
                                Longitude = (float?)data.Long,
                                Address = data.Address
                                //Speed = data.Speed
                            };
                        break;
                    case 2:
                        if (Convert.ToByte(data.AllIoElements[TNIoProperty.ECO_driving_value]) > 38)
                            @event = new TLEcoDriverAlertEvent
                            {
                                Id = Guid.NewGuid(),
                                CustomerId = customerId,
                                VehicleId = vehicleId,
                                VehicleEventType = VehicleEvent.SUDDEN_BRAKING,
                                EventUtc = data.Timestamp,
                                Latitude = (float?)data.Lat,
                                Longitude = (float?)data.Long,
                                Address = data.Address
                                //Speed = data.Speed
                            };
                        break;
                    case 3:
                        if (Convert.ToByte(data.AllIoElements[TNIoProperty.ECO_driving_value]) > 45)
                            @event = new TLEcoDriverAlertEvent
                            {
                                Id = Guid.NewGuid(),
                                CustomerId = customerId,
                                VehicleId = vehicleId,
                                VehicleEventType = VehicleEvent.FAST_CORNER,
                                EventUtc = data.Timestamp,
                                Latitude = (float?)data.Lat,
                                Longitude = (float?)data.Long,
                                Address = data.Address
                                //Speed = data.Speed
                            };
                        break;
                    default:
                        break;

                }
            }
            return @event;
        }


       
    }

    internal class PositionQuery
    {
        public Guid? VehicleId { get; set; }
        public double Lat { get; internal set; }
        public double Long { get; internal set; }
    }
}
