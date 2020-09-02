using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using MediatR;
using SmartFleet.Data;
using SmartFleet.MobileUnit.Domain.MobileUnit.Commands;
using SmartFLEET.Web.Controllers;

namespace SmartFLEET.Web.Areas.Administrator.Controllers
{
    public class GpsDeviceController : BaseController
    {
        // GET
        public ActionResult Index()
        {
            return View(ObjectContext.Boxes.ToList());
        }

        public ActionResult Create()
        {
            return View();
        }

        public async Task<ActionResult> AddMobileUnit(CreateMobileUnitCommand command)
        {
            try
            {
                await SendAsync(command).ConfigureAwait(false);
                return Json("ok");
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }
        public GpsDeviceController(SmartFleetObjectContext objectContext , IMediator mediator) : base(objectContext, mediator)
        {
        }
        
        
    }
}