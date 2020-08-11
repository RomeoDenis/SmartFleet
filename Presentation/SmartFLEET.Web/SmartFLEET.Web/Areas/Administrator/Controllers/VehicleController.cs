using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using AutoMapper;
using MediatR;
using SmartFleet.Core.Domain.Vehicles;
using SmartFleet.Customer.Domain.Queries.Vehicles;
using SmartFleet.Service.Vehicles;
using SmartFleet.Web.Framework.DataTables;
using SmartFLEET.Web.Areas.Administrator.Models;
using SmartFLEET.Web.Controllers;

namespace SmartFLEET.Web.Areas.Administrator.Controllers
{
    public class VehicleController : BaseController
    {
        private readonly IVehicleService _vehicleService;
        private readonly DataTablesLinqQueryBulider _queryBuilder;
        public VehicleController(IMediator mediator, IMapper mapper, IVehicleService vehicleService, DataTablesLinqQueryBulider queryBuilder) : base(mediator, mapper)
        {
            _vehicleService = vehicleService;
            _queryBuilder = queryBuilder;
        }

        public ActionResult Index()
        {
            return PartialView("Index");
        }
        public ActionResult GetListForCustomer()
        {
            return PartialView("_List");
        }
        //[HttpGet]
        public async Task<JsonResult> GetAllVehicles()
        {
            var data = await Mediator.Send(new GetVehiclesListQuery
            {
                Request = Request
            }).ConfigureAwait(false);
            try
            {
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        //[HttpGet]
        public async Task<JsonResult> GetAllVehiclesForCustomer  (string customerId)
        {
            var query = _queryBuilder.BuildQuery(Request , _vehicleService.GetVehiclesOfCustomer(Guid.Parse(customerId)));
            var jsResult = new
            {
                recordsTotal = query.recordsTotal,
                draw = query.draw,
                recordsFiltered = query.recordsFiltered,
                data = Mapper.Map<List<VehicleViewModel>>(query.data),
                lenght = query.length
            };
            return Json(jsResult, JsonRequestBehavior.AllowGet);
        }

       
        public ActionResult Detail()
        {
            //var id = Guid.Parse(vehicleId);
            //var vehicleviewModel = Mapper.Map<VehicleViewModel>(ObjectContext.Vehicles.Include("Customer").FirstOrDefault(x=>x.Id == id));
            return PartialView("Detail");
        }
        public ActionResult Create()
        {
            //var id = Guid.Parse(vehicleId);
            //var vehicleviewModel = Mapper.Map<VehicleViewModel>(ObjectContext.Vehicles.Include("Customer").FirstOrDefault(x=>x.Id == id));
            return PartialView("_Create");
        }

        public async Task<JsonResult> GetVehicleDetail(string vehicleId)
        {
            var id = Guid.Parse(vehicleId);
            return Json(Mapper.Map<VehicleViewModel>(await _vehicleService.GetVehicleByIdWithDetailAsync(id)),
                JsonRequestBehavior.AllowGet);
        }

        

        [HttpGet]
        public ActionResult GetNewVehicle()
        {
            return Json(new AddVehicleViewModel(Mediator), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AddNewVehicle(AddVehicleViewModel model)
        {
            ValidationViewModel validationModel;

            if (ModelState.IsValid)
            {
                var vehicle = Mapper.Map<Vehicle>(model);
                vehicle.VehicleType = (VehicleType)Enum.Parse(typeof(VehicleType), model.VehicleType);
                _vehicleService.AddNewVehicleAsync(vehicle);
                validationModel = new ValidationViewModel(new List<string>(), "Ok");
                return Json(validationModel, JsonRequestBehavior.AllowGet);
            }

            var errors = (from modelStateValue in ModelState.Values
                from error in modelStateValue.Errors
                select error.ErrorMessage).ToList();
            validationModel = new ValidationViewModel(errors, "Validation errors");

            return Json(validationModel, JsonRequestBehavior.AllowGet);
        }
    }
}