using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace PharmacyERP.Web.Models.Entities
{
    public class DoctorPrescription : BaseEntity
    {
        public string DoctorId { get; set; } = null!;
        public string DoctorName { get; set; } = null!;
        public string PatientName { get; set; } = null!;
        public string? PatientPhone { get; set; }
        
        public List<DoctorPrescriptionItem> Items { get; set; } = new();
        
        public string Status { get; set; } = "Pending"; // Pending | Dispensed | Cancelled
        
        public string? Remarks { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string? SaleId { get; set; }
    }

    public class DoctorPrescriptionItem
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string MedicineId { get; set; } = null!;
        public string MedicineName { get; set; } = null!;
        public int Qty { get; set; }
        public string? Instructions { get; set; }
    }
}
