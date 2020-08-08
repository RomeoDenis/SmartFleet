using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using SmartFleet.Core.Data;
using SmartFleet.Core.Domain.Gpsdevices;
using SmartFleet.Core.Domain.Movement;
using SmartFleet.Core.Domain.Vehicles;
using SmartFleet.Core.ReverseGeoCoding;
using SmartFleet.Data;

namespace SmartFleet.Service.Tracking
{
    public class PositionService : IPositionService
    {
        private  SmartFleetObjectContext _objectContext;
        private readonly ReverseGeoCodingService _geoCodingService;
        private readonly IDbContextScopeFactory _dbContextScopeFactory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="positionRepository"></param>
        /// <param name="geoCodingService"></param>
        /// <param name="objectContext"></param>
        /// <param name="gpsDeviceRepository"></param>
        /// <param name="vehicleRepository"></param>
        /// <param name="dbContextScopeFactory"></param>
        public PositionService(  ReverseGeoCodingService geoCodingService, IDbContextScopeFactory dbContextScopeFactory)
        {
            _geoCodingService = geoCodingService;
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public async Task<List<Position>> GetLastVehiclPosition(string userName)
        {
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _objectContext = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                var positions = new List<Position>();
                // ReSharper disable once ComplexConditionExpression
                var vehicles = await _objectContext.UserAccounts
                    .Include(x => x.Customer)
                    .Include(x => x.Customer.Vehicles)
                    .Where(x => x.UserName == userName)
                    .SelectMany(x => x.Customer.Vehicles.Where(v=>v.VehicleStatus == VehicleStatus.Active).Select(v => v))
                    .ToArrayAsync();
                    
                if (!vehicles .Any())
                    return positions;

                foreach (var vehicle in vehicles)
                {
                    var boxes = await _objectContext
                        .Boxes
                        .Where(b => b.VehicleId == vehicle.Id 
                                    &&b.BoxStatus == BoxStatus.Valid)
                        .Select(x => x.Id)
                        .ToArrayAsync();
                    if (!boxes.Any()) continue;
                    foreach (var geDevice in boxes)
                    {
                        var position = await _objectContext
                            .Positions
                            .OrderByDescending(x => x.Timestamp)
                            .FirstOrDefaultAsync(p => p.Box_Id == geDevice);
                        if (position == null) continue;
                        position.Vehicle = vehicle;
                        await _geoCodingService.ReverseGeoCodingAsync(position);
                        positions.Add(position);
                    }

                }
                return positions;
            }
        }

        public async Task<List<Position>> GetVehiclePositionsByPeriod(Guid vehivleId, DateTime startPeriod,DateTime endPeriod)
        {
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _objectContext = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                var vehicle = await _objectContext.Vehicles.Include(v => v.Boxes).FirstOrDefaultAsync(v => v.Id == vehivleId).ConfigureAwait(false);
                if (vehicle == null) return new List<Position>();
                var box = vehicle.Boxes.FirstOrDefault();
                if (box == null) return new List<Position>();
                var positions = 
                    _objectContext
                    .Positions
                    .Where(p => p.Box_Id == box.Id && p.Timestamp >= startPeriod && p.Timestamp <= endPeriod)
                    .ToList();
                return positions;
            }
        }
    }
}
