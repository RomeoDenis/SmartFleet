using System;
using SmartFleet.Core.Domain.Gpsdevices;

namespace SmartFleet.Core.Contracts.Commands
{
    [Serializable]
   public class CreateBoxCommand
    {
        public Guid Id { get; set; }
        public string Imei { get; set; }
        public DateTime? LastValidGpsDataUtc { get; set; }
        public string Address { get; set; }
        public BoxStatus BoxStatus { get; set; }
        public Guid? CustomerId { get; set; }
    }
}
