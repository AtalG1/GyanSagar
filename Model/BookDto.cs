using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GyanSagarNew.Model
{
    public class AddBookDto
    {
        [Required] public string Title { get; set; }
        [Required] public string Author { get; set; }
        [Required] public string Genre { get; set; }
        [Required] public string ISBN { get; set; }
        [Required] public decimal Price { get; set; }
        [Required] public string Language { get; set; }
        [Required] public string Format { get; set; }
        [Required] public string Publisher { get; set; }
        [Required] public int StockAvailability { get; set; }

        public IFormFile? BookImage { get; set; } // Optional
    }
}
