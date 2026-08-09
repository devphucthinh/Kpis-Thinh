using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
