namespace RAHM.BD.Models
{
    public class DiseaseLog
    {
        public int Id { get; set; }
        public int DiseaseId { get; set; }
        public int UserId { get; set; }
        public DateTime ReportedAt { get; set; }
    }
}
