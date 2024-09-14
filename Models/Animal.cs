using System.ComponentModel.DataAnnotations.Schema;

namespace AnimalKingdom.Models
{
    public class Animal
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        [NotMapped]
        public IFormFile? Photo { get; set; }
        public string? SavedUrl { get; set; } //Storing The Image Url into SQL Server

        [NotMapped]
        public string? SignedUrl { get; set; } //Storing The Image URL into Google Cloud Server
        public string? SavedFileName { get; set; } //FileName
    }
}
