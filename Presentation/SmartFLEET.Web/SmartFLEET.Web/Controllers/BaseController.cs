using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using MediatR;
using SmartFleet.Data;
using SmartFLEET.Web.Areas.Administrator.Models;

namespace SmartFLEET.Web.Controllers
{
    public class BaseController : Controller
    {
        protected readonly SmartFleetObjectContext ObjectContext;
        protected readonly IMediator Mediator;
        protected readonly IMapper Mapper;

        public BaseController(SmartFleetObjectContext objectContext)
        {
            ObjectContext = objectContext;
        }
        public BaseController(SmartFleetObjectContext objectContext, IMapper mapper)
        {
            ObjectContext = objectContext;
            Mapper = mapper;
        }
        public BaseController(IMediator mediator, IMapper mapper)
        {
            Mediator = mediator;
            Mapper = mapper;
        }
        public BaseController( IMapper mapper)
        {
            Mapper = mapper;
        }

        public ValidationViewModel ValidationViewModel()
        {
            ValidationViewModel validationModel;
            var errors = (from modelStateValue in ModelState.Values
                from error in modelStateValue.Errors
                select error.ErrorMessage).ToList();
            validationModel = new ValidationViewModel(errors, "Validation errors");
            return validationModel;
        }

    }
}