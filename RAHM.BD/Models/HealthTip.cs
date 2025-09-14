namespace RAHM.BD.Models
{
    public class HealthTip
    {
        public int Id { get; set; }
        public int DiseaseId { get; set; }
        public string TipText { get; set; } = "";
    }
}
