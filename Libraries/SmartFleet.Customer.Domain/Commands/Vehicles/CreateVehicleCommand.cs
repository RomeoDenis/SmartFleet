using System;
using FluentValidation;
using MediatR;
using SmartFleet.Core.Contracts.Commands;
using SmartFleet.Core.Domain.Vehicles;

namespace SmartFleet.Customer.Domain.Commands.Vehicles
{
    public class CreateVehicleCommand : SmartFleetCommand, IRequest<Unit>
    {
        public string VehicleName { get; set; }
        public string LicensePlate { get; set; }
        public string Vin { get; set; }
        public Guid BrandId { get; set; }
        public Guid? ModelId { get; set; }
        public Guid CustomerId { get; set; }
        public VehicleStatus VehicleStatus { get; set; }
        public short VehicleType { get; set; }
        public Guid? BoxId { get; set; }
    }

    public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
    {
        public CreateVehicleCommandValidator()
        {
            RuleFor(v => v.VehicleName)
                .NotEmpty();
            RuleFor(v => v.Vin)
                .NotEmpty();
            RuleFor(v => v.BrandId)
                .NotEmpty().Must(v => v != Guid.Empty);
            RuleFor(v => v.BoxId)
                .NotEmpty().Must(v => v != Guid.Empty);
            RuleFor(v => v.CustomerId)
                .NotEmpty()
                .Must(v=>v!= Guid.Empty);
        }
    }
}
