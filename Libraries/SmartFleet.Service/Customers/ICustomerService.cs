using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SmartFleet.Core.Domain.Customers;
using SmartFleet.Core.Domain.Users;
using SmartFleet.Core.Domain.Vehicles;

namespace SmartFleet.Service.Customers
{
    public interface ICustomerService
    {
        /// <summary>
        /// add new customer
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="users"></param>
        /// <returns></returns>
        bool AddCustomer(Customer customer, List<User> users );
        /// <summary>
        /// gets customer by current user's name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Customer GetCustomerbyName(string name);
        /// <summary>
        /// get  customer along with vehicles and zones by current user's name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<Customer> GetCustomerWithZonesAndVehiclesAsync(string name);
        /// <summary>
        /// get cuustomer by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Customer> GetCustomerbyName(Guid id);
        /// <summary>
        /// get all customers
        /// </summary>
        /// <returns></returns>
        IQueryable<Customer> GetCustomers();
        /// <summary>
        /// get user by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<bool> GetUserbyName(string name);
        /// <summary>
        /// get all aareas of customer by current user name
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        Task<List<InterestArea>> GetAllAreas(string userName, int page, int size );
        /// <summary>
       /// get all aareas of customer by current user name

        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task<List<InterestArea>> GetAllAreas(string userName);

        /// <summary>
        /// add new zone
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        bool AddArea(InterestArea area);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task<List<Vehicle>> GetAllVehiclesOfUserAsync(string userName, int page, int rows);

    }
}