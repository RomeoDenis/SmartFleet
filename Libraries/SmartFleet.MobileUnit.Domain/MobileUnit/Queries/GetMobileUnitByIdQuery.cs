using System;
using MediatR;
using SmartFleet.MobileUnit.Domain.Dtos.MobileUnit;

namespace SmartFleet.MobileUnit.Domain.MobileUnit.Queries
{
    public class GetMobileUnitByIdQuery :IRequest<MobileUnitDto>
    {
        public Guid Id { get; set; }
    }
}
