using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Armelle.Data;
using Armelle.Models;
namespace Armelle.Controllers;
public class ContentController(ClinicDb db):Controller
{
 public override void OnActionExecuting(ActionExecutingContext context)
 { if(HttpContext.Session.GetString("Role")!="Owner") {context.Result=RedirectToAction("Login","Auth");return;} base.OnActionExecuting(context); }
 public IActionResult Index(){ViewBag.Services=db.ClinicServices.OrderBy(x=>x.Name).ToList();ViewBag.Locations=db.ClinicLocations.OrderBy(x=>x.Name).ToList();ViewBag.Resources=db.ResourceItems.OrderBy(x=>x.Title).ToList();return View();}
 [HttpPost,ValidateAntiForgeryToken] public IActionResult AddService(ClinicService x){if(!ModelState.IsValid)return BadRequest("Invalid service");db.ClinicServices.Add(x);db.SaveChanges();return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public IActionResult UpdateService(int id,string name,string category,string description,decimal price,string status){var x=db.ClinicServices.Find(id);if(x==null)return NotFound();if(string.IsNullOrWhiteSpace(name)||price<0)return BadRequest("Invalid name or price");x.Name=name.Trim();x.Category=category??"";x.Description=description??"";x.Price=price;x.Status=status=="Unavailable"?"Unavailable":"Available";db.SaveChanges();return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public IActionResult DeleteService(int id){var x=db.ClinicServices.Find(id);if(x!=null){db.Remove(x);db.SaveChanges();}return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public IActionResult AddLocation(ClinicLocation x){if(!ModelState.IsValid)return BadRequest("Invalid branch");db.ClinicLocations.Add(x);db.SaveChanges();return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public IActionResult UpdateLocation(int id,string name,string address,string hours,string contact){var x=db.ClinicLocations.Find(id);if(x==null)return NotFound();if(string.IsNullOrWhiteSpace(name))return BadRequest("Branch name required");x.Name=name.Trim();x.Address=address??"";x.Hours=hours??"";x.Contact=contact??"";db.SaveChanges();return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public IActionResult DeleteLocation(int id){var x=db.ClinicLocations.Find(id);if(x!=null){db.Remove(x);db.SaveChanges();}return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public IActionResult AddResource(ResourceItem x){if(!ModelState.IsValid)return BadRequest("Invalid resource");db.ResourceItems.Add(x);db.SaveChanges();return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public IActionResult UpdateResource(int id,string title,string category,string content,bool published){var x=db.ResourceItems.Find(id);if(x==null)return NotFound();if(string.IsNullOrWhiteSpace(title))return BadRequest("Title required");x.Title=title.Trim();x.Category=category??"";x.Content=content??"";x.Published=published;db.SaveChanges();return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public IActionResult DeleteResource(int id){var x=db.ResourceItems.Find(id);if(x!=null){db.Remove(x);db.SaveChanges();}return RedirectToAction(nameof(Index));}
}
