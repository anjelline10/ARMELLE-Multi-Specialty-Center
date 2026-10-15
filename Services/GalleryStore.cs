using System.Text.Json;
using Armelle.Models;
namespace Armelle.Services;

// Persist gallery metadata independently of the clinic database schema.
public class GalleryStore(IWebHostEnvironment env)
{
    private static readonly object Sync = new();
    private string DataPath => Path.Combine(env.ContentRootPath, "App_Data", "gallery_photos.json");
    public List<GalleryPhoto> Read()
    {
        lock (Sync)
        {
            if (!File.Exists(DataPath)) return new();
            try { return JsonSerializer.Deserialize<List<GalleryPhoto>>(File.ReadAllText(DataPath)) ?? new(); }
            catch (JsonException) { throw new InvalidOperationException("Gallery data is invalid; restore gallery_photos.json from backup."); }
        }
    }
    public void Save(List<GalleryPhoto> photos)
    {
        lock (Sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath)!);
            var tmp = DataPath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(photos, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmp, DataPath, true);
        }
    }
}
