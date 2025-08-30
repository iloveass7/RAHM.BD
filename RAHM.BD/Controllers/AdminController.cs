using Microsoft.AspNetCore.Mvc;

public class AdminController : Controller
{
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        if (username == "admin" && password == "123") // placeholder
        {
            TempData["Msg"] = "Welcome Admin!";
            return RedirectToAction("Index");
        }

        ViewBag.Error = "Invalid credentials.";
        return View();
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Admin Dashboard";
        return View();
    }

    public IActionResult Users()
    {
        ViewData["Title"] = "View Users";
        return View();
    }

    public IActionResult Vaccine()
    {
        ViewData["Title"] = "Vaccine Management";
        return View();
    }

    public IActionResult SendSms()
    {
        ViewData["Title"] = "Send SMS";
        return View();
    }

    public IActionResult SendMail()
    {
        ViewData["Title"] = "Send Mail";
        return View();
    }

    public IActionResult UploadContent()
    {
        ViewData["Title"] = "Upload Content";
        return View();
    }
    public IActionResult HealthcareCenter()
    {
        ViewData["Title"] = "Healthcare Center Management";
        return View();
    }

    public IActionResult Medicine()
    {
        ViewData["Title"] = "Medicine Management";
        return View();
    }

    public IActionResult Disease()
    {
        ViewData["Title"] = "Disease Management";
        return View();
    }

}
