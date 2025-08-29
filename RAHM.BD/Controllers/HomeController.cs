using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RAHM.BD.Models;
using RAHM.BD.Services;
using System.Security.Cryptography;
using System.Text;

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

        [HttpGet]
        public IActionResult Signup() => View();

        [HttpPost]
        public async Task<IActionResult> Signup(string Name, string MobileNo, string Email,
                                               string Password, string ConfirmPassword,
                                               string Road, string District, string Division,
                                               double? Lat, double? Lng)
        {
            if (Password != ConfirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            string passwordHash = HashPassword(Password);

            // Insert user
            string sqlUser = @"INSERT INTO Users (Name, MobileNo, Email, PasswordHash)
                               OUTPUT INSERTED.Id
                               VALUES (@Name, @MobileNo, @Email, @PasswordHash)";

            var userId = await _db.QuerySingleAsync(sqlUser,
                r => (int)r["Id"],
                new SqlParameter("@Name", Name),
                new SqlParameter("@MobileNo", MobileNo),
                new SqlParameter("@Email", Email),
                new SqlParameter("@PasswordHash", passwordHash)
            );

            // Insert location
            if (userId != null)
            {
                string sqlLoc = @"INSERT INTO Locations (UserId, Road, District, Division, Lat, Lng)
                                  VALUES (@UserId, @Road, @District, @Division, @Lat, @Lng)";

                await _db.ExecuteAsync(sqlLoc,
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Road", Road),
                    new SqlParameter("@District", District),
                    new SqlParameter("@Division", Division),
                    new SqlParameter("@Lat", (object?)Lat ?? DBNull.Value),
                    new SqlParameter("@Lng", (object?)Lng ?? DBNull.Value)
                );
            }

            TempData["Message"] = "Signup successful! Please log in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            string passwordHash = HashPassword(Password);

            string sql = "SELECT * FROM Users WHERE Email=@Email AND PasswordHash=@PasswordHash";
            var user = await _db.QuerySingleAsync(sql,
                r => new User
                {
                    Id = (int)r["Id"],
                    Name = (string)r["Name"],
                    Email = (string)r["Email"],
                    MobileNo = (string)r["MobileNo"],
                    PasswordHash = (string)r["PasswordHash"]
                },
                new SqlParameter("@Email", Email),
                new SqlParameter("@PasswordHash", passwordHash)
            );

            if (user == null)
            {
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            // Save login info in session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.Name);

            return RedirectToAction("Index");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
