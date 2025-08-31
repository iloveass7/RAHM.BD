using Microsoft.AspNetCore.Mvc;
using RAHM.BD.Models;
using RAHM.BD.Services;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

public class AdminController : Controller
{
    private readonly IDb _db;
    public AdminController(IDb db)
    {
        _db = db;
    }

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

    //public IActionResult Users()
    //{
    //    ViewData["Title"] = "View Users";
    //    return View();
    //}

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

    public IActionResult Medicine()
    {
        ViewData["Title"] = "Medicine Management";
        return View();
    }

    //public IActionResult Disease()
    //{
    //    ViewData["Title"] = "Disease Management";
    //    return View();
    //}

    // ===============================
    // 🚑 Healthcare Center Management
    // ===============================

    // GET: /Admin/HealthcareCenter
    public async Task<IActionResult> HealthcareCenter()
    {
        string sql = "SELECT Id, Name, Road, District, Division, Lat, Lng FROM HealthCenters";
        var centers = await _db.QueryAsync(sql, r => new HealthCenter
        {
            Id = r.GetInt32(0),
            Name = r.GetString(1),
            Road = r.GetString(2),
            District = r.GetString(3),
            Division = r.GetString(4),
            Lat = r.GetDouble(5),
            Lng = r.GetDouble(6)
        });

        return View(centers); // 👈 will use HealthcareCenter.cshtml
    }

    // POST: /Admin/AddHealthcareCenter
    [HttpPost]
    public async Task<IActionResult> AddHealthcareCenter(HealthCenter center)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("HealthcareCenter");

        string sql = @"INSERT INTO HealthCenters (Name, Road, District, Division, Lat, Lng) 
                       VALUES (@Name, @Road, @District, @Division, @Lat, @Lng)";
        await _db.ExecuteAsync(sql,
            new SqlParameter("@Name", center.Name),
            new SqlParameter("@Road", center.Road),
            new SqlParameter("@District", center.District),
            new SqlParameter("@Division", center.Division),
            new SqlParameter("@Lat", center.Lat),
            new SqlParameter("@Lng", center.Lng));

