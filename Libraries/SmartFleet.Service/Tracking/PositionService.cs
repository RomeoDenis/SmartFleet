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
        /// <param name="geoCodingService"></param>
        /// <param name="dbContextScopeFactory"></param>
        public PositionService(  ReverseGeoCodingService geoCodingService, IDbContextScopeFactory dbContextScopeFactory)
        {
            _geoCodingService = geoCodingService;
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public async Task<List<Position>> GetLastVehiclePositionAsync(string userName)
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
                    .ToArrayAsync().ConfigureAwait(false);
                    
                if (!vehicles .Any())
                    return positions;

                foreach (var vehicle in vehicles)
                {
                    var boxes = await _objectContext
                        .Boxes
                        .Where(b => b.VehicleId == vehicle.Id 
                                    &&b.BoxStatus == BoxStatus.Valid)
                        .Select(x => x.Id)
                        .ToArrayAsync().ConfigureAwait(false);
                    if (!boxes.Any()) continue;
                    foreach (var geDevice in boxes)
                    {
                        var position = await _objectContext
                            .Positions
                            .OrderByDescending(x => x.Timestamp)
                            .FirstOrDefaultAsync(p => p.Box_Id == geDevice).ConfigureAwait(false);
                        if (position == null) continue;
                        position.Vehicle = vehicle;
                        await _geoCodingService.ReverseGeoCodingAsync(position).ConfigureAwait(false);
                        positions.Add(position);
                    }

                }
                return positions;
            }
        }

        public async Task<List<Position>> GetVehiclePositionsByPeriodAsync(Guid vehicleId, DateTime startPeriod,DateTime endPeriod)
        {
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                _objectContext = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                
                var box = await _objectContext
                    .Boxes
                    .Select(x=>new {  x.Id, x.VehicleId })
                    .FirstOrDefaultAsync(v => v.VehicleId == vehicleId)
                    .ConfigureAwait(false);
                
                if (box == null) return new List<Position>();
                
                if (box.Id == Guid.Empty) return new List<Position>();
                
                var positions =await 
                    _objectContext
                        .Positions
                        .Where(p => p.Box_Id == box.Id && p.Timestamp >= startPeriod && p.Timestamp <= endPeriod)
                        .ToListAsync()
                        .ConfigureAwait(false);
                return positions;
            }
        }
    }
}
