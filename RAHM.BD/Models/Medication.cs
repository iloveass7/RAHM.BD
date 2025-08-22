namespace RAHM.BD.Models
{
    public class Medication
    {
        public int Id { get; set; }
        public int DiseaseId { get; set; }
        public string MedName { get; set; } = "";
    }
}
