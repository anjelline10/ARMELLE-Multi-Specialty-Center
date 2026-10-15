
using System.ComponentModel.DataAnnotations;

namespace Armelle.Models
{
    public class OwnerProfile
    {
        public int Id { get; set; }

        [Required]
        public int AppUserId { get; set; }

        [Required]
        [MaxLength(120)]
        public string DisplayName { get; set; } = "";

        [Required]
        [MaxLength(160)]
        public string Position { get; set; } = "";

        [MaxLength(500)]
        public string PhotoPath { get; set; } = "";
    }
}
