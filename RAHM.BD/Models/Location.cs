namespace RAHM.BD.Models
{
    public class Location
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Road { get; set; } = "";
        public string District { get; set; } = "";
        public string Division { get; set; } = "";
        public double? Lat { get; set; }   // nullable, set via geocoding or map pin
        public double? Lng { get; set; }
    }
}
