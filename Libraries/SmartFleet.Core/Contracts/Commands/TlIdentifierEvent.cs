using System;

namespace SmartFleet.Core.Contracts.Commands
{
    public class TlIdentifierEvent
    {
        public string IdentifierNumber { get; set; }
        public Guid CustomerId { get; set; }
    }
}