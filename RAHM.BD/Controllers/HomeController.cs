using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
=======
using Microsoft.Data.SqlClient;
>>>>>>> iloveass-clean
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
        public IActionResult Ambulance()
        {
            ViewData["Title"] = "Ambulance Numbers";
            return View();
        }
<<<<<<< HEAD
=======
        //public IActionResult MedicineLookup()
        //{
        //    return View();
        //}

        // Add these API endpoints to HomeController
        [HttpGet("api/medication/diseases")]
        public async Task<IActionResult> GetMedicationDiseases()
        {
            var diseases = await _db.QueryAsync(
                @"SELECT DISTINCT d.Id, d.Name, COUNT(m.Id) as MedicationCount
              FROM Diseases d
              INNER JOIN Medications m ON d.Id = m.DiseaseId
              GROUP BY d.Id, d.Name
              ORDER BY d.Name",
                r => new {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    MedicationCount = r.GetInt32(2)
                }
            );
            return Json(diseases);
        }

        [HttpGet("api/medication/by-disease/{diseaseId}")]
        public async Task<IActionResult> GetMedicationsByDisease(int diseaseId)
        {
            var medications = await _db.QueryAsync(
                @"SELECT m.Id, m.MedName, d.Name as DiseaseName
              FROM Medications m
              INNER JOIN Diseases d ON m.DiseaseId = d.Id
              WHERE m.DiseaseId = @DiseaseId
              ORDER BY m.MedName",
                r => new
                {
                    Id = r.GetInt32(0),
                    MedName = r.GetString(1),
                    DiseaseName = r.GetString(2)
                },
                new SqlParameter("@DiseaseId", diseaseId)
            );
            return Json(medications);
        }

        [HttpGet("api/medication/disease-info/{diseaseId}")]
        public async Task<IActionResult> GetMedicationDiseaseInfo(int diseaseId)
        {
            var diseaseInfo = await _db.QuerySingleAsync(
                @"SELECT d.Id, d.Name, COUNT(m.Id) as MedicationCount
              FROM Diseases d
              LEFT JOIN Medications m ON d.Id = m.DiseaseId
              WHERE d.Id = @DiseaseId
              GROUP BY d.Id, d.Name",
                r => new
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    MedicationCount = r.GetInt32(2)
                },
                new SqlParameter("@DiseaseId", diseaseId)
            );
            return Json(diseaseInfo);
        }
>>>>>>> iloveass-clean

    }
}
