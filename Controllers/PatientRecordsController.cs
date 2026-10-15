using System.Net.Http.Json;
using Armelle.Data;
using Armelle.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Armelle.Controllers;
public class PatientRecordsController(ClinicDb db, IWebHostEnvironment env, IConfiguration config, IHttpClientFactory httpClientFactory) : Controller
{
    private string? PatientUser => HttpContext.Session.GetString("Role")=="Patient" ? HttpContext.Session.GetString("User") : null;
    private bool Owner => HttpContext.Session.GetString("Role")=="Owner";
    public IActionResult Index()
    {
        var user=PatientUser;
        if(user==null)return RedirectToAction("Login","Auth");
        ViewBag.User=user;
        ViewBag.Profile=db.Patients.FirstOrDefault(x=>x.Name==user);
        ViewBag.Doctors=db.Doctors.Where(x=>x.IsActive).OrderBy(x=>x.Name).ToList();
        ViewBag.Appointments=db.Appointments.Where(x=>x.Patient==user).OrderByDescending(x=>x.Schedule).ToList();
        ViewBag.Invoices=db.Invoices.Where(x=>x.Patient==user).OrderByDescending(x=>x.Date).ToList();
        ViewBag.Consultations=db.Consultations.Where(x=>x.Patient==user).OrderByDescending(x=>x.Date).ToList();
        ViewBag.PrescriptionDocuments=db.PrescriptionDocuments.Where(d=>db.Consultations.Any(c=>c.Id==d.ConsultationId && c.Patient==user) && db.PrescriptionReleases.Any(r=>r.ConsultationId==d.ConsultationId)).ToList();
        ViewBag.ReleasedPrescriptions=db.PrescriptionReleases.Where(x=>db.Consultations.Any(c=>c.Id==x.ConsultationId && c.Patient==user)).ToList();
        ViewBag.Labs=db.LabRequests.Where(x=>x.Patient==user && (x.Status=="Released" || x.Status=="Completed")).OrderByDescending(x=>x.Requested).ToList();
        return View();
    }
    [HttpPost,ValidateAntiForgeryToken]
    public IActionResult SaveProfile(string contact,string address,string? hmo)
    {
        var user=PatientUser;if(user==null)return Unauthorized();
        var profile=db.Patients.FirstOrDefault(x=>x.Name==user);
        if(profile==null){profile=new Patient{Name=user};db.Patients.Add(profile);}
        profile.Contact=(contact??"").Trim();profile.Address=(address??"").Trim();profile.Hmo=(hmo??"").Trim();
        db.SaveChanges();return Json(new{saved=true});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public IActionResult Book(int doctorId,DateTime schedule,string service)
    {
        var user=PatientUser;if(user==null)return Unauthorized();
        var doctor=db.Doctors.FirstOrDefault(x=>x.Id==doctorId && x.IsActive);
        if(doctor==null || schedule<DateTime.Now || string.IsNullOrWhiteSpace(service))return BadRequest("Invalid booking.");
        if(db.Appointments.Any(x=>x.Doctor==doctor.Name && x.Schedule==schedule && x.Status!="Cancelled"))
        {TempData["Message"]="That appointment slot is already taken.";return RedirectToAction(nameof(Index));}
        db.Appointments.Add(new Appointment{Patient=user,Doctor=doctor.Name,Specialty=doctor.Specialty,Branch=doctor.Branch,Service=service.Trim(),Schedule=schedule,EndTime=schedule.AddMinutes(30),Status="Pending"});
        db.SaveChanges();TempData["Message"]="Appointment request submitted.";return RedirectToAction(nameof(Index));
    }
    [HttpPost,ValidateAntiForgeryToken,RequestSizeLimit(5_000_000)]
    public IActionResult UploadProof(int appointmentId,IFormFile proof,string referenceNo)
    {
        var user=PatientUser;if(user==null)return Unauthorized();
        var a=db.Appointments.FirstOrDefault(x=>x.Id==appointmentId && x.Patient==user);
        if(a==null)return NotFound();
        var ext=Path.GetExtension(proof?.FileName??"").ToLowerInvariant();
        if(proof==null || proof.Length==0 || proof.Length>4_000_000 || !new[]{".jpg",".jpeg",".png",".webp",".pdf"}.Contains(ext))return BadRequest("Upload a JPG, PNG, WEBP or PDF up to 4 MB.");
        var dir=Path.Combine(env.ContentRootPath,"App_Data","private_payment_proofs");Directory.CreateDirectory(dir);
        var filename=Guid.NewGuid().ToString("N")+ext;
        using(var file=System.IO.File.Create(Path.Combine(dir,filename)))proof.CopyTo(file);
        a.ProofPath=filename;a.ReferenceNo=(referenceNo??"").Trim();a.PaymentStatus="Pending Verification";
        db.SaveChanges();TempData["Message"]="Proof uploaded; payment is NOT verified yet.";return RedirectToAction(nameof(Index));
    }
    public IActionResult PrintReceipt(int id)
    {
        var user=PatientUser;var invoice=db.Invoices.Find(id);
        if(invoice==null || !(Owner || (user!=null && invoice.Patient==user)))return NotFound();
        if(invoice.Status!="Paid")return BadRequest("Receipt available only for verified paid invoices.");
        return View(invoice);
    }
    public IActionResult PrintPrescription(int id)
    {
        var user=PatientUser;var consult=db.Consultations.Find(id);
        if(consult==null || !(Owner || (user!=null && consult.Patient==user)))return NotFound();
        if(string.IsNullOrWhiteSpace(consult.Prescription) || !db.PrescriptionReleases.Any(x=>x.ConsultationId==id))return BadRequest("Prescription is not yet released by the clinic.");
        return View(consult);
    }
    // Downloadable PDFs are available only to the patient who owns the record or an Owner.
    public IActionResult DownloadReceiptPdf(int id)
    {
        var user=PatientUser;var invoice=db.Invoices.Find(id);
        if(invoice==null || !(Owner || (user!=null && invoice.Patient==user)))return NotFound();
        if(invoice.Status!="Paid")return BadRequest("Receipt requires verified payment.");
        var lines=new [] { $"Receipt No: {invoice.Id}", $"Patient: {invoice.Patient}", $"Service: {invoice.Service}", $"Date: {invoice.Date:yyyy-MM-dd HH:mm}", $"Method: {invoice.PaymentMethod}", $"Amount PHP: {invoice.Amount:0.00}", $"Discount PHP: {invoice.Discount:0.00}", $"Status: {invoice.Status}" };
        return File(Armelle.Services.SimplePdfDocument.Create("ARMELLE PAYMENT RECEIPT",lines),"application/pdf",$"ARMELLE_Receipt_{id}.pdf");
    }
    public IActionResult DownloadPrescriptionPdf(int id)
    {
        var user=PatientUser;var c=db.Consultations.Find(id);
        if(c==null || !(Owner || (user!=null && c.Patient==user)))return NotFound();
        if(string.IsNullOrWhiteSpace(c.Prescription))return BadRequest("No prescription is recorded.");
        var release=db.PrescriptionReleases.FirstOrDefault(x=>x.ConsultationId==id);
        if(release==null)return BadRequest("Prescription is not yet released by the clinic.");
        var lines=new List<string>{"PRESCRIPTION RECORD - VERIFY ORIGINAL PHYSICIAN SIGNATURE",$"Patient: {c.Patient}",$"Doctor (recorded): {c.Doctor}",$"Date: {c.Date:yyyy-MM-dd}",$"Diagnosis: {c.Diagnosis}","Prescription recorded:"};
        lines.AddRange(c.Prescription.Split('\n').SelectMany(x=>Enumerable.Range(0,Math.Max(1,(x.Length+89)/90)).Select(i=>x.Substring(Math.Min(i*90,x.Length),Math.Min(90,Math.Max(0,x.Length-i*90))))));
        lines.Add($"Physician approval recorded: {release.PhysicianName}, license {release.LicenseNumber}");
        lines.Add($"Estimated medicine cost (PHP): {release.EstimatedMedicineTotal:0.00}");
        lines.AddRange(release.PriceBreakdown.Split('\n',StringSplitOptions.RemoveEmptyEntries));
        lines.Add("Prices are estimates, not a bill or proof of dispensing.");
        lines.Add("Verify original physician-signed prescription before dispensing.");
        return File(Armelle.Services.SimplePdfDocument.Create("ARMELLE PRESCRIPTION RECORD",lines),"application/pdf",$"ARMELLE_Prescription_Record_{id}.pdf");
    }

    // Hosted PayMongo checkout: the clinic must configure its own API secret.
    // Payment is NEVER marked paid based on a browser redirect alone.
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> StartOnlinePayment(int invoiceId)
    {
        var user=PatientUser;if(user==null)return Unauthorized();
        var invoice=db.Invoices.FirstOrDefault(x=>x.Id==invoiceId && x.Patient==user);
        if(invoice==null)return NotFound();
        if(invoice.Status=="Paid")return BadRequest("Invoice already paid.");
        var secret=config["PayMongo:SecretKey"];
        if(string.IsNullOrWhiteSpace(secret))return BadRequest("Online checkout is not configured. Ask the clinic owner to configure PayMongo:SecretKey.");
        var amount=(int)Math.Round((invoice.Amount-invoice.Discount)*100m,0);
        if(amount<100)return BadRequest("Invoice amount must be at least PHP 1.00.");
        var origin=$"{Request.Scheme}://{Request.Host}";
        var payload=new {data=new {attributes=new {
            billing=new {name=user},
            send_email_receipt=false,
            show_description=true,
            show_line_items=true,
            payment_method_types=new[]{"gcash","card"},
            line_items=new[]{new {currency="PHP",amount,name=invoice.Service,quantity=1}},
            success_url=$"{origin}/PatientRecords/OnlinePaymentReturn?invoiceId={invoiceId}",
            cancel_url=$"{origin}/PatientRecords/Index",
            description=$"ARMELLE invoice #{invoiceId}"
        }}};
        using var client=httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(secret+":")));
        using var response=await client.PostAsJsonAsync("https://api.paymongo.com/v1/checkout_sessions",payload);
        var json=await response.Content.ReadAsStringAsync();
        if(!response.IsSuccessStatusCode)return BadRequest("Checkout provider rejected request: "+(int)response.StatusCode);
        using var doc=System.Text.Json.JsonDocument.Parse(json);
        var attrs=doc.RootElement.GetProperty("data").GetProperty("attributes");
        var checkout=attrs.GetProperty("checkout_url").GetString();
        var sessionId=doc.RootElement.GetProperty("data").GetProperty("id").GetString();
        if(string.IsNullOrWhiteSpace(checkout)||string.IsNullOrWhiteSpace(sessionId))return BadRequest("Invalid checkout response.");
        invoice.PaymentMethod="PayMongo Checkout";
        invoice.ProofPath="checkout:"+sessionId;
        invoice.Status="Pending";
        db.SaveChanges();
        return Redirect(checkout);
    }
    public async Task<IActionResult> OnlinePaymentReturn(int invoiceId)
    {
        var user=PatientUser;if(user==null)return Unauthorized();
        var invoice=db.Invoices.FirstOrDefault(x=>x.Id==invoiceId && x.Patient==user);
        if(invoice==null)return NotFound();
        if(!invoice.ProofPath.StartsWith("checkout:",StringComparison.Ordinal))return BadRequest("No checkout session found.");
        var secret=config["PayMongo:SecretKey"];
        if(string.IsNullOrWhiteSpace(secret))return BadRequest("Payment provider not configured.");
        using var client=httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(secret+":")));
        var sessionId=invoice.ProofPath.Substring("checkout:".Length);
        using var response=await client.GetAsync("https://api.paymongo.com/v1/checkout_sessions/"+Uri.EscapeDataString(sessionId));
        if(!response.IsSuccessStatusCode)return BadRequest("Unable to verify payment with provider.");
        using var doc=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var attrs=doc.RootElement.GetProperty("data").GetProperty("attributes");
        var status=attrs.TryGetProperty("payment_intent",out var intent)&&intent.ValueKind==System.Text.Json.JsonValueKind.Object&&intent.TryGetProperty("attributes",out var iattrs)&&iattrs.TryGetProperty("status",out var ps)?ps.GetString():null;
        // Confirm only provider-reported paid checkout status, never success_url alone.
        var checkoutStatus=attrs.TryGetProperty("status",out var st)?st.GetString():null;
        if(string.Equals(checkoutStatus,"paid",StringComparison.OrdinalIgnoreCase)||string.Equals(status,"succeeded",StringComparison.OrdinalIgnoreCase))
        {invoice.Status="Paid";db.SaveChanges();TempData["Message"]="Online payment verified with provider.";}
        else TempData["Message"]="Payment not yet confirmed by provider. Please check again later.";
        return RedirectToAction(nameof(Index));
    }
    public IActionResult PhysicianAuthorization()
    {
        if(!Owner)return Forbid();
        ViewBag.Medicines=db.Medicines.OrderBy(x=>x.GenericName).ToList();
        ViewBag.ReleasedIds=db.PrescriptionReleases.Select(x=>x.ConsultationId).ToList();
        return View(db.Consultations.Where(x=>x.Prescription!="").OrderByDescending(x=>x.Date).ToList());
    }
    // Explicit physician attestation releases the existing prescription to the patient's portal.
    // Prices are calculated from the clinic's medicine catalog, never invented from free text.
    [HttpPost,ValidateAntiForgeryToken]
    public IActionResult RecordPhysicianAuthorization(
        int consultationId,string physicianName,string licenseNumber,bool confirmed,
        int[]? medicineIds,int[]? quantities)
    {
        if(!Owner)return Forbid();
        if(!confirmed || string.IsNullOrWhiteSpace(physicianName) || string.IsNullOrWhiteSpace(licenseNumber))
            return BadRequest("Real physician approval, name and license are required.");
        var consult=db.Consultations.Find(consultationId);
        if(consult==null)return NotFound();
        if(string.IsNullOrWhiteSpace(consult.Prescription))return BadRequest("No prescription recorded.");
        if(db.PrescriptionReleases.Any(x=>x.ConsultationId==consultationId))
            return BadRequest("Already released. To revise a prescription, use a new authorized consultation record.");
        decimal total=0m;
        var breakdown=new List<string>();
        medicineIds ??= Array.Empty<int>();
        quantities ??= Array.Empty<int>();
        if(medicineIds.Length!=quantities.Length || medicineIds.Length>20)
            return BadRequest("Invalid medicine line items.");
        for(int i=0;i<medicineIds.Length;i++)
        {
            if(medicineIds[i]==0 && quantities[i]==0)continue;
            if(quantities[i]<1 || quantities[i]>1000)return BadRequest("Invalid medicine quantity.");
            var medicine=db.Medicines.Find(medicineIds[i]);
            if(medicine==null || medicine.Price<0)return BadRequest("Unknown medicine or price.");
            var amount=medicine.Price*quantities[i];
            total+=amount;
            breakdown.Add($"{medicine.GenericName} {medicine.Strength} x{quantities[i]} @ PHP {medicine.Price:0.00} = PHP {amount:0.00}");
        }
        db.PrescriptionReleases.Add(new PrescriptionRelease
        {
            ConsultationId=consultationId,
            PhysicianName=physicianName.Trim(),
            LicenseNumber=licenseNumber.Trim(),
            RecordedBy=HttpContext.Session.GetString("User")??"Owner",
            ReleasedAt=DateTime.UtcNow,
            EstimatedMedicineTotal=total,
            PriceBreakdown=string.Join("\n",breakdown)
        });
        db.SaveChanges();
        TempData["Message"]="Prescription released automatically to the patient's private portal. No email was sent. Medicine prices are estimates.";
        return RedirectToAction(nameof(PhysicianAuthorization));
    }

    public IActionResult PaymentProof(int id)
    {
        if(!Owner)return Forbid();
        var a=db.Appointments.Find(id);if(a==null||string.IsNullOrWhiteSpace(a.ProofPath))return NotFound();
        var path=Path.Combine(env.ContentRootPath,"App_Data","private_payment_proofs",Path.GetFileName(a.ProofPath));
        if(!System.IO.File.Exists(path))return NotFound();
        var ext=Path.GetExtension(path).ToLowerInvariant();var mime=ext==".pdf"?"application/pdf":ext==".png"?"image/png":ext==".webp"?"image/webp":"image/jpeg";
        return PhysicalFile(path,mime);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public IActionResult VerifyPayment(int appointmentId,bool approved)
    {
        if(!Owner)return Forbid();var a=db.Appointments.Find(appointmentId);if(a==null)return NotFound();
        if(approved && string.IsNullOrWhiteSpace(a.ProofPath))return BadRequest("No payment proof was submitted.");
        a.PaymentStatus=approved?"Verified":"Rejected";
        if(approved && !db.Invoices.Any(x=>x.Patient==a.Patient && x.Service=="Appointment #"+a.Id))
        {
            var fee=db.Doctors.Where(d=>d.Name==a.Doctor).Select(d=>(decimal?)d.ConsultationFee).FirstOrDefault()??0m;
            db.Invoices.Add(new Invoice{Patient=a.Patient,Service="Appointment #"+a.Id,Amount=fee,PaymentMethod="Manual proof verified",Status="Paid",Date=DateTime.Now});
        }
        db.SaveChanges();return RedirectToAction("Appointments","Clinic");
    }
}
