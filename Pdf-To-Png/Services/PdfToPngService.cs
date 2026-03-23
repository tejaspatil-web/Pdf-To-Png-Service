using System.Diagnostics;

namespace Pdf_To_Png.Services
{
    public interface IPdfToPngService
    {
        Task<List<byte[]>> ConvertPdfToPng(IFormFile pdfFile);
    }

    public class PdfToPngService : IPdfToPngService
    {
        private const string PdftoppmPath = "/usr/bin/pdftoppm";

        public async Task<List<byte[]>> ConvertPdfToPng(IFormFile pdfFile)
        {
            var imageByteArrays = new List<byte[]>();

            if (pdfFile == null || pdfFile.Length == 0)
                throw new ArgumentException("Invalid PDF file.");

            if (!File.Exists(PdftoppmPath))
                throw new Exception("pdftoppm is not installed in container.");

            //Temp paths
            var tempPdfPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
            var outputPrefix = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                //Save uploaded PDF
                using (var stream = new FileStream(tempPdfPath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }

                //Start process
                var startInfo = new ProcessStartInfo
                {
                    FileName = PdftoppmPath,
                    Arguments = $"-r 144 -png \"{tempPdfPath}\" \"{outputPrefix}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };

                process.Start();

                //Read streams asynchronously
                var stdErrorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var stderr = await stdErrorTask;

                if (process.ExitCode != 0)
                {
                    throw new Exception($"pdftoppm failed: {stderr}");
                }

                //Read generated images
                string directory = Path.GetDirectoryName(outputPrefix)!;
                string filePrefix = Path.GetFileName(outputPrefix);

                var imageFiles = Directory
                    .GetFiles(directory, filePrefix + "-*.png")
                    .OrderBy(f => f);

                foreach (var imageFile in imageFiles)
                {
                    var bytes = await File.ReadAllBytesAsync(imageFile);
                    imageByteArrays.Add(bytes);
                }

                return imageByteArrays;
            }
            finally
            {
                //Cleanup temp PDF
                if (File.Exists(tempPdfPath))
                    File.Delete(tempPdfPath);

                //Cleanup generated PNGs
                var directory = Path.GetDirectoryName(outputPrefix);
                if (directory != null)
                {
                    var files = Directory.GetFiles(directory, Path.GetFileName(outputPrefix) + "-*.png");
                    foreach (var file in files)
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
            }
        }
    }
}