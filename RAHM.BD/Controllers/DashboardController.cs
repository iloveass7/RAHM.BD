using Microsoft.AspNetCore.Mvc;

public class DashboardController : Controller
{
    [Route("dashboard")]
    public IActionResult Index()
    {
        return View();
    }
}
