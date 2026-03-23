using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pdf_To_Png.Models;
using Pdf_To_Png.Services;

namespace Pdf_To_Png.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/pdf-to-png")]
    public class PdfToPngController : Controller
    {
        private readonly IPdfToPngService _pdfToPngService;

        public PdfToPngController(IPdfToPngService pdfToPngService)
        {
            _pdfToPngService = pdfToPngService;
        }

        [HttpPost("convert")]
        public async Task<IActionResult> ConvertPdfToPng([FromForm] PdfToPngModel request)
        {
            if (request.File == null)
            {
                return BadRequest("No file uploaded.");
            }
            try
            {
                var pngBytesList = await _pdfToPngService.ConvertPdfToPng(request.File);
                return Ok(pngBytesList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
