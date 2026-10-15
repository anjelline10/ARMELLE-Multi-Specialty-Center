using System.Net.Http.Headers;using System.Text;using System.Text.Json;using Armelle.Data;using Microsoft.EntityFrameworkCore;
namespace Armelle.Services;
public class AiService(IConfiguration cfg,ClinicDb db){
 public async Task<string> Ask(string question){
  var key=Environment.GetEnvironmentVariable("GROQ_API_KEY")??cfg["Groq:ApiKey"];
  if(string.IsNullOrWhiteSpace(key))return "ARMELLE AI is ready, but the server administrator still needs to configure GROQ_API_KEY. You can choose Talk to Agent for live support.";
  var endpoint="https://api.groq.com/openai/v1/chat/completions";var model=cfg["Groq:Model"]??"openai/gpt-oss-20b";
  var doctors=await db.Doctors.AsNoTracking().ToListAsync();var services=await db.ClinicServices.AsNoTracking().Where(x=>x.Status=="Available").ToListAsync();var locations=await db.ClinicLocations.AsNoTracking().ToListAsync();var resources=await db.ResourceItems.AsNoTracking().Where(x=>x.Published).ToListAsync();
  var context=$"Doctors: {string.Join("; ",doctors.Select(x=>$"{x.Name}, {x.Specialty}, {x.Schedule}, {x.Status}"))}\nServices: {string.Join("; ",services.Select(x=>$"{x.Name} ({x.Category}) PHP {x.Price}: {x.Description}"))}\nLocations: {string.Join("; ",locations.Select(x=>$"{x.Name}, {x.Address}, {x.Hours}, {x.Contact}"))}\nResources: {string.Join("; ",resources.Select(x=>$"{x.Title}: {x.Content}"))}";
  var system="You are Ask ARMELLE, the public information assistant for ARMELLE Multi-Specialty Center. Reply naturally in English, Filipino/Tagalog, or Taglish matching the user. Use the clinic data supplied below and do not invent schedules, prices, HMO coverage, results, or availability. You may explain services, Find Care, appointments, walk-ins, lab preparation, pharmacy guidance, locations and clinic resources. You are not a doctor: do not diagnose or prescribe. For emergencies advise contacting local emergency services or seeking urgent professional care. Offer human live support when requested or when clinic-specific information is unavailable. Clinic data:\n"+context;
  using var http=new HttpClient();http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",key);
  var body=JsonSerializer.Serialize(new{model,messages=new[]{new{role="system",content=system},new{role="user",content=question}}});
  var resp=await http.PostAsync(endpoint,new StringContent(body,Encoding.UTF8,"application/json"));if(!resp.IsSuccessStatusCode)return "I couldn't reach the AI service right now. You can use Talk to Agent for live support.";
  using var doc=JsonDocument.Parse(await resp.Content.ReadAsStringAsync());return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()??"No response received.";
 }
}
