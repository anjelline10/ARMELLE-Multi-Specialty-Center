
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace Armelle.Services;

public class PublicPhotoStore
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public PublicPhotoStore(IWebHostEnvironment env)
    {
        var folder = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "public_photos.json");
    }

    public string Get(string key)
    {
        lock (_lock)
        {
            var photos = Read();
            return photos.TryGetValue(key, out var path)
                ? path
                : "";
        }
    }

    public void Set(string key, string imagePath)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Invalid photo key.");

        lock (_lock)
        {
            var photos = Read();
            photos[key] = imagePath;

            File.WriteAllText(
                _filePath,
                JsonSerializer.Serialize(photos)
            );
        }
    }

    private Dictionary<string, string> Read()
    {
        if (!File.Exists(_filePath))
            return new Dictionary<string, string>();

        var json = File.ReadAllText(_filePath);

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
    }
}