        TempData["Message"] = "Healthcare center added successfully!";
        return RedirectToAction("HealthcareCenter");
    }

    // POST: /Admin/DeleteHealthcareCenter/{id}
    [HttpPost]
    public async Task<IActionResult> DeleteHealthcareCenter(int id)
    {
        string sql = "DELETE FROM HealthCenters WHERE Id=@Id";
        await _db.ExecuteAsync(sql, new SqlParameter("@Id", id));

        TempData["Message"] = "Healthcare center deleted successfully!";
        return RedirectToAction("HealthcareCenter");
    }

    // GET: /Admin/Users
    public async Task<IActionResult> Users()
    {
        string sql = @"
        SELECT u.Id, u.Name, u.Email, u.MobileNo,
               l.Road, l.District, l.Division
        FROM Users u
        LEFT JOIN Locations l ON u.Id = l.UserId";

        var users = await _db.QueryAsync(sql, r => new
        {
            Id = r.GetInt32(0),
            Name = r.GetString(1),
            Email = r.GetString(2),
            MobileNo = r.GetString(3),
            Road = r.IsDBNull(4) ? "" : r.GetString(4),
            District = r.IsDBNull(5) ? "" : r.GetString(5),
            Division = r.IsDBNull(6) ? "" : r.GetString(6),
        });

        return View(users);  // Users.cshtml uses @model IEnumerable<dynamic>
    }



    // GET: /Admin/EditUser/{id}
    public async Task<IActionResult> EditUser(int id)
    {
        string sql = @"
        SELECT u.Id, u.Name, u.Email, u.MobileNo,
               l.Road, l.District, l.Division
        FROM Users u
        LEFT JOIN Locations l ON u.Id = l.UserId
        WHERE u.Id = @Id";

        var user = await _db.QuerySingleAsync(sql, r => new
        {
            Id = r.GetInt32(0),
            Name = r.GetString(1),
            Email = r.GetString(2),
            MobileNo = r.GetString(3),
            Road = r.IsDBNull(4) ? "" : r.GetString(4),
            District = r.IsDBNull(5) ? "" : r.GetString(5),
            Division = r.IsDBNull(6) ? "" : r.GetString(6),
        }, new SqlParameter("@Id", id));

        return View(user); // EditUser.cshtml uses @model dynamic
    }


    [HttpPost]
    public async Task<IActionResult> EditUser(int Id, string Name, string Email, string MobileNo, string Road, string District, string Division)
    {
        // Update Users table
        await _db.ExecuteAsync(
            "UPDATE Users SET Name=@Name, Email=@Email, MobileNo=@MobileNo WHERE Id=@Id",
            new SqlParameter("@Name", Name),
            new SqlParameter("@Email", Email),
            new SqlParameter("@MobileNo", MobileNo),
            new SqlParameter("@Id", Id)
        );

        // Update or Insert into Locations table
        string checkSql = "SELECT COUNT(*) FROM Locations WHERE UserId=@UserId";
        int exists = await _db.QuerySingleAsync<int>(checkSql, r => r.GetInt32(0), new SqlParameter("@UserId", Id));

        if (exists > 0)
        {
            await _db.ExecuteAsync(
                "UPDATE Locations SET Road=@Road, District=@District, Division=@Division WHERE UserId=@UserId",
                new SqlParameter("@Road", Road),
                new SqlParameter("@District", District),
                new SqlParameter("@Division", Division),
                new SqlParameter("@UserId", Id)
            );
        }
        else
        {
            await _db.ExecuteAsync(
                "INSERT INTO Locations (UserId, Road, District, Division) VALUES (@UserId, @Road, @District, @Division)",
                new SqlParameter("@UserId", Id),
                new SqlParameter("@Road", Road),
                new SqlParameter("@District", District),
                new SqlParameter("@Division", Division)
            );
        }

        return RedirectToAction("Users");
    }

    // DELETE: /Admin/DeleteUser/{id}
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _db.ExecuteAsync("DELETE FROM Locations WHERE UserId=@Id",
            new SqlParameter("@Id", id));
        await _db.ExecuteAsync("DELETE FROM Users WHERE Id=@Id",
            new SqlParameter("@Id", id));

        return RedirectToAction("Users");
    }
    // GET: /Admin/Disease
    public async Task<IActionResult> Disease()
    {
        string sql = "SELECT Id, Name FROM Diseases";
        var diseases = await _db.QueryAsync(sql, r => new {
            Id = r.GetInt32(0),
            Name = r.GetString(1)
        });

        return View(diseases); // Disease.cshtml
    }

    // POST: /Admin/AddDisease
    [HttpPost]
    public async Task<IActionResult> AddDisease(string Name)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            TempData["Error"] = "Disease name is required.";
            return RedirectToAction("Disease");
        }

        string sql = "INSERT INTO Diseases (Name) VALUES (@Name)";
        await _db.ExecuteAsync(sql, new SqlParameter("@Name", Name));

        TempData["Message"] = "Disease added successfully!";
        return RedirectToAction("Disease");
    }

    // GET: /Admin/DiseaseLog
    public async Task<IActionResult> DiseaseLog(string diseaseName = "", string district = "", string division = "")
    {
        string sql = @"
        SELECT dl.Id, u.Name AS UserName, u.Email, u.MobileNo,
               l.Road, l.District, l.Division, d.Name AS DiseaseName
        FROM DiseaseLogs dl
        INNER JOIN Users u ON dl.UserId = u.Id
        INNER JOIN Diseases d ON dl.DiseaseId = d.Id
        LEFT JOIN Locations l ON u.Id = l.UserId
        WHERE (@diseaseName = '' OR d.Name LIKE '%' + @diseaseName + '%')
          AND (@district = '' OR l.District LIKE '%' + @district + '%')
          AND (@division = '' OR l.Division LIKE '%' + @division + '%')";

        var logs = await _db.QueryAsync(sql, r => new
        {
            Id = r.GetInt32(0),
            UserName = r.GetString(1),
            Email = r.GetString(2),
            MobileNo = r.GetString(3),
            Road = r.IsDBNull(4) ? "" : r.GetString(4),
            District = r.IsDBNull(5) ? "" : r.GetString(5),
            Division = r.IsDBNull(6) ? "" : r.GetString(6),
            DiseaseName = r.GetString(7)
        },
        new SqlParameter("@diseaseName", diseaseName),
        new SqlParameter("@district", district),
        new SqlParameter("@division", division));

        return View(logs);
    }
    [HttpPost]
    public async Task<IActionResult> AddDiseaseLog(int UserId, int DiseaseId)
    {
        string sql = "INSERT INTO DiseaseLogs (UserId, DiseaseId, ReportedAt) VALUES (@UserId, @DiseaseId, @ReportedAt)";
        await _db.ExecuteAsync(sql,
            new SqlParameter("@UserId", UserId),
            new SqlParameter("@DiseaseId", DiseaseId),
            new SqlParameter("@ReportedAt", DateTime.Now)
        );

        TempData["Message"] = "Disease log added!";
        return RedirectToAction("DiseaseLog");
    }

    // GET: /Admin/AssignDisease
    public async Task<IActionResult> AssignDisease()
    {
        // Get all districts (distinct)
        var districts = await _db.QueryAsync("SELECT DISTINCT District FROM Locations ORDER BY District",
            r => r.IsDBNull(0) ? "" : r.GetString(0));

        // Get all diseases
        var diseases = await _db.QueryAsync("SELECT Id, Name FROM Diseases ORDER BY Name",
            r => new { Id = r.GetInt32(0), Name = r.GetString(1) });

        ViewBag.Districts = districts;
        ViewBag.Diseases = diseases;

        return View();
    }

    // AJAX endpoint to get users by district
    public async Task<JsonResult> GetUsersByDistrict(string district)
    {
        string sql = @"
        SELECT u.Id, u.Name
        FROM Users u
        INNER JOIN Locations l ON u.Id = l.UserId
        WHERE l.District = @District
        ORDER BY u.Name";

        var users = await _db.QueryAsync(sql, r => new {
            Id = r.GetInt32(0),
            Name = r.GetString(1)
        }, new SqlParameter("@District", district));

        return Json(users);
    }


    // POST: Assign disease to user
    [HttpPost]
    public async Task<IActionResult> AssignDisease(int UserId, int DiseaseId)
    {
        string sql = "INSERT INTO DiseaseLogs (UserId, DiseaseId, ReportedAt) VALUES (@UserId, @DiseaseId, @ReportedAt)";
        await _db.ExecuteAsync(sql,
            new SqlParameter("@UserId", UserId),
            new SqlParameter("@DiseaseId", DiseaseId),
            new SqlParameter("@ReportedAt", DateTime.Now));

        TempData["Message"] = "Disease assigned successfully!";
        return RedirectToAction("AssignDisease");
    }


}
//public class UserWithLocation : User
//{
//    public string Road { get; set; } = "";
//    public string District { get; set; } = "";
//    public string Division { get; set; } = "";
//}