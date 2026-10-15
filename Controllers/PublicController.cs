using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using Armelle.Data;using Armelle.Models;using Armelle.Services;
namespace Armelle.Controllers;
public class PublicController(ClinicDb db,AiService ai,GalleryStore gallery,PublicPhotoStore publicPhotos,IWebHostEnvironment env):Controller{
 public IActionResult Index()
 {
     ViewBag.OwnerCanEdit = HttpContext.Session.GetString("Role") == "Owner" && HttpContext.Session.GetString("User") != null;
     var path = Path.Combine(env.ContentRootPath, "App_Data", "homepage_hero.txt");
     var stored = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path).Trim() : "";
     ViewBag.HeroImage = stored.StartsWith("/uploads/homepage/", StringComparison.Ordinal) ? stored : "";
     return View();
 }
 [HttpPost]
 [ValidateAntiForgeryToken]
 [RequestSizeLimit(12_000_000)]
 public async Task<IActionResult> UploadHomepageHero(IFormFile? photo)
 {
     if (HttpContext.Session.GetString("Role") != "Owner" || HttpContext.Session.GetString("User") == null) return Forbid();
     if (photo == null || photo.Length == 0 || photo.Length > 10_000_000) return BadRequest("Select a picture up to 10 MB.");
     var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
     if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext)) return BadRequest("Only JPG, PNG, or WEBP images are accepted.");
     var header = new byte[12];
     await using (var stream = photo.OpenReadStream())
     {
         if (await stream.ReadAsync(header.AsMemory(0, 12)) < 12) return BadRequest("Invalid image.");
     }
     bool valid = ((ext == ".jpg" || ext == ".jpeg") && header[0] == 255 && header[1] == 216 && header[2] == 255)
       || (ext == ".png" && header.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}))
       || (ext == ".webp" && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP");
     if (!valid) return BadRequest("Image content does not match its extension.");
     var dir = Path.Combine(env.WebRootPath, "uploads", "homepage");
     Directory.CreateDirectory(dir);
     var fileName = Guid.NewGuid().ToString("N") + ext;
     await using (var output = System.IO.File.Create(Path.Combine(dir, fileName))) await photo.CopyToAsync(output);
     var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
     Directory.CreateDirectory(dataDir);
     var dest = Path.Combine(dataDir, "homepage_hero.txt");
     var temp = dest + ".tmp";
     await System.IO.File.WriteAllTextAsync(temp, "/uploads/homepage/" + fileName);
     System.IO.File.Move(temp, dest, true);
     return RedirectToAction(nameof(Index));
 }


 [HttpPost]
 [ValidateAntiForgeryToken]
 [RequestSizeLimit(12_000_000)]
 public async Task<IActionResult> UploadPublicPhoto(string key, string? returnUrl, IFormFile? photo)
 {
     if (HttpContext.Session.GetString("Role") != "Owner" || string.IsNullOrEmpty(HttpContext.Session.GetString("User"))) return Forbid();
     var allowed = System.Text.RegularExpressions.Regex.IsMatch(key ?? "", @"^about-(about|history|mission|vision|values|facilities)-[1-4]$") || key == "contact-main" || System.Text.RegularExpressions.Regex.IsMatch(key ?? "", @"^resource-(guide|hmo|philhealth|price|laboratory|imaging|pharmacy|rights|faq|appointment)-[1-4]$") || System.Text.RegularExpressions.Regex.IsMatch(key ?? "", @"^branch-preview-[1-4]$") || System.Text.RegularExpressions.Regex.IsMatch(key ?? "", @"^branch-[1-9][0-9]*-[1-8]$");
     if (!allowed) return BadRequest("Unknown photo location.");
     if (photo == null || photo.Length == 0 || photo.Length > 10_000_000) return BadRequest("Choose an image under 10 MB.");
     var ext=Path.GetExtension(photo.FileName).ToLowerInvariant();
     if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return BadRequest("Only JPG, PNG, and WEBP are allowed.");
     var header=new byte[12];
     await using (var input=photo.OpenReadStream()) if (await input.ReadAsync(header.AsMemory()) < 12) return BadRequest("Invalid image.");
     var valid=(ext is ".jpg" or ".jpeg") && header[0]==255 && header[1]==216 && header[2]==255
         || ext==".png" && header.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})
         || ext==".webp" && System.Text.Encoding.ASCII.GetString(header,0,4)=="RIFF" && System.Text.Encoding.ASCII.GetString(header,8,4)=="WEBP";
     if (!valid) return BadRequest("Invalid image content.");
     var folder=Path.Combine(env.WebRootPath,"uploads","public-content");
     Directory.CreateDirectory(folder);
     var filename=Guid.NewGuid().ToString("N")+ext;
     await using(var output=System.IO.File.Create(Path.Combine(folder,filename))) await photo.CopyToAsync(output);
     publicPhotos.Set(key!,"/uploads/public-content/"+filename);
     if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
     return RedirectToAction(nameof(Index));
 }

 public IActionResult Doctors(string? specialty){var q=db.Doctors.Where(x=>x.IsActive);if(!string.IsNullOrWhiteSpace(specialty))q=q.Where(x=>x.Specialty.Contains(specialty));return View(q.OrderBy(x=>x.Specialty).ThenBy(x=>x.Name).ToList());}
 public IActionResult Services(string? q){var x=db.ClinicServices.Where(s=>s.Status=="Available");if(!string.IsNullOrWhiteSpace(q))x=x.Where(s=>s.Name.Contains(q)||s.Category.Contains(q)||s.Description.Contains(q));return View(x.ToList());}
 public IActionResult Locations()=>View(db.ClinicLocations.ToList());
 public IActionResult Team()=>View(new Armelle.Models.TeamDirectoryVm { Doctors=db.Doctors.Where(x=>x.IsActive).OrderBy(x=>x.Name).ToList(), Employees=db.Employees.Where(x=>x.IsActive).OrderBy(x=>x.Name).ToList() });
 public IActionResult Resources(string? category){ViewBag.Category=category??"";var q=db.ResourceItems.Where(x=>x.Published);if(!string.IsNullOrWhiteSpace(category))q=q.Where(x=>x.Category==category||x.Title.Contains(category));return View(q.ToList());}
 public IActionResult Contact()=>View();public IActionResult About(string? section){ViewBag.AboutSection=section??"about";return View();}public IActionResult Gallery()
 {
     ViewBag.OwnerCanEdit = HttpContext.Session.GetString("Role") == "Owner" && HttpContext.Session.GetString("User") != null;
     return View(gallery.Read());
 }
 [HttpPost]
 [ValidateAntiForgeryToken]
 [RequestSizeLimit(12_000_000)]
 public async Task<IActionResult> UploadGallery(int id, string? title, IFormFile? photo)
 {
     if (HttpContext.Session.GetString("Role") != "Owner" || HttpContext.Session.GetString("User") == null) return Forbid();
     if (photo == null || photo.Length == 0 || photo.Length > 10_000_000) return BadRequest("Please select an image up to 10 MB.");
     var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
     if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension)) return BadRequest("Only JPG, PNG and WEBP images are allowed.");
     var signatures = new byte[12];
     await using (var check = photo.OpenReadStream()) { _ = await check.ReadAsync(signatures); }
     bool valid = (extension is ".jpg" or ".jpeg") && signatures[0] == 0xff && signatures[1] == 0xd8 && signatures[2] == 0xff
       || extension == ".png" && signatures.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})
       || extension == ".webp" && System.Text.Encoding.ASCII.GetString(signatures,0,4) == "RIFF" && System.Text.Encoding.ASCII.GetString(signatures,8,4) == "WEBP";
     if (!valid) return BadRequest("Invalid image file.");
     var folder = Path.Combine(env.WebRootPath, "uploads", "gallery");
     Directory.CreateDirectory(folder);
     var filename = Guid.NewGuid().ToString("N") + extension;
     await using (var output = System.IO.File.Create(Path.Combine(folder, filename))) await photo.CopyToAsync(output);
     var photos = gallery.Read();
     var existing = photos.FirstOrDefault(x => x.Id == id);
     if (existing == null && id > 0 && id <= 95)
     {
         existing = new Armelle.Models.GalleryPhoto { Id = id };
         photos.Add(existing);
     }
     if (existing == null)
     {
         existing = new Armelle.Models.GalleryPhoto { Id = Math.Max(95, photos.Count == 0 ? 0 : photos.Max(x=>x.Id)) + 1 };
         photos.Add(existing);
     }
     existing.Title = string.IsNullOrWhiteSpace(title) ? (string.IsNullOrWhiteSpace(existing.Title) ? "ARMELLE Facility" : existing.Title) : title.Trim()[..Math.Min(title.Trim().Length, 150)];
     existing.ImagePath = "/uploads/gallery/" + filename;
     existing.IsCustom = true;
     gallery.Save(photos);
     return RedirectToAction(nameof(Gallery), new { updated = 1 });
 }
 [HttpPost]
 [ValidateAntiForgeryToken]
 public IActionResult DeleteGallery(int id)
 {
     if (HttpContext.Session.GetString("Role") != "Owner" || HttpContext.Session.GetString("User") == null) return Forbid();
     var photos = gallery.Read();
     photos.RemoveAll(x => x.Id == id);
     gallery.Save(photos);
     return RedirectToAction(nameof(Gallery));
 }
 [HttpPost]public async Task<IActionResult> AskAi([FromBody]AiQuestion x){var q=x.Question??"";var wantsBooking=new[]{"appointment","book","booking","reserve","magpa-appointment","mag book","mag-book"}.Any(k=>q.Contains(k,StringComparison.OrdinalIgnoreCase));var logged=HttpContext.Session.GetString("User")!=null;var answer=await ai.Ask(q);if(wantsBooking&&!logged)answer += " To reserve a schedule, please sign in or create your ARMELLE Patient Account first.";return Json(new{answer,loginRequired=wantsBooking&&!logged});}
 [HttpPost]public IActionResult StartHuman([FromBody]HumanStart x){var visitor=HttpContext.Session.GetString("User")??"Guest Visitor";var agents=db.AppUsers.Where(u=>u.Role.Contains("Agent")).ToList();var assigned=agents.OrderBy(a=>db.SupportConversations.Count(c=>c.AssignedTo==a.Username&&c.Status!="Closed")).FirstOrDefault();var c=new SupportConversation{VisitorName=visitor,Email="",Status=assigned==null?"Waiting for Agent":"Assigned to Agent",AssignedTo=assigned?.Username??""};db.Add(c);db.SaveChanges();if(x.History!=null)foreach(var h in x.History.TakeLast(20))db.Add(new SupportMessage{ConversationId=c.Id,Sender=h.Sender=="Visitor"?"Visitor":"AI",Message=h.Message??""});db.Add(new SupportMessage{ConversationId=c.Id,Sender="Visitor",Message=x.Message??"I would like to talk to an agent."});db.SaveChanges();var message=assigned==null?"All agents are currently assisting patients. You are in the ARMELLE support queue and will be connected as soon as an agent is available.":$"Connected to ARMELLE Agent {assigned.Username}. Your Ask ARMELLE conversation has been shared with the agent.";return Json(new{id=c.Id,status=c.Status,message});}
 [HttpGet]public IActionResult Messages(int id)=>Json(db.SupportMessages.Where(x=>x.ConversationId==id).OrderBy(x=>x.SentAt).ToList());
 [HttpPost]public IActionResult SendMessage([FromBody]ChatSend x){var c=db.SupportConversations.Find(x.ConversationId);if(c==null)return NotFound();db.Add(new SupportMessage{ConversationId=x.ConversationId,Sender="Visitor",Message=x.Message??""});c.UpdatedAt=DateTime.Now;db.SaveChanges();return Ok();}
 [HttpPost]public IActionResult Rate([FromBody]RateRequest x){db.Add(new SupportRating{ConversationId=x.ConversationId,RatedType=x.RatedType??"AI",Stars=Math.Clamp(x.Stars,1,5),Comment=x.Comment??""});db.SaveChanges();return Ok();}
 [HttpGet]public IActionResult Book(){if(HttpContext.Session.GetString("User")==null||HttpContext.Session.GetString("Role")!="Patient")return RedirectToAction("Login","Auth",new{returnUrl="/Public/Book"});return View(new BookingPageVm{Branches=db.ClinicLocations.ToList(),Services=db.ClinicServices.Where(x=>x.Status=="Available").OrderBy(x=>x.Category).ThenBy(x=>x.Name).ToList()});}
 [HttpPost]public async Task<IActionResult> Book(BookingRequest r){if(HttpContext.Session.GetString("User")==null||HttpContext.Session.GetString("Role")!="Patient")return RedirectToAction("Login","Auth",new{returnUrl="/Public/Book"});
  var vm=new BookingPageVm{Branches=db.ClinicLocations.ToList(),Services=db.ClinicServices.Where(x=>x.Status=="Available").OrderBy(x=>x.Category).ThenBy(x=>x.Name).ToList()};
  if(string.IsNullOrWhiteSpace(r.Patient)||string.IsNullOrWhiteSpace(r.Branch)||string.IsNullOrWhiteSpace(r.Service)){ViewBag.BookError="Please complete the patient, branch, service and preferred date.";return View(vm);}
  if(r.PreferredDate.Date<DateTime.Today){ViewBag.BookError="Please select today or a future clinic date.";return View(vm);}
  var service=db.ClinicServices.FirstOrDefault(x=>x.Name==r.Service&&x.Status=="Available");if(service==null){ViewBag.BookError="Selected service is unavailable.";return View(vm);}
  var specialty=service.Category=="Consultation"?service.Name.Replace(" Consultation",""):service.Category;
  var doctors=db.Doctors.Where(x=>x.IsActive&&x.Branch==r.Branch&&x.Status=="Available"&&(x.Specialty.Contains(specialty)||specialty.Contains(x.Specialty))).ToList();
  if(doctors.Count==0)doctors=db.Doctors.Where(x=>x.IsActive&&x.Branch==r.Branch&&x.Status=="Available").ToList();
  var duration=service.Category.Contains("Dental",StringComparison.OrdinalIgnoreCase)?60:service.Category.Contains("Imaging",StringComparison.OrdinalIgnoreCase)?30:30;
  Doctor? chosen=null;DateTime slot=default;
  foreach(var d in doctors.OrderBy(x=>db.Appointments.Count(a=>a.Doctor==x.Name&&a.Schedule.Date==r.PreferredDate.Date))){
   var booked=db.Appointments.Count(a=>a.Doctor==d.Name&&a.Schedule.Date==r.PreferredDate.Date&&a.Status!="Cancelled");if(booked>=d.DailyLimit)continue;
   for(var t=r.PreferredDate.Date.AddHours(8);t<r.PreferredDate.Date.AddHours(17);t=t.AddMinutes(duration)){
    var clash=db.Appointments.Any(a=>a.Doctor==d.Name&&a.Status!="Cancelled"&&a.Schedule<t.AddMinutes(duration)&&a.EndTime>t);if(!clash){chosen=d;slot=t;break;}
   }if(chosen!=null)break;
  }
  if(chosen==null){ViewBag.BookError="The selected date is fully booked at this branch. Please choose another date.";return View(vm);}
  string proof="";var online=r.PaymentMethod!="Pay at Clinic";
  if(online&&r.PaymentProof==null){ViewBag.BookError="Online payment requires a proof of payment image or PDF.";return View(vm);}
  if(r.PaymentProof!=null){var dir=Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","uploads","payments");Directory.CreateDirectory(dir);var ext=Path.GetExtension(r.PaymentProof.FileName);var fn=$"PAY-{Guid.NewGuid():N}{ext}";await using var fs=System.IO.File.Create(Path.Combine(dir,fn));await r.PaymentProof.CopyToAsync(fs);proof="/uploads/payments/"+fn;}
  var reference=$"ARM-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";var loggedInPatient = HttpContext.Session.GetString("Role") == "Patient" ? HttpContext.Session.GetString("User") : null;
  var bookingPatient = !string.IsNullOrWhiteSpace(loggedInPatient) ? loggedInPatient : r.Patient;
  var appt=new Appointment{Patient=bookingPatient,Branch=r.Branch,Doctor=chosen.Name,Specialty=chosen.Specialty,Service=r.Service,Schedule=slot,EndTime=slot.AddMinutes(duration),Status="Pending",PaymentStatus=online?"Proof Submitted - For Verification":"Pay at Clinic",ProofPath=proof,ReferenceNo=reference};db.Add(appt);db.SaveChanges();
  var nurses=db.NurseAssignments.Where(x=>x.Doctor==chosen.Name&&x.Branch==r.Branch&&(x.DutyDate.Date==r.PreferredDate.Date||x.DutyDate.Date==DateTime.Today)&&db.Employees.Any(e=>e.IsActive&&e.Name==x.Nurse)).Select(x=>x.Nurse).Take(3).ToList();
  if(nurses.Count==0)nurses=db.Employees.Where(x=>x.IsActive&&x.Role.Contains("Nurse")&&x.Branch==r.Branch).Select(x=>x.Name).Take(3).ToList();
  vm.Result=new BookingResultVm{ReferenceNo=reference,Doctor=chosen.Name,Branch=r.Branch,Service=r.Service,Start=slot,End=slot.AddMinutes(duration),Room=chosen.Room,Nurses=nurses,PaymentStatus=appt.PaymentStatus};return View(vm);
 }
}
public class AiQuestion{public string? Question{get;set;}}public class HumanStart{public string? Message{get;set;}public List<ChatHistoryItem>? History{get;set;}}public class ChatHistoryItem{public string Sender{get;set;}="AI";public string? Message{get;set;}}public class ChatSend{public int ConversationId{get;set;}public string? Message{get;set;}}public class RateRequest{public int ConversationId{get;set;}public string? RatedType{get;set;}public int Stars{get;set;}public string? Comment{get;set;}}
