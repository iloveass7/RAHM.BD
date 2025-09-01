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
        public IActionResult Medicine()
        {
            ViewData["Title"] = "Medicine Lookup";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchMedicine(string symptoms, string severity, string duration)
        {
            

            return Json(new { success = true, message = "Backend implementation needed" });
        }
        public IActionResult Blog(string post = "seasonal-flu")
        {
            ViewData["Title"] = "Blog Post";
            return View();
        }
    }
}
