
using Armelle.Data;
using Armelle.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<GroqAiService>();
builder.Services.AddScoped<AiService>();
builder.Services.AddSingleton<GalleryStore>();
builder.Services.AddSingleton<PublicPhotoStore>();

builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Missing DefaultConnection.");

builder.Services.AddDbContext<ClinicDb>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicDb>();

    // Automatically create missing additive tables in the existing MySQL database.
    // Does not delete patient data; existing tables are left untouched.
    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS `PatientProfiles` (
 `Id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
 `AppUserId` INT NOT NULL,
 `FullName` VARCHAR(120) NOT NULL DEFAULT '',
 `BirthDate` DATETIME(6) NULL,
 `Sex` VARCHAR(40) NOT NULL DEFAULT '',
 `Contact` VARCHAR(20) NOT NULL DEFAULT '',
 `Email` VARCHAR(160) NOT NULL DEFAULT '',
 `Address` VARCHAR(350) NOT NULL DEFAULT '',
 `EmergencyContactName` VARCHAR(120) NOT NULL DEFAULT '',
 `EmergencyRelationship` VARCHAR(70) NOT NULL DEFAULT '',
 `EmergencyContactNumber` VARCHAR(20) NOT NULL DEFAULT '',
 `Hmo` VARCHAR(120) NOT NULL DEFAULT '',
 `HmoMemberNumber` VARCHAR(80) NOT NULL DEFAULT '',
 `PhotoPath` VARCHAR(300) NOT NULL DEFAULT '',
 UNIQUE KEY `IX_PatientProfiles_AppUserId` (`AppUserId`),
 CONSTRAINT `FK_PatientProfiles_AppUsers_AppUserId` FOREIGN KEY (`AppUserId`) REFERENCES `AppUsers` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

    db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS `PrescriptionDocuments` (
 `Id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
 `ConsultationId` INT NOT NULL,
 `StoredFileName` VARCHAR(255) NOT NULL DEFAULT '',
 `OriginalFileName` VARCHAR(255) NOT NULL DEFAULT '',
 `UploadedAtUtc` DATETIME(6) NOT NULL,
 INDEX `IX_PrescriptionDocuments_ConsultationId` (`ConsultationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");


    // Repair legacy NULL values before EF Core materializes non-nullable string properties.
    // Preserve all records and replace only missing string values.
    db.Database.ExecuteSqlRaw(@"UPDATE `Patients` SET
      `Name` = COALESCE(`Name`, ''),
      `Sex` = COALESCE(`Sex`, ''),
      `Contact` = COALESCE(`Contact`, ''),
      `Address` = COALESCE(`Address`, ''),
      `Hmo` = COALESCE(`Hmo`, ''),
      `Branch` = COALESCE(`Branch`, 'North QC')");

    db.Database.ExecuteSqlRaw(@"UPDATE `LabRequests` SET
      `Patient` = COALESCE(`Patient`, ''),
      `Test` = COALESCE(`Test`, ''),
      `Result` = COALESCE(`Result`, ''),
      `Status` = COALESCE(`Status`, 'Pending')");

    OwnerSchemaUpgrade.Apply(db);

    OwnerAccountSetup.Apply(db, app.Configuration);

    // Temporary: use only for a one-time password reset.
    // Remove after successful setup.
    OwnerPasswordReset.Apply(db, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Public/Index");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Public}/{action=Index}/{id?}");

app.Run();
