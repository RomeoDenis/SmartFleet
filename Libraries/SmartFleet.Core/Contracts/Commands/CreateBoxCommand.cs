using System;

namespace SmartFleet.Core.Contracts.Commands
{
   public class CreateBoxCommand
    {
        public Guid Id { get; set; }
        public string Imei { get; set; }
        public DateTime? LastValidGpsDataUtc { get; set; }
        public string Address { get; set; }
    }
}
