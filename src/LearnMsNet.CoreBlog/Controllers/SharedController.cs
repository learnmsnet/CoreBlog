using Microsoft.AspNetCore.Mvc;

namespace LearnMsNet.CoreBlog.Controllers
{
    public class SharedController : Controller
    {
        public IActionResult Error() => View(Response.StatusCode);
        public IActionResult Offline() => View();
    }
}
