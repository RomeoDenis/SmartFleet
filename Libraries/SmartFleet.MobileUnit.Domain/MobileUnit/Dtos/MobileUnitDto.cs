using System;
using SmartFleet.Core.Domain.Gpsdevices;

namespace SmartFleet.MobileUnit.Domain.Dtos.MobileUnit
{
    public class MobileUnitDto
    {
        public string SerialNumber { get; set; }

        public string Imei { get; set; }
        public Guid? VehicleId { get; set; }
        public BoxStatus BoxStatus { get; set; }
        public DateTime? CreationDate { get; set; }
        public string IccId { get; set; }

        public string PhoneNumber { get; set; }
        public DateTime? LastGpsInfoTime { get; set; }
        public DeviceType Type { get; set; }
        public string Brand { get; set; }
    }
}
