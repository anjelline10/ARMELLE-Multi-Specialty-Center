using System;
using System.ComponentModel.DataAnnotations;

namespace Armelle.Models;

public class PatientProfile
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    [MaxLength(120)] public string FullName { get; set; } = "";
    public DateTime? BirthDate { get; set; }
    [MaxLength(40)] public string Sex { get; set; } = "";
    [MaxLength(20)] public string Contact { get; set; } = "";
    [MaxLength(160)] public string Email { get; set; } = "";
    [MaxLength(350)] public string Address { get; set; } = "";
    [MaxLength(120)] public string EmergencyContactName { get; set; } = "";
    [MaxLength(70)] public string EmergencyRelationship { get; set; } = "";
    [MaxLength(20)] public string EmergencyContactNumber { get; set; } = "";
    [MaxLength(120)] public string Hmo { get; set; } = "";
    [MaxLength(80)] public string HmoMemberNumber { get; set; } = "";
    [MaxLength(300)] public string PhotoPath { get; set; } = "";
}
