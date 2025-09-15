using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RAHM.BD.Models;
using RAHM.BD.Services;
using System.Data;
using System.Threading.Tasks;

namespace RAHM.BD.Controllers
{
    public class VaccineController : Controller
    {
        private readonly IDb _db;

        public VaccineController(IDb db)
        {
            _db = db;
        }

        // ✅ Get diseases that have vaccines available
        [HttpGet("/Vaccine/Diseases")]
        public async Task<IActionResult> GetDiseases()
        {
            var diseases = await _db.QueryAsync(
                @"SELECT DISTINCT d.Id, d.Name
                  FROM Diseases d
                  JOIN Vaccines v ON d.Id = v.DiseaseId
                  JOIN VaccineInventories i ON v.Id = i.VaccineId
                  WHERE i.QuantityAvailable > 0
                  ORDER BY d.Name",
                r => new {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1)
                }
            );
            return Json(diseases);
        }

        // ✅ Get vaccines and healthcare centers for a disease
        [HttpGet("/Vaccine/ByDisease/{diseaseId}")]
        public async Task<IActionResult> ByDisease(int diseaseId)
        {
            var vaccineData = await _db.QueryAsync(
                @"SELECT v.Id as VaccineId, v.Name as VaccineName,
                         c.Id as CenterId, c.Name as CenterName, 
                         c.District, c.Division, i.QuantityAvailable
                  FROM Vaccines v
                  JOIN VaccineInventories i ON v.Id = i.VaccineId
                  JOIN HealthCenters c ON i.HealthCenterId = c.Id
                  WHERE v.DiseaseId = @DiseaseId AND i.QuantityAvailable > 0
                  ORDER BY v.Name, c.Name",
                r => new
                {
                    VaccineId = r.GetInt32(0),
                    VaccineName = r.GetString(1),
                    CenterId = r.GetInt32(2),
                    CenterName = r.GetString(3),
                    District = r.IsDBNull(4) ? "" : r.GetString(4),
                    Division = r.IsDBNull(5) ? "" : r.GetString(5),
                    QuantityAvailable = r.GetInt32(6)
                },
                new SqlParameter("@DiseaseId", diseaseId)
            );

            var result = vaccineData
                .GroupBy(x => new { x.VaccineId, x.VaccineName })
                .Select(vaccineGroup => new
                {
                    VaccineId = vaccineGroup.Key.VaccineId,
                    VaccineName = vaccineGroup.Key.VaccineName,
                    Centers = vaccineGroup.Select(x => new
                    {
                        CenterId = x.CenterId,
                        CenterName = x.CenterName,
                        Location = $"{x.District}, {x.Division}".Trim(' ', ','),
                        QuantityAvailable = x.QuantityAvailable
                    }).ToList()
                }).ToList();

            return Json(result);
        }

        // ✅ SINGLE Take vaccine method - REMOVE ALL OTHER DUPLICATES
        [HttpPost("/Vaccine/Take")]
        public async Task<IActionResult> Take([FromBody] VaccinationRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Invalid request data" });
                }

                // Create connection for transaction
                using var connection = new SqlConnection(_db.GetConnectionString());
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Check if user already received this vaccine recently
                    using var checkCmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM VaccinationLogs 
                          WHERE UserId = @UserId AND VaccineId = @VaccineId 
                          AND VaccinatedAt > DATEADD(month, -6, GETDATE())",
                        connection, transaction);
                    checkCmd.Parameters.AddWithValue("@UserId", request.UserId);
                    checkCmd.Parameters.AddWithValue("@VaccineId", request.VaccineId);

                    var recentCount = (int)await checkCmd.ExecuteScalarAsync();
                    if (recentCount > 0)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { success = false, message = "You have already received this vaccine recently" });
                    }

                    // Get current stock and vaccine info
                    using var stockCmd = new SqlCommand(
                        @"SELECT i.QuantityAvailable, c.Name as CenterName, v.Name as VaccineName
                          FROM VaccineInventories i
                          JOIN HealthCenters c ON i.HealthCenterId = c.Id
                          JOIN Vaccines v ON i.VaccineId = v.Id
                          WHERE i.VaccineId = @VaccineId AND i.HealthCenterId = @CenterId",
                        connection, transaction);
                    stockCmd.Parameters.AddWithValue("@VaccineId", request.VaccineId);
                    stockCmd.Parameters.AddWithValue("@CenterId", request.HealthCenterId);

                    using var reader = await stockCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        await reader.CloseAsync();
                        await transaction.RollbackAsync();
                        return BadRequest(new { success = false, message = "Vaccine not found at this center" });
                    }

                    var currentStock = reader.GetInt32("QuantityAvailable");
                    var centerName = reader.GetString("CenterName");
                    var vaccineName = reader.GetString("VaccineName");
                    await reader.CloseAsync();

                    if (currentStock <= 0)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { success = false, message = $"{vaccineName} is out of stock at {centerName}" });
                    }

                    // Update inventory (decrease by 1)
                    using var updateCmd = new SqlCommand(
                        @"UPDATE VaccineInventories 
                          SET QuantityAvailable = QuantityAvailable - 1
                          WHERE VaccineId = @VaccineId AND HealthCenterId = @CenterId AND QuantityAvailable > 0",
                        connection, transaction);
                    updateCmd.Parameters.AddWithValue("@VaccineId", request.VaccineId);
                    updateCmd.Parameters.AddWithValue("@CenterId", request.HealthCenterId);

                    var updateResult = await updateCmd.ExecuteNonQueryAsync();
                    if (updateResult == 0)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { success = false, message = "Failed to update vaccine inventory" });
                    }

                    // Insert vaccination log
                    using var logCmd = new SqlCommand(
                        @"INSERT INTO VaccinationLogs (UserId, VaccineId, HealthCenterId, VaccinatedAt)
                          VALUES (@UserId, @VaccineId, @CenterId, @Now)",
                        connection, transaction);
                    logCmd.Parameters.AddWithValue("@UserId", request.UserId);
                    logCmd.Parameters.AddWithValue("@VaccineId", request.VaccineId);
                    logCmd.Parameters.AddWithValue("@CenterId", request.HealthCenterId);
                    logCmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);

                    var logResult = await logCmd.ExecuteNonQueryAsync();
                    if (logResult == 0)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { success = false, message = "Failed to record vaccination" });
                    }

                    // Commit transaction
                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"✅ {vaccineName} vaccination recorded successfully at {centerName}!",
                        remainingStock = currentStock - 1
                    });
                }
                catch (Exception innerEx)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "Database operation failed" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request" });
            }
        }
    }

    public class VaccinationRequest
    {
        public int UserId { get; set; }
        public int VaccineId { get; set; }
        public int HealthCenterId { get; set; }
    }
}
