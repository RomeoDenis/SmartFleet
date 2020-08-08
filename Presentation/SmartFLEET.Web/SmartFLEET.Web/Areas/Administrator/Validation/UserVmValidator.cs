using FluentValidation;
using SmartFleet.Service.Customers;
using SmartFLEET.Web.Areas.Administrator.Models;

namespace SmartFLEET.Web.Areas.Administrator.Validation
{
    public class UserVmValidator: AbstractValidator<UserVm>
    {
        private readonly ICustomerService _customerService;
        public UserVmValidator(ICustomerService customerService)
        {
            _customerService = customerService;
            RuleFor(vehicle => vehicle.UserName).NotEmpty().Must(UniqueName).WithMessage("This  name already exists.");
            RuleFor(vehicle => vehicle.Password).NotEmpty().WithMessage("Le champs marque est requis"); ;
            //   RuleFor(vehicle => vehicle.Model).NotEmpty().WithMessage("Le modèle de véhicule est requis");
            RuleFor(vehicle => vehicle.Email).NotEmpty();
            RuleFor(vehicle => vehicle.TimeZoneInfo).NotEmpty();
        }

        private bool UniqueName(string arg)
        {
            return _customerService.GetUserByNameAsync(arg).Result ;
        }
    }
}