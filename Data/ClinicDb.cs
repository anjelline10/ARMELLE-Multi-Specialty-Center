
using Armelle.Models;
using Microsoft.EntityFrameworkCore;

namespace Armelle.Data
{
    public class ClinicDb : DbContext
    {
        public ClinicDb(DbContextOptions<ClinicDb> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<PrescriptionDocument> PrescriptionDocuments => Set<PrescriptionDocument>();
        public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
        public DbSet<ClinicPromotion> ClinicPromotions => Set<ClinicPromotion>();
        public DbSet<PromotionAudit> PromotionAudits => Set<PromotionAudit>();
        public DbSet<PromotionRedemption> PromotionRedemptions => Set<PromotionRedemption>();
        public DbSet<PrescriptionRelease> PrescriptionReleases => Set<PrescriptionRelease>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();
        public DbSet<Consultation> Consultations => Set<Consultation>();
        public DbSet<LabRequest> LabRequests => Set<LabRequest>();
        public DbSet<Medicine> Medicines => Set<Medicine>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Payroll> Payrolls => Set<Payroll>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<AppUser> AppUsers => Set<AppUser>();

        public DbSet<ClinicService> ClinicServices
            => Set<ClinicService>();

        public DbSet<ClinicLocation> ClinicLocations
            => Set<ClinicLocation>();

        public DbSet<ResourceItem> ResourceItems
            => Set<ResourceItem>();

        public DbSet<SupportConversation> SupportConversations
            => Set<SupportConversation>();

        public DbSet<SupportMessage> SupportMessages
            => Set<SupportMessage>();

        public DbSet<SupportRating> SupportRatings
            => Set<SupportRating>();

        public DbSet<NurseAssignment> NurseAssignments
            => Set<NurseAssignment>();

        public DbSet<PharmacyOrder> PharmacyOrders
            => Set<PharmacyOrder>();

        public DbSet<Armelle.Models.OwnerProfile> OwnerProfiles
            => Set<Armelle.Models.OwnerProfile>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<PatientProfile>().HasIndex(x => x.AppUserId).IsUnique();
            modelBuilder.Entity<PatientProfile>().HasOne<AppUser>().WithOne()
                .HasForeignKey<PatientProfile>(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Armelle.Models.OwnerProfile>()
                .HasIndex(x => x.AppUserId)
                .IsUnique();

            modelBuilder.Entity<Armelle.Models.OwnerProfile>()
                .HasOne<AppUser>()
                .WithOne()
                .HasForeignKey<Armelle.Models.OwnerProfile>(
                    x => x.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
