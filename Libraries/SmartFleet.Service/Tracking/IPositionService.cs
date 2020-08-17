using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartFleet.Core.Domain.Movement;

namespace SmartFleet.Service.Tracking
{
    public interface IPositionService
    {
        Task<List<Position>> GetLastVehiclePositionAsync(string userName);

        Task<List<Position>> GetVehiclePositionsByPeriodAsync(Guid vehicleId, DateTime startPeriod,
            DateTime endPeriod);
    }
}