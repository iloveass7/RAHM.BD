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
    private bool IsAdminLoggedIn()
    {
        return HttpContext.Session.GetString("IsAdmin") == "true";
    }

    private IActionResult RedirectToLogin()
    {
        return RedirectToAction("Login", "Admin");
    }


    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        if (username == "admin" && password == "123") // placeholder
        {
            HttpContext.Session.SetString("IsAdmin", "true"); // ✅ Save session
            //TempData["Msg"] = "Welcome Admin!";
            return RedirectToAction("Index");
        }

        ViewBag.Error = "Invalid credentials.";
        return View();
    }


    public IActionResult Index()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();
        ViewData["Title"] = "Admin Dashboard";
        return View();
    }

    //public IActionResult Users()
    //{
    //    if (!IsAdminLoggedIn()) return RedirectToLogin();
    //    ViewData["Title"] = "View Users";
    //    return View();
    //}

    //public IActionResult Vaccine()
    //{
    //    if (!IsAdminLoggedIn()) return RedirectToLogin();
    //    ViewData["Title"] = "Vaccine Management";
    //    return View();
    //}

    public IActionResult SendSms()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();
        ViewData["Title"] = "Send SMS";
        return View();
    }

    public IActionResult SendMail()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();
        ViewData["Title"] = "Send Mail";
        return View();
    }

    public IActionResult UploadContent()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();
        ViewData["Title"] = "Upload Content";
        return View();
    }

    //public IActionResult Medicine()
    //{
    //    if (!IsAdminLoggedIn()) return RedirectToLogin();
    //    ViewData["Title"] = "Medicine Management";
    //    return View();
    //}

    //public IActionResult Disease()
    //{
    //    if (!IsAdminLoggedIn()) return RedirectToLogin();
    //    ViewData["Title"] = "Disease Management";
    //    return View();
    //}

    // ===============================
    // 🚑 Healthcare Center Management
    // ===============================

    // GET: /Admin/HealthcareCenter
    public async Task<IActionResult> HealthcareCenter()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
        if (!IsAdminLoggedIn()) return RedirectToLogin();
        string sql = "DELETE FROM HealthCenters WHERE Id=@Id";
        await _db.ExecuteAsync(sql, new SqlParameter("@Id", id));

        TempData["Message"] = "Healthcare center deleted successfully!";
        return RedirectToAction("HealthcareCenter");
    }

    // GET: /Admin/Users
    public async Task<IActionResult> Users()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
        if (!IsAdminLoggedIn()) return RedirectToLogin();
        await _db.ExecuteAsync("DELETE FROM Locations WHERE UserId=@Id",
            new SqlParameter("@Id", id));
        await _db.ExecuteAsync("DELETE FROM Users WHERE Id=@Id",
            new SqlParameter("@Id", id));

        return RedirectToAction("Users");
    }
    // GET: /Admin/Disease
    public async Task<IActionResult> Disease()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
        if (!IsAdminLoggedIn()) return RedirectToLogin();
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
    // GET: /Admin/Medicine
    public async Task<IActionResult> Medicine()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();
        // Get all diseases from DB
        var diseases = await _db.QueryAsync(
            "SELECT Id, Name FROM Diseases ORDER BY Name",
            r => new { Id = r.GetInt32(0), Name = r.GetString(1) }
        );

        // Get all medicines with their disease name
        string sql = @"
        SELECT m.Id, d.Name AS DiseaseName, m.MedName
        FROM Medications m
        INNER JOIN Diseases d ON m.DiseaseId = d.Id
        ORDER BY d.Name, m.MedName";

        var meds = await _db.QueryAsync(sql, r => new
        {
            Id = r.GetInt32(0),
            DiseaseName = r.GetString(1),
            MedName = r.GetString(2)
        });

        ViewBag.Diseases = diseases;   // pass diseases to dropdown
        return View(meds);             // pass meds list to table
    }

    // POST: /Admin/AddMedicine
    [HttpPost]
    public async Task<IActionResult> AddMedicine(int DiseaseId, string MedName)
    {
        if (DiseaseId == 0 || string.IsNullOrWhiteSpace(MedName))
        {
            TempData["Error"] = "Please select a disease and enter a medicine name.";
            return RedirectToAction("Medicine");
        }

        string sql = "INSERT INTO Medications (DiseaseId, MedName) VALUES (@DiseaseId, @MedName)";
        await _db.ExecuteAsync(sql,
            new SqlParameter("@DiseaseId", DiseaseId),
            new SqlParameter("@MedName", MedName)
        );

        TempData["Message"] = "Medicine added successfully!";
        return RedirectToAction("Medicine");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMedicine(int Id)
    {
        await _db.ExecuteAsync("DELETE FROM Medications WHERE Id=@Id", new SqlParameter("@Id", Id));
        TempData["Message"] = "Medicine deleted successfully!";
        return RedirectToAction("Medicine");
    }
    // GET: /Admin/Vaccine
    public async Task<IActionResult> Vaccine()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();
        // Get all diseases
        var diseases = await _db.QueryAsync(
            "SELECT Id, Name FROM Diseases ORDER BY Name",
            r => new { Id = r.GetInt32(0), Name = r.GetString(1) }
        );

        // Get all healthcare centers
        var centers = await _db.QueryAsync(
            "SELECT Id, Name, District, Division FROM HealthCenters ORDER BY Name",
            r => new { Id = r.GetInt32(0), Name = r.GetString(1), District = r.GetString(2), Division = r.GetString(3) }
        );

        // Get vaccine inventory list (join across tables)
        string sql = @"
        SELECT vi.Id, v.Name AS VaccineName, d.Name AS DiseaseName,
               hc.Name AS CenterName, vi.QuantityAvailable
        FROM VaccineInventories vi
        INNER JOIN Vaccines v ON vi.VaccineId = v.Id
        INNER JOIN Diseases d ON v.DiseaseId = d.Id
        INNER JOIN HealthCenters hc ON vi.HealthCenterId = hc.Id
        ORDER BY v.Name";

        var inventory = await _db.QueryAsync(sql, r => new
        {
            Id = r.GetInt32(0),
            VaccineName = r.GetString(1),
            DiseaseName = r.GetString(2),
            CenterName = r.GetString(3),
            QuantityAvailable = r.GetInt32(4)
        });

        ViewBag.Diseases = diseases;
        ViewBag.Centers = centers;

        return View(inventory); // Vaccine.cshtml
    }

    // POST: /Admin/AddVaccine
    [HttpPost]
    public async Task<IActionResult> AddVaccine(int DiseaseId, string VaccineName, int HealthCenterId, int QuantityAvailable)
    {
        if (DiseaseId == 0 || string.IsNullOrWhiteSpace(VaccineName) || HealthCenterId == 0)
        {
            TempData["Error"] = "All fields are required.";
            return RedirectToAction("Vaccine");
        }

        // Insert vaccine
        string insertVaccineSql = "INSERT INTO Vaccines (DiseaseId, Name) OUTPUT INSERTED.Id VALUES (@DiseaseId, @Name)";
        int vaccineId = await _db.QuerySingleAsync(insertVaccineSql, r => r.GetInt32(0),
            new SqlParameter("@DiseaseId", DiseaseId),
            new SqlParameter("@Name", VaccineName)
        );

        // Insert into inventory
        string insertInventorySql = "INSERT INTO VaccineInventories (HealthCenterId, VaccineId, QuantityAvailable) VALUES (@HealthCenterId, @VaccineId, @Quantity)";
        await _db.ExecuteAsync(insertInventorySql,
            new SqlParameter("@HealthCenterId", HealthCenterId),
            new SqlParameter("@VaccineId", vaccineId),
            new SqlParameter("@Quantity", QuantityAvailable)
        );

        TempData["Message"] = "Vaccine added successfully!";
        return RedirectToAction("Vaccine");
    }

}
//public class UserWithLocation : User
//{
//    public string Road { get; set; } = "";
//    public string District { get; set; } = "";
//    public string Division { get; set; } = "";
//}