using System.ComponentModel.DataAnnotations;
namespace Armelle.Models;
public class ClinicService{public int Id{get;set;}[Required]public string Name{get;set;}="";public string Category{get;set;}="";public string Description{get;set;}="";public decimal Price{get;set;}public string Status{get;set;}="Available";}
public class ClinicLocation{public int Id{get;set;}[Required]public string Name{get;set;}="";public string Address{get;set;}="";public string Hours{get;set;}="";public string Contact{get;set;}="";}
public class ResourceItem{public int Id{get;set;}[Required]public string Title{get;set;}="";public string Category{get;set;}="Guide";public string Content{get;set;}="";public bool Published{get;set;}=true;}
public class SupportConversation{public int Id{get;set;}public string VisitorName{get;set;}="Guest";public string Email{get;set;}="";public string Status{get;set;}="AI";public string AssignedTo{get;set;}="";public DateTime CreatedAt{get;set;}=DateTime.Now;public DateTime UpdatedAt{get;set;}=DateTime.Now;}
public class SupportMessage{public int Id{get;set;}public int ConversationId{get;set;}public string Sender{get;set;}="Visitor";public string Message{get;set;}="";public DateTime SentAt{get;set;}=DateTime.Now;}
public class SupportRating{public int Id{get;set;}public int ConversationId{get;set;}public string RatedType{get;set;}="AI";public int Stars{get;set;}public string Comment{get;set;}="";public DateTime CreatedAt{get;set;}=DateTime.Now;}
