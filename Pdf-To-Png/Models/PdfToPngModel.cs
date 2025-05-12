using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace Pdf_To_Png.Models
{
    public class PdfToPngModel
    {

        [FromForm(Name = "file")]
        public IFormFile File { get; set; }
    }
}
