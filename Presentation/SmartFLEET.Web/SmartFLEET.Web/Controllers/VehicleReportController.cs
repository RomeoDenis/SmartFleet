using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using AutoMapper;
using Microsoft.AspNet.SignalR;
using SmartFleet.Data;
using SmartFleet.Service.Common;
using SmartFleet.Service.Customers;
using SmartFleet.Service.Models;
using SmartFleet.Service.Report;
using SmartFleet.Service.Tracking;
using SmartFleet.Service.Vehicles;
using SmartFLEET.Web.Areas.Administrator.Models;
using SmartFLEET.Web.Helpers;
using SmartFLEET.Web.Hubs;

namespace SmartFLEET.Web.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class VehicleReportController : BaseController
    {
        private readonly IPositionService _positionService;
        private readonly ICustomerService _customerService;
        private readonly IVehicleService _vehicleService;
        private readonly IPdfService _pdfService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectContext"></param>
        /// <param name="mapper"></param>
        /// <param name="positionService"></param>
        /// <param name="customerService"></param>
        /// <param name="vehicleService"></param>
        /// <param name="pdfService"></param>
        public VehicleReportController(SmartFleetObjectContext objectContext,
            IMapper mapper,
            IPositionService positionService,
            ICustomerService customerService,
            IVehicleService vehicleService, 
            IPdfService pdfService ) : base(objectContext, mapper)
        {
            _positionService = positionService;
            _customerService = customerService;
            _vehicleService = vehicleService;
            _pdfService = pdfService;
        }
        // GET: VehicleReport
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return PartialView();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> GetVehicles()
        {
            var cst = _customerService.GetCustomerbyName(User.Identity.Name);
            var vehicles = await ObjectContext.Vehicles.Where(x => x.CustomerId == cst.Id).Select(x=>new {x.VehicleName, x.Id}).ToArrayAsync();
            return Json(vehicles, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<JsonResult> AllVehiclesWithLastPosition()
        {
            var report = new ActivitiesReport();
            var user = User.Identity;
            var positions = report.PositionViewModels(await _positionService.GetLastVehiclPosition(user.Name));
            return Json(positions, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="startPeriod"></param>
        /// <returns></returns>
        public async Task<JsonResult> GetDailyVehicleReport(string vehicleId, string startPeriod)
        {
            var id = Guid.Parse(vehicleId);
            DateTime start;
            try
            {
                DateTime.TryParseExact(startPeriod, "yyyy-MM-dd", null, DateTimeStyles.AssumeLocal, out start);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var hubContext = GlobalHost.ConnectionManager.GetHubContext<SignalRHandler>();
            var report = new CompleteDailyReport();
            var connctionId = string.Empty;
            try
            {
               connctionId= SignalRHubManager.Connections[User.Identity.Name];
            }
            catch (Exception)
            {
                // ignored
            }

            var endPeriod = start.AddHours(24).AddTicks(-1);
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            var positions = await _positionService.GetVehiclePositionsByPeriod(id, start, endPeriod);

            if (!string.IsNullOrEmpty(connctionId))
                hubContext.Clients.Client(connctionId).sendprogressVal(50);
            if (!positions.Any())
            {
                return Json(new CompleteDailyReport
                    {
                        VehicleName = vehicle?.VehicleName,
                        ReportDate = start.ToShortDateString(),
                        Positions = new List<TargetViewModel>()
                    }, JsonRequestBehavior.AllowGet);

            }
            report.UpdateProgress += val =>
            {
                if(string.IsNullOrEmpty(connctionId))
                    return;
                hubContext.Clients.Client(connctionId)
                    .sendprogressVal(val);
            };
            report.Build(positions.OrderBy(p => p.Timestamp).ToList(), vehicle);
            report.FuelConsumption = _vehicleService.GetFuelConsuptionByPeriod(start, endPeriod, id, report.Distance);
            report.FuelConsumptions = _vehicleService.GetFuelConsuptionList(start, endPeriod, id).ToList();
            return Json(report, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// export daily report to pdf
        /// </summary>
        /// <param name="vehicleId"></param>
        /// <param name="startPeriod"></param>
        /// <returns></returns>
        public async Task<FileResult> ExportReportPdf(string vehicleId, string startPeriod)
        {
            var id = Guid.Parse(vehicleId);
            DateTime start;
            try
            {
                DateTime.TryParseExact(startPeriod, "yyyy-MM-dd", null, DateTimeStyles.AssumeLocal, out start);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            var endPeriod = start.AddHours(24).AddTicks(-1);
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id).ConfigureAwait(false);
            var positions = await _positionService.GetVehiclePositionsByPeriod(id, start, endPeriod).ConfigureAwait(false);
            MemoryStream stream = new MemoryStream();
            if (positions.Any())
            {
                var report = new CompleteDailyReport(positions.OrderBy(p=>p.Timestamp).ToList(), vehicle);
                _pdfService.CreatePdfReport(positions.OrderBy(p => p.Timestamp).ToList(), vehicle, report,stream);
            }
            stream.Flush(); //Always catches me out
            stream.Position = 0; //Not sure if this is required
            return File(stream, "application/pdf", $"reeport_{vehicle?.VehicleName}_{DateTime.Now.Date:yyyy-MM-dd}.pdf");

        }

        // ReSharper disable once MethodTooLong
        public async Task<JsonResult> GetListOfVehicls()
        {
            var parm = RequestHelper.GetDataGridParams(Request);
            var vehicles = Mapper.Map<List<VehicleViewModel>>(await _customerService.GetAllVehiclesOfUserAsync(User.Identity.Name, parm.page, parm.rows));
            return Json(vehicles, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetListOfvehicles()
        {
            return PartialView("_List");
        }

    }
}