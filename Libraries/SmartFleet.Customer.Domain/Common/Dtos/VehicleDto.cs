using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SmartFleet.Customer.Domain.Common.Dtos
{
    public class VehicleDto
    {
        public VehicleDto(string vehicleName , Guid id, string customerId)
        {
            VehicleName = vehicleName;
            Id = id;
            CustomerId = customerId;
        }
        public Guid Id { get; set; }
        public string VehicleName { get; set; }
        public string LicensePlate { get; set; }
        public string Vin { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Customer { get; set; }
        public string Imei { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public string VehicleStatus { get; set; }
        public string VehicleType { get; set; }
        public string CreationDate { get; set; }
        public string InitServiceDate { get; set; }
        public string CustomerId { get; set; }
    }
}
