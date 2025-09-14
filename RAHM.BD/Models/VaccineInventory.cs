namespace RAHM.BD.Models
{
    public class VaccineInventory
    {
        public int Id { get; set; }
        public int HealthCenterId { get; set; }
        public int VaccineId { get; set; }
        public int QuantityAvailable { get; set; }
    }
}
