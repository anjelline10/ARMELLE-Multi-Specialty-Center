using Microsoft.AspNetCore.Mvc;using Microsoft.AspNetCore.Mvc.Filters;using Armelle.Data;using Armelle.Models;
namespace Armelle.Controllers; public class ClinicController(ClinicDb db, IWebHostEnvironment env):Controller{
 public override void OnActionExecuting(ActionExecutingContext context){if(HttpContext.Session.GetString("Role")!="Owner"){context.Result=RedirectToAction("WorkforceLogin","Auth");return;}base.OnActionExecuting(context);}
 bool NoLogin()=>HttpContext.Session.GetString("User")==null; IActionResult Back(string a)=>RedirectToAction(a); bool CanManage(){var r=HttpContext.Session.GetString("Role")??"";return r.Contains("Owner",StringComparison.OrdinalIgnoreCase)||r.Contains("Admin",StringComparison.OrdinalIgnoreCase)||r.Contains("Root",StringComparison.OrdinalIgnoreCase);} string SavePhoto(IFormFile? f,string folder,string old=""){if(f==null||f.Length==0)return old;var ext=Path.GetExtension(f.FileName).ToLowerInvariant();if(!new[]{".jpg",".jpeg",".png",".webp"}.Contains(ext)||f.Length>3_000_000)return old;var dir=Path.Combine(env.WebRootPath??Path.Combine(env.ContentRootPath,"wwwroot"),"uploads",folder);Directory.CreateDirectory(dir);var name=$"{Guid.NewGuid():N}{ext}";using var fs=System.IO.File.Create(Path.Combine(dir,name));f.CopyTo(fs);return $"/uploads/{folder}/{name}";}
 public IActionResult Patients(string? branch=null)
 {
  if(!CanManage())return RedirectToAction("WorkforceLogin","Auth");
  ViewBag.SelectedBranch=branch??"";
  var q=db.Patients.AsQueryable();
  if(!string.IsNullOrWhiteSpace(branch))q=q.Where(x=>x.Branch==branch);
  return View(q.OrderBy(x=>x.Branch).ThenBy(x=>x.Name)
   .Select(x => new Patient {
     Id=x.Id, Name=x.Name ?? "", BirthDate=x.BirthDate,
     Sex=x.Sex ?? "", Contact=x.Contact ?? "", Address=x.Address ?? "",
     Hmo=x.Hmo ?? "", Branch=x.Branch ?? "North QC"
   }).ToList());
 }
 public IActionResult ExportPatientsCsv(string? branch=null)
 {
  if(!CanManage())return Forbid();
  var q=db.Patients.AsQueryable();if(!string.IsNullOrWhiteSpace(branch))q=q.Where(x=>x.Branch==branch);
  var rows=q.OrderBy(x=>x.Branch).ThenBy(x=>x.Name).ToList();
  static string C(string? v)=>"\""+(v??"").Replace("\"","\"\"")+"\"";
  var sb=new System.Text.StringBuilder("ID,Name,Branch,Birth Date,Sex,Contact,Address,HMO\r\n");
  foreach(var x in rows)sb.AppendLine(string.Join(",",x.Id.ToString(),C(x.Name),C(x.Branch),C(x.BirthDate?.ToString("yyyy-MM-dd")),C(x.Sex),C(x.Contact),C(x.Address),C(x.Hmo)));
  return File(new System.Text.UTF8Encoding(true).GetBytes(sb.ToString()),"text/csv","ARMELLE_Patients_"+DateTime.Today.ToString("yyyyMMdd")+".csv");
 }
[HttpPost,ValidateAntiForgeryToken]
 public IActionResult AddPatient(Patient x)
 {
  if(!CanManage())return Forbid();
  if(string.IsNullOrWhiteSpace(x.Name))return BadRequest("Patient name required.");
  db.Patients.Add(x);db.SaveChanges();return Back("Patients");
 }
 public IActionResult EditPatient(int id)
 {
  if(!CanManage())return Forbid();
  var patient=db.Patients.Find(id);
  return patient==null?NotFound():View(patient);
 }
 [HttpPost,ValidateAntiForgeryToken]
 public IActionResult UpdatePatient(Patient x)
 {
  if(!CanManage())return Forbid();
  var existing=db.Patients.Find(x.Id);
  if(existing==null)return NotFound();
  if(string.IsNullOrWhiteSpace(x.Name))return BadRequest("Patient name required.");
  existing.Name=x.Name.Trim();existing.Branch=x.Branch;existing.BirthDate=x.BirthDate;
  existing.Sex=x.Sex;existing.Contact=x.Contact;existing.Address=x.Address;existing.Hmo=x.Hmo;
  db.SaveChanges();return Back("Patients");
 }
 [HttpPost,ValidateAntiForgeryToken]
 public IActionResult DeletePatient(int id)
 {
  if(!CanManage())return Forbid();
  return DeleteSelectedPatients(new[]{id});
 }
 [HttpPost,ValidateAntiForgeryToken]
 public IActionResult DeleteSelectedPatients(int[] ids)
 {
  if(!CanManage())return Forbid();
  var selected=ids.Distinct().ToArray();
  if(selected.Length==0)return Back("Patients");
  var patients=db.Patients.Where(x=>selected.Contains(x.Id)).ToList();
  int skipped=0;
  foreach(var patient in patients)
  {
   // Do not erase a patient identity when other medical or billing records still reference it.
   bool hasRecords=db.Appointments.Any(x=>x.Patient==patient.Name)
    ||db.Consultations.Any(x=>x.Patient==patient.Name)
    ||db.Invoices.Any(x=>x.Patient==patient.Name)
    ||db.LabRequests.Any(x=>x.Patient==patient.Name);
   if(hasRecords){skipped++;continue;}
   db.Patients.Remove(patient);
  }
  db.SaveChanges();
  if(skipped>0)TempData["PatientNotice"]=$"{skipped} patient(s) retained because they have medical or billing records.";
  return Back("Patients");
 }
 public IActionResult Doctors()=>View(db.Doctors.Where(x=>x.IsActive).OrderBy(x=>x.Name).ToList()); public IActionResult EditDoctor(int id){if(!CanManage())return Forbid();var x=db.Doctors.Find(id);return x==null?NotFound():View(x);}[HttpPost,ValidateAntiForgeryToken]public IActionResult AddDoctor(Doctor x,IFormFile? photo){if(!CanManage())return Forbid();x.PhotoPath=SavePhoto(photo,"doctors");db.Doctors.Add(x);db.SaveChanges();return Back("Doctors");}[HttpPost,ValidateAntiForgeryToken]public IActionResult UpdateDoctor(Doctor x,IFormFile? photo){if(!CanManage())return Forbid();var d=db.Doctors.Find(x.Id);if(d!=null){var old=d.Name;d.Name=x.Name;d.Specialty=x.Specialty;d.Branch=x.Branch;d.Room=x.Room;d.Schedule=x.Schedule;d.DailyLimit=x.DailyLimit;d.ConsultationFee=x.ConsultationFee;d.Status=x.Status;d.Credentials=x.Credentials;d.Bio=x.Bio;d.IsActive=x.IsActive;d.PhotoPath=SavePhoto(photo,"doctors",d.PhotoPath);db.SaveChanges();}return Back("Doctors");}[HttpPost,ValidateAntiForgeryToken]public IActionResult ToggleDoctor(int id){if(!CanManage())return Forbid();var d=db.Doctors.Find(id);if(d!=null){d.IsActive=!d.IsActive;d.Status=d.IsActive?"Available":"Inactive";db.SaveChanges();}return Back("Doctors");}
 public IActionResult Appointments()=>View(db.Appointments.OrderByDescending(x=>x.Schedule).ToList());[HttpPost,ValidateAntiForgeryToken]public IActionResult AddAppointment(Appointment x){db.Appointments.Add(x);db.SaveChanges();return Back("Appointments");}[HttpPost][ValidateAntiForgeryToken]public IActionResult AppointmentStatus(int id,string status){var x=db.Appointments.Find(id);if(x!=null && new[]{"Pending","Confirmed","In Progress","Consultation Done","Completed","Cancelled"}.Contains(status)){x.Status=status;if(status=="Consultation Done"||status=="Completed") {if(!db.Consultations.Any(c=>c.Patient==x.Patient && c.Doctor==x.Doctor && c.Date.Date==x.Schedule.Date)){db.Consultations.Add(new Consultation{Patient=x.Patient,Doctor=x.Doctor,Date=DateTime.Now,Notes="Consultation completed. No prescription issued unless entered from an authorized source."});}}db.SaveChanges();}return Back("Appointments");}
 public IActionResult Queue()=>View(db.QueueEntries.OrderByDescending(x=>x.Id).ToList());[HttpPost,ValidateAntiForgeryToken]public IActionResult CheckIn(string patient,string department){var n=(db.QueueEntries.Any()?db.QueueEntries.Max(x=>x.Id):0)+1;db.QueueEntries.Add(new(){QueueNo=$"A-{n:000}",Patient=patient,Department=department});db.SaveChanges();return Back("Queue");}[HttpPost,ValidateAntiForgeryToken]public IActionResult QueueStatus(int id,string status){var x=db.QueueEntries.Find(id);if(x!=null){x.Status=status;db.SaveChanges();}return Back("Queue");}
 public IActionResult Consultations()=>View(db.Consultations.OrderByDescending(x=>x.Date).ToList());[HttpPost,ValidateAntiForgeryToken]public IActionResult AddConsultation(Consultation x){db.Consultations.Add(x);db.SaveChanges();return Back("Consultations");}
 public IActionResult Laboratory()=>View(db.LabRequests.OrderByDescending(x=>x.Id)
   .Select(x=>new LabRequest { Id=x.Id, Patient=x.Patient ?? "", Test=x.Test ?? "",
     Result=x.Result ?? "", Status=x.Status ?? "Pending", Requested=x.Requested })
   .ToList());[HttpPost,ValidateAntiForgeryToken]public IActionResult AddLab(LabRequest x){db.LabRequests.Add(x);db.SaveChanges();return Back("Laboratory");}[HttpPost,ValidateAntiForgeryToken]public IActionResult LabResult(int id,string result){var x=db.LabRequests.Find(id);if(x!=null){x.Result=result;x.Status="Draft";db.SaveChanges();}return Back("Laboratory");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult ReleaseLab(int id){var x=db.LabRequests.Find(id);if(x!=null && !string.IsNullOrWhiteSpace(x.Result)){x.Status="Released";db.SaveChanges();}return Back("Laboratory");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult UpdateConsultation(int id,string diagnosis,string prescription,string notes){var x=db.Consultations.Find(id);if(x!=null){x.Diagnosis=diagnosis??"";x.Prescription=prescription??"";x.Notes=notes??"";db.SaveChanges();}return Back("Consultations");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult DeleteMedicine(int id){var x=db.Medicines.Find(id);if(x!=null){db.Medicines.Remove(x);db.SaveChanges();}return Back("Pharmacy");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult DeleteLab(int id){var x=db.LabRequests.Find(id);if(x!=null){db.LabRequests.Remove(x);db.SaveChanges();}return Back("Laboratory");}
 public IActionResult Pharmacy()=>View(db.Medicines.ToList());[HttpPost,ValidateAntiForgeryToken]public IActionResult AddMedicine(Medicine x){db.Medicines.Add(x);db.SaveChanges();return Back("Pharmacy");}[HttpPost,ValidateAntiForgeryToken]public IActionResult Dispense(int id,int qty){var x=db.Medicines.Find(id);if(x!=null&&qty>0&&x.Stock>=qty){x.Stock-=qty;db.SaveChanges();}return Back("Pharmacy");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult AdjustStock(int id,int quantity,string operation){var x=db.Medicines.Find(id);if(x==null||quantity<=0)return Back("Pharmacy");if(operation=="In")x.Stock+=quantity;else if(operation=="Out"&&x.Stock>=quantity)x.Stock-=quantity;db.SaveChanges();return Back("Pharmacy");}
 public IActionResult Billing()=>View(db.Invoices.OrderByDescending(x=>x.Id).ToList());[HttpPost,ValidateAntiForgeryToken]public IActionResult AddInvoice(Invoice x){db.Invoices.Add(x);db.SaveChanges();return Back("Billing");}[HttpPost,ValidateAntiForgeryToken]public IActionResult Pay(int id,string method){var x=db.Invoices.Find(id);if(x!=null){x.PaymentMethod=method;x.Status="Paid";db.SaveChanges();}return Back("Billing");}
 public IActionResult Employees()=>View(db.Employees.Where(x=>x.IsActive).OrderBy(x=>x.Name).ToList()); public IActionResult EditEmployee(int id){if(!CanManage())return Forbid();var x=db.Employees.Find(id);return x==null?NotFound():View(x);}[HttpPost,ValidateAntiForgeryToken]public IActionResult AddEmployee(Employee x,IFormFile? photo,string? personalPassword){if(!CanManage())return Forbid();x.PhotoPath=SavePhoto(photo,"employees");db.Employees.Add(x);db.SaveChanges();return Back("Employees");}[HttpPost,ValidateAntiForgeryToken]public IActionResult UpdateEmployee(Employee x,IFormFile? photo,string? newPassword){if(!CanManage())return Forbid();var e=db.Employees.Find(x.Id);if(e!=null){var old=e.Name;e.Name=x.Name;e.Role=x.Role;e.Department=x.Department;e.Branch=x.Branch;e.Shift=x.Shift;e.Rate=x.Rate;e.Email=x.Email;e.Contact=x.Contact;e.Bio=x.Bio;e.IsActive=x.IsActive;e.PhotoPath=SavePhoto(photo,"employees",e.PhotoPath);db.SaveChanges();}return Back("Employees");}[HttpPost,ValidateAntiForgeryToken]public IActionResult ToggleEmployee(int id){if(!CanManage())return Forbid();var e=db.Employees.Find(id);if(e!=null){e.IsActive=!e.IsActive;db.SaveChanges();}return Back("Employees");}[HttpPost,ValidateAntiForgeryToken]public IActionResult TimeIn(int id){var x=db.Employees.Find(id);if(x!=null){x.TimeIn=DateTime.Now;x.TimeOut=null;db.SaveChanges();}return Back("Employees");}[HttpPost,ValidateAntiForgeryToken]public IActionResult TimeOut(int id){var x=db.Employees.Find(id);if(x!=null){x.TimeOut=DateTime.Now;db.SaveChanges();}return Back("Employees");}
 public IActionResult Payroll()=>View(db.Payrolls.ToList());[HttpPost,ValidateAntiForgeryToken]public IActionResult AddPayroll(Payroll x){x.NetPay=x.RegularPay+x.OvertimePay-x.Deductions;db.Payrolls.Add(x);db.SaveChanges();return Back("Payroll");}[HttpPost,ValidateAntiForgeryToken]public IActionResult PayrollStatus(int id,string status){var x=db.Payrolls.Find(id);if(x!=null){x.Status=status;db.SaveChanges();}return Back("Payroll");}
 public IActionResult Expenses()=>View(db.Expenses.OrderByDescending(x=>x.Date).ToList());[HttpPost,ValidateAntiForgeryToken]public IActionResult AddExpense(Expense x){db.Expenses.Add(x);db.SaveChanges();return Back("Expenses");}

 [HttpPost,ValidateAntiForgeryToken]
 public IActionResult DeleteSelectedDoctors(int[]? selectedIds)
 {
  if(!CanManage())return Forbid();
  var ids=(selectedIds??Array.Empty<int>()).Distinct().ToArray();
  var doctors=db.Doctors.Where(d=>ids.Contains(d.Id)&&d.IsActive).ToList();
  foreach(var doctor in doctors){doctor.IsActive=false;doctor.Status="Inactive";}
  db.SaveChanges();
  TempData["Success"]=$"Removed {doctors.Count} doctor(s) from active listings.";
  return Back("Doctors");
 }
 [HttpPost,ValidateAntiForgeryToken]
 public IActionResult DeleteDoctor(int id)
 {
  if(!CanManage())return Forbid();
  var doctor=db.Doctors.Find(id);
  if(doctor!=null){doctor.IsActive=false;doctor.Status="Inactive";db.SaveChanges();}
  return Back("Doctors");
 }
 [HttpPost,ValidateAntiForgeryToken]
 public IActionResult DeleteSelectedEmployees(int[]? selectedIds)
 {
  if(!CanManage())return Forbid();
  var ids=(selectedIds??Array.Empty<int>()).Distinct().ToArray();
  var employees=db.Employees.Where(e=>ids.Contains(e.Id)&&e.IsActive).ToList();
  foreach(var employee in employees)employee.IsActive=false;
  db.SaveChanges();
  TempData["Success"]=$"Removed {employees.Count} staff member(s) from active listings.";
  return Back("Employees");
 }
 [HttpPost,ValidateAntiForgeryToken]
 public IActionResult DeleteEmployee(int id)
 {
  if(!CanManage())return Forbid();
  var employee=db.Employees.Find(id);
  if(employee!=null){employee.IsActive=false;db.SaveChanges();}
  return Back("Employees");
 }

 [HttpPost,ValidateAntiForgeryToken]public IActionResult UpdateMedicine(int id,string genericName,string brand,string strength,decimal price,int stock){var x=db.Medicines.Find(id);if(x==null)return NotFound();if(price<0||stock<0)return BadRequest("Invalid price or stock.");x.GenericName=genericName??"";x.Brand=brand??"";x.Strength=strength??"";x.Price=price;x.Stock=stock;db.SaveChanges();return Back("Pharmacy");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult DeleteAppointment(int id){var x=db.Appointments.Find(id);if(x==null)return NotFound();x.Status="Cancelled";db.SaveChanges();return Back("Appointments");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult DeleteConsultation(int id){var x=db.Consultations.Find(id);if(x==null)return NotFound();db.Consultations.Remove(x);db.SaveChanges();return Back("Consultations");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult UpdateInvoice(int id,string service,decimal amount){var x=db.Invoices.Find(id);if(x==null)return NotFound();if(amount<0)return BadRequest("Invalid amount.");x.Service=service??"";x.Amount=amount;db.SaveChanges();return Back("Billing");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult DeleteInvoice(int id){var x=db.Invoices.Find(id);if(x==null)return NotFound();if(x.Status=="Paid")return BadRequest("Paid receipts must be retained for audit.");db.Invoices.Remove(x);db.SaveChanges();return Back("Billing");}
 [HttpPost,ValidateAntiForgeryToken]public IActionResult DeleteExpense(int id){var x=db.Expenses.Find(id);if(x==null)return NotFound();db.Expenses.Remove(x);db.SaveChanges();return Back("Expenses");}
 public IActionResult Reports(){ViewBag.Revenue=db.Invoices.Where(x=>x.Status=="Paid").Sum(x=>(decimal?)x.Amount)??0;ViewBag.Expenses=db.Expenses.Sum(x=>(decimal?)x.Amount)??0;ViewBag.Payroll=db.Payrolls.Where(x=>x.Status=="Approved").Sum(x=>(decimal?)x.NetPay)??0;ViewBag.Patients=db.Patients.Count();ViewBag.Appointments=db.Appointments.Count();return View();}
}
