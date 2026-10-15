namespace Armelle.Models;
public class PrescriptionDocument
{
 public int Id { get; set; }
 public int ConsultationId { get; set; }
 public string StoredFileName { get; set; } = "";
 public string OriginalFileName { get; set; } = "";
 public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
