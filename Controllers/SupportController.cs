using Microsoft.AspNetCore.Mvc;using Armelle.Data;using Armelle.Models;
namespace Armelle.Controllers;public class SupportController(ClinicDb db):Controller{
 bool No()=>HttpContext.Session.GetString("User")==null;
 public IActionResult Index(){if(No())return RedirectToAction("Login","Auth");return View(db.SupportConversations.OrderByDescending(x=>x.UpdatedAt).ToList());}
 public IActionResult Chat(int id){if(No())return RedirectToAction("Login","Auth");ViewBag.C=db.SupportConversations.Find(id);return View(db.SupportMessages.Where(x=>x.ConversationId==id).OrderBy(x=>x.SentAt).ToList());}
 [HttpPost]public IActionResult Reply(int id,string message){var c=db.SupportConversations.Find(id);if(c!=null){c.Status="Human Connected";c.AssignedTo=HttpContext.Session.GetString("User")??"Staff";c.UpdatedAt=DateTime.Now;db.Add(new SupportMessage{ConversationId=id,Sender="Staff",Message=message});db.SaveChanges();}return RedirectToAction("Chat",new{id});}
 [HttpPost]public IActionResult Close(int id){var c=db.SupportConversations.Find(id);if(c!=null){c.Status="Closed";c.UpdatedAt=DateTime.Now;db.SaveChanges();}return RedirectToAction("Index");}
}
