using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using SmartFleet.Core.Data;
using SmartFleet.Core.Domain.Gpsdevices;
using SmartFleet.Core.Domain.Vehicles;
using SmartFleet.Data;

namespace SmartFleet.Customer.Domain.Commands.Vehicles
{
    public class VehiclesCommandsHandler :
        IRequestHandler<CreateVehicleCommand, ValidationResult>
    {

        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        private readonly IMapper _mapper;

        public VehiclesCommandsHandler(IDbContextScopeFactory dbContextScopeFactory, IMapper mapper)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
            _mapper = mapper;
        }


        public async Task<ValidationResult> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
        {
            using (var contextFScope = _dbContextScopeFactory.Create())
            {
                var db = contextFScope.DbContexts.Get<SmartFleetObjectContext>();
                var vehicle = _mapper.Map<Vehicle>(request);
                vehicle.MileStoneUpdateUtc = DateTime.Now;
                if (vehicle.Box_Id.HasValue)
                    vehicle.VehicleStatus = VehicleStatus.Active;
                var boxId = request.BoxId;
                var box = await db.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, cancellationToken)
                    .ConfigureAwait(false);

                if (box != null)
                {
                    box.VehicleId = request.CmdId;
                    box.BoxStatus = BoxStatus.Valid;

                }

                db.Vehicles.Add(vehicle);
                await contextFScope.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            }

            return default;
        }
    }
}
