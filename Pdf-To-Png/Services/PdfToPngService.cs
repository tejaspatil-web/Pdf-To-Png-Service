using System.Diagnostics;

namespace Pdf_To_Png.Services
{
    public interface IPdfToPngService
    {
        Task<List<byte[]>> ConvertPdfToPng(IFormFile pdfFile);
    }
    public class PdfToPngService: IPdfToPngService
    {
        public async Task<List<byte[]>> ConvertPdfToPng(IFormFile pdfFile)
        {
            var imageByteArrays = new List<byte[]>();

            var tempPdfPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".pdf");
            using (var stream = new FileStream(tempPdfPath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(stream);
            }

            string outputPrefix = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            const string pdftoppmPath = "pdftoppm";
            //string outputPrefix = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            //var pdftoppmPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"poppler\Library\bin\pdftoppm.exe");

            var startInfo = new ProcessStartInfo
            {
                FileName = pdftoppmPath,
                Arguments = $"-r 144 -png \"{tempPdfPath}\" \"{outputPrefix}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();

                string stderr = process.StandardError.ReadToEnd();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"pdftoppm failed: {stderr}");
                }
            }

            string directory = Path.GetDirectoryName(outputPrefix)!;
            string filePrefix = Path.GetFileName(outputPrefix);
            var imageFiles = Directory.GetFiles(directory, filePrefix + "-*.png").OrderBy(f => f);

            foreach (var imageFile in imageFiles)
            {
                var bytes = await File.ReadAllBytesAsync(imageFile);
                imageByteArrays.Add(bytes);
            }

            return imageByteArrays;
        }
    }
}
