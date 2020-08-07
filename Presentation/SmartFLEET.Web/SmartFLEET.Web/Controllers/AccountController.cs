using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SmartFleet.Service.Authentication;
using SmartFLEET.Web.Models.Account;

namespace SmartFLEET.Web.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authenticationService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authenticationService"></param>
        public AccountController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }
        // GET: Account
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home", new { area = "" });
            return View(new LoginModel());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Login(LoginModel model)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home", new {area = ""});
            if (!ModelState.IsValid) return View(model);
            var userExists =
                await _authenticationService.AuthenticationAsync(model.UserName, model.Password, model.RememberMe).ConfigureAwait(false);
            if (userExists == null) return View();
            return _authenticationService.GetRoleByUserId(userExists.Id).Any(identityUserRole => identityUserRole.Equals("customer") || identityUserRole.Equals("user")) ?
                RedirectToAction("Index", "Home") :
                RedirectToAction("Index", "Admin", new {area = "Administrator"});
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Logout()
        {
            _authenticationService.Logout();
            return RedirectToAction("Login", "Account", new { area = "" });
        }
    }
}