
namespace LearnMsNet.CoreBlog.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService) => this._userService = userService;

    [Route("/login")]
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData[BlogConstants.RETURNURL] = returnUrl;
        return View();
    }

    [Route("/login")]
    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAsync(string? returnUrl, LoginViewModel? model)
    {
        ViewData[BlogConstants.RETURNURL] = returnUrl;

        if (model is null || model.UserName is null || model.Password is null)
        {
            ModelState.AddModelError(string.Empty, LearnMsNet.CoreBlog.Config.Resources.UsernameOrPasswordIsInvalid);
            return View(nameof(Login), model);
        }

        if (!ModelState.IsValid || !_userService.ValidateUser(model.UserName, model.Password))
        {
            ModelState.AddModelError(string.Empty, LearnMsNet.CoreBlog.Config.Resources.UsernameOrPasswordIsInvalid);
            return this.View(nameof(Login), model);
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, model.UserName));

        var principle = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties { IsPersistent = model.RememberMe };
        await HttpContext.SignInAsync(principle, properties).ConfigureAwait(false);

        return LocalRedirect(returnUrl ?? "/");
    }

    [Route("/logout")]
    public async Task<IActionResult> LogOutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        return LocalRedirect("/");
    }
}
