
using System;
using System.ComponentModel.DataAnnotations;

namespace Armelle.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public DateTime? BirthDate { get; set; }
        public string Sex { get; set; } = "";
        public string Contact { get; set; } = "";
        public string Address { get; set; } = "";
        public string Hmo { get; set; } = "";
        public string Branch { get; set; } = "North QC";
    }

    public class Doctor
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string Specialty { get; set; } = "";
        public string Branch { get; set; } = "";
        public string Room { get; set; } = "";
        public string Schedule { get; set; } = "";
        public int DailyLimit { get; set; } = 12;
        public decimal ConsultationFee { get; set; } = 650;
        public string Status { get; set; } = "Available";
        public string PhotoPath { get; set; } = "";
        public string Credentials { get; set; } = "";
        public string Bio { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string Patient { get; set; } = "";

        public string Branch { get; set; } = "";
        public string Doctor { get; set; } = "";
        public string Specialty { get; set; } = "";
        public string Service { get; set; } = "";

        public DateTime Schedule { get; set; } = DateTime.Now;

        public DateTime EndTime { get; set; } =
            DateTime.Now.AddMinutes(30);

        public string Type { get; set; } = "Appointment";
        public string Status { get; set; } = "Reserved";
        public string PaymentStatus { get; set; } = "Pay at Clinic";
        public string ProofPath { get; set; } = "";
        public string ReferenceNo { get; set; } = "";
    }

    public class QueueEntry
    {
        public int Id { get; set; }
        public string QueueNo { get; set; } = "";
        public string Patient { get; set; } = "";
        public string Department { get; set; } = "";
        public string Status { get; set; } = "Waiting";
        public DateTime CheckInAt { get; set; } = DateTime.Now;
    }

    public class Consultation
    {
        public int Id { get; set; }
        public string Patient { get; set; } = "";
        public string Doctor { get; set; } = "";
        public string Vitals { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Diagnosis { get; set; } = "";
        public string Prescription { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class LabRequest
    {
        public int Id { get; set; }
        public string Patient { get; set; } = "";
        public string Test { get; set; } = "";
        public string Result { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime Requested { get; set; } = DateTime.Now;
    }

    public class Medicine
    {
        public int Id { get; set; }
        public string GenericName { get; set; } = "";
        public string Brand { get; set; } = "";
        public string Category { get; set; } = "";
        public string Strength { get; set; } = "";
        public string Branch { get; set; } = "";
        public int Stock { get; set; }
        public int ReservedStock { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public string BatchNo { get; set; } = "";
        public string Supplier { get; set; } = "";
        public bool PrescriptionRequired { get; set; }
        public DateTime? Expiry { get; set; }
    }

    public class Invoice
    {
        public int Id { get; set; }
        public string Patient { get; set; } = "";
        public string Service { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Discount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string Status { get; set; } = "Pending";
        public string ProofPath { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public string Department { get; set; } = "";
        public string Branch { get; set; } = "";
        public string Shift { get; set; } = "8:00 AM - 5:00 PM";
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public decimal Rate { get; set; }
        public string PhotoPath { get; set; } = "";
        public string Email { get; set; } = "";
        public string Contact { get; set; } = "";
        public string Bio { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    public class Payroll
    {
        public int Id { get; set; }
        public string Employee { get; set; } = "";
        public decimal RegularPay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetPay { get; set; }
        public string Status { get; set; } = "For Approval";
    }

    public class Expense
    {
        public int Id { get; set; }
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
    }

    public class AppUser
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        [Required]
        [MaxLength(30)]
        public string Role { get; set; } = "Patient";
    }

    public class NurseAssignment
    {
        public int Id { get; set; }
        public string Doctor { get; set; } = "";
        public string Nurse { get; set; } = "";
        public string Branch { get; set; } = "";
        public string Room { get; set; } = "";
        public DateTime DutyDate { get; set; } = DateTime.Today;
        public string Shift { get; set; } = "";
        public string AssignmentRole { get; set; } = "Clinical Support";
    }

    public class PharmacyOrder
    {
        public int Id { get; set; }
        public string Patient { get; set; } = "";
        public string Branch { get; set; } = "";
        public string PrescriptionPath { get; set; } = "";
        public string ExtractedItems { get; set; } = "";
        public decimal Total { get; set; }
        public string Status { get; set; } = "Under Review";
        public string PaymentStatus { get; set; } = "Pending";
        public string PaymentProofPath { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
