namespace Armelle.Models;
public class GalleryPhoto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public bool IsCustom { get; set; }
}
