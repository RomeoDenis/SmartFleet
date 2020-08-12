using MediatR;
using SmartFleet.MobileUnit.Domain.Dtos.MobileUnit;

namespace SmartFleet.MobileUnit.Domain.MobileUnit.Queries
{
    public class GetMobileUnitByImeiQuery :IRequest<MobileUnitDto>
    {
        public string Imei { get; set; }
    }
}
