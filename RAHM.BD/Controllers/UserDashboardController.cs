using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RAHM.BD.Models;
using RAHM.BD.Services;

namespace RAHM.BD.Controllers
{
    public class UserDashboardController : Controller
    {
        private readonly IDb _db;

        public UserDashboardController(IDb db)
        {
            _db = db;
        }

        // GET: /UserDashboard
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Not logged in → redirect to login
                return RedirectToAction("Login", "Home");
            }

            string sql = "SELECT * FROM Users WHERE Id=@Id";
            var user = await _db.QuerySingleAsync(sql,
                r => new User
                {
                    Id = (int)r["Id"],
                    Name = (string)r["Name"],
                    MobileNo = (string)r["MobileNo"],
                    Email = (string)r["Email"],
                    PasswordHash = (string)r["PasswordHash"]
                },
                new SqlParameter("@Id", userId)
            );

            if (user == null)
            {
                // Safety: user deleted from DB → force logout
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Home");
            }

            return View(user); // 👈 IMPORTANT
        }


        // GET: /UserDashboard/Edit
        public async Task<IActionResult> Edit()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Home");

            string sql = "SELECT * FROM Users WHERE Id=@Id";
            var user = await _db.QuerySingleAsync(sql,
                r => new User
                {
                    Id = (int)r["Id"],
                    Name = (string)r["Name"],
                    MobileNo = (string)r["MobileNo"],
                    Email = (string)r["Email"],
                    PasswordHash = (string)r["PasswordHash"]
                },
                new SqlParameter("@Id", userId)
            );

            return View(user);
        }

        // POST: /UserDashboard/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(User updatedUser)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Home");

            string sql = @"UPDATE Users SET Name=@Name, MobileNo=@MobileNo, Email=@Email
                           WHERE Id=@Id";

            await _db.ExecuteAsync(sql,
                new SqlParameter("@Name", updatedUser.Name),
                new SqlParameter("@MobileNo", updatedUser.MobileNo),
                new SqlParameter("@Email", updatedUser.Email),
                new SqlParameter("@Id", userId)
            );

            TempData["Message"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }
        public class LocationDto
        {
            public double? Lat { get; set; }
            public double? Lng { get; set; }
        }

        // Save user location
        [HttpPost]
        public async Task<IActionResult> SetLocation([FromBody] LocationDto dto)
        {
            if (dto.Lat == null || dto.Lng == null)
                return BadRequest("Location required");

            // Replace with actual userId from session/auth
            int userId = 1;

            string sqlCheck = "SELECT COUNT(*) FROM Locations WHERE UserId=@UserId";
            int exists = await _db.QuerySingleAsync<int>(sqlCheck, r => r.GetInt32(0), new SqlParameter("@UserId", userId));

            if (exists > 0)
            {
                await _db.ExecuteAsync("UPDATE Locations SET Lat=@Lat, Lng=@Lng WHERE UserId=@UserId",
                    new SqlParameter("@Lat", dto.Lat),
                    new SqlParameter("@Lng", dto.Lng),
                    new SqlParameter("@UserId", userId));
            }
            else
            {
                await _db.ExecuteAsync("INSERT INTO Locations (UserId, Lat, Lng, Road, District, Division) VALUES (@UserId, @Lat, @Lng, '', '', '')",
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Lat", dto.Lat),
                    new SqlParameter("@Lng", dto.Lng));
            }

            return Ok();
        }

        // Get top 3 nearest centers
        [HttpPost]
        public async Task<IActionResult> GetNearestCenters([FromBody] LocationDto dto)
        {
            if (dto.Lat == null || dto.Lng == null)
                return BadRequest("Location required");

            string sql = @"
                SELECT TOP 3 Id, Name, Road, District, Division, Lat, Lng,
                    (6371 * acos(
                        cos(radians(@Lat)) * cos(radians(Lat)) *
                        cos(radians(Lng) - radians(@Lng)) +
                        sin(radians(@Lat)) * sin(radians(Lat))
                    )) AS DistanceKm
                FROM HealthCenters
                ORDER BY DistanceKm";

            var centers = await _db.QueryAsync(sql, reader => new {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Road = reader.GetString(2),
                District = reader.GetString(3),
                Division = reader.GetString(4),
                Lat = reader.GetDouble(5),
                Lng = reader.GetDouble(6),
                DistanceKm = reader.GetDouble(7)
            },
            new SqlParameter("@Lat", dto.Lat),
            new SqlParameter("@Lng", dto.Lng));

            return Json(centers.Select(c => new {
                c.Id,
                c.Name,
                c.Road,
                c.District,
                c.Lat,
                c.Lng,
                DistanceKm = Math.Round(c.DistanceKm, 2)
            }));
        }
    }
}