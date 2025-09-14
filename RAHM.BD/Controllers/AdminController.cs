using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RAHM.BD.Models;
using RAHM.BD.Services;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;


public class AdminController : Controller
{
    private readonly IDb _db;
    private readonly INotificationService _notificationService;
    public AdminController(IDb db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
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


    //public IActionResult Index()
    //{
    //    if (!IsAdminLoggedIn()) return RedirectToLogin();
    //    return RedirectToAction("Dashboard");
    //}

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

    //public IActionResult SendSms()
    //{
    //    if (!IsAdminLoggedIn()) return RedirectToLogin();
    //    ViewData["Title"] = "Send SMS";
    //    return View();
    //}

    //public IActionResult SendMail()
    //{
    //    if (!IsAdminLoggedIn()) return RedirectToLogin();
    //    ViewData["Title"] = "Send Mail";
    //    return View();
    //}

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
    ///////////eta neew
    ///
    // GET: /Admin/SendSms
    public async Task<IActionResult> SendSms()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();

        // Get available districts and divisions for dropdown
        await LoadLocationDataAsync();

        ViewData["Title"] = "Send SMS";
        return View();
    }

    // POST: /Admin/SendSms
    [HttpPost]
    public async Task<IActionResult> SendSms(string message, string targetType, string? division, string? district)
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();

        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Message is required";
            await LoadLocationDataAsync();
            return View();
        }

        var request = new NotificationRequest
        {
            Message = message,
            Channel = NotificationChannel.SMS,
            SendToAll = targetType == "all",
            Division = targetType == "division" ? division : null,
            District = targetType == "district" ? district : null
        };

        var result = await _notificationService.SendBulkNotificationAsync(request);

        if (result.IsSuccess)
        {
            TempData["Message"] = $"SMS sent successfully to {result.SuccessCount} users!";
        }
        else
        {
            TempData["Error"] = $"SMS partially sent: {result.SuccessCount} successful, {result.FailureCount} failed";
        }

        return RedirectToAction("SendSms");
    }

    // GET: /Admin/SendMail
    public async Task<IActionResult> SendMail()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();

        await LoadLocationDataAsync();

        ViewData["Title"] = "Send Mail";
        return View();
    }

    // POST: /Admin/SendMail
    [HttpPost]
    public async Task<IActionResult> SendMail(string subject, string message, string targetType, string? division, string? district)
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Subject and message are required";
            await LoadLocationDataAsync();
            return View();
        }

        var request = new NotificationRequest
        {
            Title = subject,
            Message = message,
            Channel = NotificationChannel.Email,
            SendToAll = targetType == "all",
            Division = targetType == "division" ? division : null,
            District = targetType == "district" ? district : null
        };

        var result = await _notificationService.SendBulkNotificationAsync(request);

        if (result.IsSuccess)
        {
            TempData["Message"] = $"Email sent successfully to {result.SuccessCount} users!";
        }
        else
        {
            TempData["Error"] = $"Email partially sent: {result.SuccessCount} successful, {result.FailureCount} failed";
        }

        return RedirectToAction("SendMail");
    }

    // GET: /Admin/SendNotification
    public async Task<IActionResult> SendNotification()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();

        await LoadLocationDataAsync();

        ViewData["Title"] = "Send Notification";
        return View();
    }

    // POST: /Admin/SendNotification - Send both SMS and Email
    [HttpPost]
    public async Task<IActionResult> SendNotification(string subject, string message, string targetType, string? division, string? district)
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Subject and message are required";
            await LoadLocationDataAsync();
            return View();
        }

        var request = new NotificationRequest
        {
            Title = subject,
            Message = message,
            Channel = NotificationChannel.Both,
            SendToAll = targetType == "all",
            Division = targetType == "division" ? division : null,
            District = targetType == "district" ? district : null
        };

        var result = await _notificationService.SendBulkNotificationAsync(request);

        if (result.IsSuccess)
        {
            TempData["Message"] = $"Notifications sent successfully to {result.SuccessCount} users!";
        }
        else
        {
            TempData["Error"] = $"Notifications partially sent: {result.SuccessCount} successful, {result.FailureCount} failed";
        }

        return RedirectToAction("SendNotification");
    }

    // Helper method to load districts and divisions
    private async Task LoadLocationDataAsync()
    {
        var divisions = await _db.QueryAsync("SELECT DISTINCT Division FROM Locations WHERE Division IS NOT NULL AND Division != '' ORDER BY Division",
            r => r.GetString(0));

        var districts = await _db.QueryAsync("SELECT DISTINCT District FROM Locations WHERE District IS NOT NULL AND District != '' ORDER BY District",
            r => r.GetString(0));

        ViewBag.Divisions = divisions;
        ViewBag.Districts = districts;
    }

    // AJAX endpoint to get districts by division
    [HttpGet]
    public async Task<JsonResult> GetDistrictsByDivision(string division)
    {
        var districts = await _db.QueryAsync(
            "SELECT DISTINCT District FROM Locations WHERE Division = @Division AND District IS NOT NULL AND District != '' ORDER BY District",
            r => r.GetString(0),
            new SqlParameter("@Division", division));

        return Json(districts);
    }

    // GET: /Admin/NotificationHistory
    public async Task<IActionResult> NotificationHistory()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();

        // You can implement this to show notification history if needed
        ViewData["Title"] = "Notification History";
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> VaccinationLogs()
    {
        var logs = await _db.QueryAsync(
            @"SELECT 
            vl.Id,
            u.Name as UserName,
            u.Email as UserEmail,
            u.MobileNo,
            v.Name as VaccineName,
            d.Name as DiseaseName,
            hc.Name as HealthCenterName,
            hc.District,
            hc.Division,
            vl.VaccinatedAt,
            DATEDIFF(day, vl.VaccinatedAt, GETDATE()) as DaysAgo
          FROM VaccinationLogs vl
          INNER JOIN Users u ON vl.UserId = u.Id
          INNER JOIN Vaccines v ON vl.VaccineId = v.Id
          INNER JOIN Diseases d ON v.DiseaseId = d.Id
          INNER JOIN HealthCenters hc ON vl.HealthCenterId = hc.Id
          ORDER BY vl.VaccinatedAt DESC",
            r => new
            {
                Id = r.GetInt32("Id"),
                UserName = r.GetString("UserName"),
                UserEmail = r.GetString("UserEmail"),
                MobileNo = r.GetString("MobileNo"),
                VaccineName = r.GetString("VaccineName"),
                DiseaseName = r.GetString("DiseaseName"),
                HealthCenterName = r.GetString("HealthCenterName"),
                District = r.GetString("District"),
                Division = r.GetString("Division"),
                VaccinatedAt = r.GetDateTime("VaccinatedAt"),
                DaysAgo = r.GetInt32("DaysAgo")
            }
        );

        return View(logs);
    }
    public async Task<IActionResult> Index()
    {
        if (!IsAdminLoggedIn()) return RedirectToLogin();

        try
        {
            // Basic counts
            var totalUsers = await _db.QuerySingleAsync(
                "SELECT COUNT(*) FROM Users",
                r => r.GetInt32(0)
            );

            var totalVaccines = await _db.QuerySingleAsync(
                "SELECT COUNT(*) FROM Vaccines",
                r => r.GetInt32(0)
            );

            var totalHealthCenters = await _db.QuerySingleAsync(
                "SELECT COUNT(*) FROM HealthCenters",
                r => r.GetInt32(0)
            );

            var totalVaccinations = await _db.QuerySingleAsync(
                "SELECT COUNT(*) FROM VaccinationLogs",
                r => r.GetInt32(0)
            );

            var todayVaccinations = await _db.QuerySingleAsync(
                "SELECT COUNT(*) FROM VaccinationLogs WHERE CAST(VaccinatedAt AS DATE) = CAST(GETDATE() AS DATE)",
                r => r.GetInt32(0)
            );

            var outOfStockCount = await _db.QuerySingleAsync(
                "SELECT COUNT(*) FROM VaccineInventories WHERE QuantityAvailable = 0",
                r => r.GetInt32(0)
            );

            // Recent vaccination activities
            var recentVaccinations = await _db.QueryAsync(
                @"SELECT TOP 10 
                u.Name as UserName, v.Name as VaccineName, 
                hc.Name as CenterName, vl.VaccinatedAt,
                d.Name as DiseaseName
              FROM VaccinationLogs vl
              JOIN Users u ON vl.UserId = u.Id
              JOIN Vaccines v ON vl.VaccineId = v.Id
              JOIN HealthCenters hc ON vl.HealthCenterId = hc.Id
              JOIN Diseases d ON v.DiseaseId = d.Id
              ORDER BY vl.VaccinatedAt DESC",
                r => new
                {
                    UserName = r.GetString(0),
                    VaccineName = r.GetString(1),
                    CenterName = r.GetString(2),
                    VaccinatedAt = r.GetDateTime(3),
                    DiseaseName = r.GetString(4)
                }
            );

            // Stock alerts (vaccines with low stock)
            var lowStockAlerts = await _db.QueryAsync(
                @"SELECT v.Name as VaccineName, hc.Name as CenterName, 
                     vi.QuantityAvailable, d.Name as DiseaseName
              FROM VaccineInventories vi
              JOIN Vaccines v ON vi.VaccineId = v.Id
              JOIN HealthCenters hc ON vi.HealthCenterId = hc.Id
              JOIN Diseases d ON v.DiseaseId = d.Id
              WHERE vi.QuantityAvailable <= 10 AND vi.QuantityAvailable > 0
              ORDER BY vi.QuantityAvailable ASC",
                r => new
                {
                    VaccineName = r.GetString(0),
                    CenterName = r.GetString(1),
                    QuantityAvailable = r.GetInt32(2),
                    DiseaseName = r.GetString(3)
                }
            );

            // Top vaccines by usage
            var topVaccines = await _db.QueryAsync(
                @"SELECT TOP 5 v.Name, d.Name as DiseaseName, COUNT(vl.Id) as TimesUsed
              FROM VaccinationLogs vl
              JOIN Vaccines v ON vl.VaccineId = v.Id
              JOIN Diseases d ON v.DiseaseId = d.Id
              GROUP BY v.Name, d.Name
              ORDER BY TimesUsed DESC",
                r => new
                {
                    VaccineName = r.GetString(0),
                    DiseaseName = r.GetString(1),
                    TimesUsed = r.GetInt32(2)
                }
            );

            // Vaccination by region
            var vaccinationsByRegion = await _db.QueryAsync(
                @"SELECT hc.Division, COUNT(vl.Id) as VaccinationCount
              FROM VaccinationLogs vl
              JOIN HealthCenters hc ON vl.HealthCenterId = hc.Id
              GROUP BY hc.Division
              ORDER BY VaccinationCount DESC",
                r => new
                {
                    Division = r.GetString(0),
                    VaccinationCount = r.GetInt32(1)
                }
            );

            // Daily vaccinations for the last 7 days
            var dailyVaccinations = await _db.QueryAsync(
                @"SELECT CAST(vl.VaccinatedAt AS DATE) as VaccinationDate, 
                     COUNT(*) as DailyCount
              FROM VaccinationLogs vl
              WHERE vl.VaccinatedAt >= DATEADD(day, -7, GETDATE())
              GROUP BY CAST(vl.VaccinatedAt AS DATE)
              ORDER BY VaccinationDate DESC",
                r => new
                {
                    VaccinationDate = r.GetDateTime(0),
                    DailyCount = r.GetInt32(1)
                }
            );

            // Create view model
            var dashboardData = new
            {
                TotalUsers = totalUsers,
                TotalVaccines = totalVaccines,
                TotalHealthCenters = totalHealthCenters,
                TotalVaccinations = totalVaccinations,
                TodayVaccinations = todayVaccinations,
                OutOfStockCount = outOfStockCount,
                LowStockAlerts = lowStockAlerts,
                RecentVaccinations = recentVaccinations,
                VaccinationsByRegion = vaccinationsByRegion,
                TopVaccines = topVaccines,
                DailyVaccinations = dailyVaccinations
            };

            ViewData["Title"] = "Admin Dashboard";
            return View(dashboardData);
        }
        catch (Exception ex)
        {
            // Log error
            var emptyData = new
            {
                TotalUsers = 0,
                TotalVaccines = 0,
                TotalHealthCenters = 0,
                TotalVaccinations = 0,
                TodayVaccinations = 0,
                OutOfStockCount = 0,
                LowStockAlerts = new List<dynamic>(),
                RecentVaccinations = new List<dynamic>(),
                VaccinationsByRegion = new List<dynamic>(),
                TopVaccines = new List<dynamic>(),
                DailyVaccinations = new List<dynamic>()
            };
            return View(emptyData);
        }
    }

}
//public class UserWithLocation : User
//{
//    public string Road { get; set; } = "";
//    public string District { get; set; } = "";
//    public string Division { get; set; } = "";
//}