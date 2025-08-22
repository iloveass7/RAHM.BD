namespace RAHM.BD.Models
{
    public class HealthCenter
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Road { get; set; } = "";
        public string District { get; set; } = "";
        public string Division { get; set; } = "";
        public double Lat { get; set; }    // required for nearest-center queries
        public double Lng { get; set; }
    }
}
