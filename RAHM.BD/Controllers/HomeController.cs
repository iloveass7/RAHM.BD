using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RAHM.BD.Models;



namespace RAHM.BD.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        public IActionResult RequestCenter() => View();

        public IActionResult RequestVaccine() => View();

        public IActionResult Tips() => View();

        public IActionResult Signup() => View();

        public IActionResult Login() => View();
    }
}
