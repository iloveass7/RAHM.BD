using Microsoft.AspNetCore.Mvc;

public class AdminController : Controller
{
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
}
