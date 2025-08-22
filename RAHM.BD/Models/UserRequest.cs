namespace RAHM.BD.Models
{
    // 1=NearbyCenter, 2=HealthTip, 3=VaccineAvailability
    // 0=Pending, 1=Done, 2=Rejected
    public class UserRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Type { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Only when relevant to the request:
        public int? DiseaseId { get; set; }
        public int? VaccineId { get; set; }
        public int? HealthCenterId { get; set; }
    }
}
