using System;
using System.ComponentModel.DataAnnotations;
namespace Armelle.Models;
public class ClinicPromotion { public int Id {get;set;} [Required,MaxLength(120)] public string Title {get;set;}=""; [MaxLength(1000)] public string Description {get;set;}=""; [MaxLength(50)] public string PromoCode {get;set;}=""; public decimal DiscountValue {get;set;} public string DiscountType {get;set;}="Percent"; public DateTime StartsAt {get;set;}=DateTime.Today; public DateTime EndsAt {get;set;}=DateTime.Today.AddDays(30); public bool IsPublished {get;set;}=false; public DateTime CreatedAt {get;set;}=DateTime.UtcNow; public string CreatedBy {get;set;}=""; }
public class PromotionAudit { public int Id {get;set;} public int PromotionId {get;set;} public string Action {get;set;}=""; public string OwnerUsername {get;set;}=""; public DateTime AtUtc {get;set;}=DateTime.UtcNow; public string Details {get;set;}=""; }
public class PromotionRedemption { public int Id {get;set;} public int PromotionId {get;set;} public int PatientUserId {get;set;} public DateTime RedeemedAtUtc {get;set;}=DateTime.UtcNow; public string RecordedBy {get;set;}=""; public string Reference {get;set;}=""; }
