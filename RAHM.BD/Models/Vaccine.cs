namespace RAHM.BD.Models
{
    public class Vaccine
    {
        public int Id { get; set; }
        public int DiseaseId { get; set; }
        public string Name { get; set; } = "";
    }
}
