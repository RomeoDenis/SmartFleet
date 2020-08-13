using System;
using MediatR;
using SmartFleet.Customer.Domain.Common.Dtos;

namespace SmartFleet.Customer.Domain.Queries.Vehicles
{
    public class GetVehicleByMobileUnitIdQuery : IRequest<VehicleDto>
    {
        public Guid MobileUnitId { get; set; }
    }
}
