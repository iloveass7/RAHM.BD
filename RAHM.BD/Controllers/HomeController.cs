using Microsoft.AspNetCore.Mvc;
using RAHM.BD.Services;

namespace RAHM.BD.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDb _db;

        public HomeController(IDb db)
        {
            _db = db;
        }

        public IActionResult Index() => View();
        public IActionResult RequestCenter() => View();
        public IActionResult RequestVaccine() => View();
        public IActionResult Tips() => View();
    }
}
