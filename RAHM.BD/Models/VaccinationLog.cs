namespace RAHM.BD.Models
{
    public class VaccinationLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int VaccineId { get; set; }
        public int HealthCenterId { get; set; }
        public DateTime VaccinatedAt { get; set; }
    }
}
