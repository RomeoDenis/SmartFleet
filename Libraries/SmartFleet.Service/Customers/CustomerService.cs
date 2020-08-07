using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using SmartFleet.Core.Data;
using SmartFleet.Core.Domain.Customers;
using SmartFleet.Core.Domain.Users;
using SmartFleet.Core.Domain.Vehicles;
using SmartFleet.Data;

namespace SmartFleet.Service.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly IRepository<Customer> _customerRepository;
        private readonly SmartFleetObjectContext _objectContext;
        private readonly UserManager<User> _userManager;
        public CustomerService(IRepository<Customer> customerRepository,SmartFleetObjectContext objectContext)
        {
            _customerRepository = customerRepository;
            _objectContext = objectContext;
           _userManager = new UserManager<User>(new UserStore<User>(_objectContext));

        }
        public bool AddCustomer(Customer customer, List<User> users)
        {
            try
            {
                customer.Id = Guid.NewGuid();
                _customerRepository.Insert(customer);
                if (!users.Any()) return true;
                var passwordHash = new PasswordHasher();
                foreach (var user in users)
                {

                    if (_objectContext.Users.Any(u => u.UserName == user.UserName)) continue;
                    user.PasswordHash =  passwordHash.HashPassword(user.Password);
                    user.CustomerId = customer.Id;
                    _userManager.Create(user);
                    _userManager.AddToRole(user.Id, user.Role);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }

        }
        public Customer GetCustomerbyName(string name)
        {
            var user = _userManager.Users.Include(x=>x.Customer)
                .FirstOrDefault(x => x.UserName == name);
           
            return user?.Customer;
        }

        public async Task<Customer> GetCustomerWithZonesAndVehiclesAsync(string name)
        {
            var user = await _userManager.Users
                .Include(x => x.Customer)
                .Include(x => x.Customer.Areas)
                .Include(x => x.Customer.Vehicles)
                .FirstOrDefaultAsync(x => x.UserName == name).ConfigureAwait(false);

            return user?.Customer;
        }

        public async Task<Customer> GetCustomerbyName(Guid id)
        {
            var cst = _objectContext.Customers
                //.Include(x => x.Vehicles)
                //.Include(x=>x.Users)
                .FirstOrDefaultAsync(x => x.Id == id);
            return await cst;
        }

        public IQueryable<Customer> GetCustomers( )
        {
            return _objectContext.Customers;
        }

        public Task<Boolean> GetUserbyName(string id)
        {
            return _userManager.Users.AnyAsync(u => u.UserName == id);
        }

        public async Task<List<InterestArea>> GetAllAreas(string userName, int page , int size)
        {
            var customer =await _userManager.Users.Include(x=>x.Customer).Select(x=> new { x.CustomerId , x.UserName}).FirstOrDefaultAsync(x => x.UserName == userName).ConfigureAwait(false);
            if (customer != null)
                return await _objectContext.InterestAreas
                    .OrderBy(x=>x.Name)
                    .Skip(page-1)
                    .Take(size*page)
                    .ToListAsync();
            return new List<InterestArea>();
        }

        public async Task<List<InterestArea>> GetAllAreas(string userName)
        {
            var customer = await _userManager.Users.Include(x => x.Customer).Select(x => new { x.CustomerId, x.UserName }).FirstOrDefaultAsync(x => x.UserName == userName).ConfigureAwait(false);
            if (customer != null)
                return await _objectContext.InterestAreas
                    .OrderBy(x => x.Name)
                    .ToListAsync().ConfigureAwait(false);
            return new List<InterestArea>();
        }

        public bool AddArea(InterestArea area)
        {
            try
            {
                _objectContext.InterestAreas.Add(area);
                _objectContext.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<List<Vehicle>> GetAllVehiclesOfUserAsync(string userName,int page, int rows )
        {
            var vehicles = await _userManager.Users.Where(x => x.UserName == userName)
                .Include(x => x.Customer)
                .Include(x => x.Customer.Vehicles)
                .SelectMany(x => x.Customer.Vehicles)
                .OrderBy(v => v.VehicleName)
                .Skip(page-1)
                .Take(page*rows)
               
                .ToListAsync().ConfigureAwait(false);
            return vehicles;
        }
    }
}
