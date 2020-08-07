using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;

namespace SmartFleet.Service.Authentication
{
    public interface IAuthenticationService
    {
        Task<IdentityUser> AuthenticationAsync(string userName, string password, bool remember);
        IEnumerable<string> GetRoleByUserId(string userId);
        void Logout();
    }
}