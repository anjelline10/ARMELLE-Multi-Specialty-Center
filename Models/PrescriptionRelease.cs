using System;
namespace Armelle.Models;
public class PrescriptionRelease
{
 public int Id { get; set; }
 public int ConsultationId { get; set; }
 public string PhysicianName { get; set; } = "";
 public string LicenseNumber { get; set; } = "";
 public string RecordedBy { get; set; } = "";
 public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
 public decimal EstimatedMedicineTotal { get; set; }
 public string PriceBreakdown { get; set; } = "";
}
