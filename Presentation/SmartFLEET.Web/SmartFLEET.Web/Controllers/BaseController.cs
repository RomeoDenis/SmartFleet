using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using AutoMapper;
using FluentValidation.Results;
using MediatR;
using SmartFleet.Data;
using SmartFLEET.Web.Areas.Administrator.Models;
using SmartFLEET.Web.Helpers;

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

        protected ValidationViewModel ValidationViewModel()
        {
            ValidationViewModel validationModel;
            var errors = (from modelStateValue in ModelState.Values
                from error in modelStateValue.Errors
                select error.ErrorMessage).ToList();
            validationModel = new ValidationViewModel(errors, "Validation errors");
            return validationModel;
        }
        protected List<string> ValidationViewModel(ValidationResult result)
        {
            var errors = new List<string>();
            if (result?.Errors != null)
                foreach (var error in result.Errors)
                    errors.Add(error.ErrorMessage);
            return errors;
        }

      
        protected override bool DisableAsyncSupport
        {
            get { return false; }
        }
        protected Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
        {
            return Mediator.Send(request);
        }
        protected override void Initialize(RequestContext requestContext)
        {
            if (requestContext.RouteData.Values["lang"] != null && requestContext.RouteData.Values["lang"] as string != "null")
            {
                var currentLangCode = requestContext.RouteData.Values["lang"] as string;
                try
                {
                    var ci = new CultureInfo(currentLangCode);
                    Thread.CurrentThread.CurrentUICulture = ci;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(ci.Name);
                }
                catch (CultureNotFoundException ex)
                {
                }
            }

            base.Initialize(requestContext);
        }
    }
}