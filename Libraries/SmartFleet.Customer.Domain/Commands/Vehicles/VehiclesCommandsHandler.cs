using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace SmartFleet.Customer.Domain.Commands.Vehicles
{
    public class VehiclesCommandsHandler : IRequestHandler<CreateVehicleCommand>
    {
        public Task<Unit> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
