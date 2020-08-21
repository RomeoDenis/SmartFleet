using SmartFleet.Core.Contracts.Commands;

namespace TeltonicaService.Handlers
{
    public interface ITeltonikaAvlService
    {
         CreateTeltonikaGps Data { get; set; }
         
    }
}