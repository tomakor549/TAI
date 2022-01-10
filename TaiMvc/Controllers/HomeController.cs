using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TaiMvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace TaiMvc.Controllers
{
    public class HomeController : Controller
    {
		private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Operations()
        {
			return View();
		}
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

		[HttpPost]
		public IActionResult Index(IFormFile[] files)
		{
			// Iterate each files
			foreach (var file in files)
			{
				// Get the file name from the browser
				var fileName = System.IO.Path.GetFileName(file.FileName);

				// Get file path to be uploaded
				var filePath = Path.Combine(Directory.GetCurrentDirectory(), "upload", fileName);

				// Check If file with same name exists and delete it
				if (System.IO.File.Exists(filePath))
				{
					System.IO.File.Delete(filePath);
				}

				// Create a new local file and copy contents of uploaded file
				using (var localFile = System.IO.File.OpenWrite(filePath))
				using (var uploadedFile = file.OpenReadStream())
				{
					uploadedFile.CopyTo(localFile);
				}
			}
			ViewBag.Message = "Files are successfully uploaded";

           // Get files from the server

           var model = new FilesViewModel();
            foreach (var item in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "upload")))
            {
                model.Files.Add(
                    new FileDetails { Name = System.IO.Path.GetFileName(item), Path = item });
            }
            return View(model);
        }
		// Download file from the server
		public async Task<IActionResult> Download(string filename)
		{
			if (filename == null)
				return Content("filename is not availble");

			var path = Path.Combine(Directory.GetCurrentDirectory(), "upload", filename);

			var memory = new MemoryStream();
			using (var stream = new FileStream(path, FileMode.Open))
			{
				await stream.CopyToAsync(memory);
			}
			memory.Position = 0;
			return File(memory, GetContentType(path), Path.GetFileName(path));
		}

		// Get content type
		private string GetContentType(string path)
		{
			var types = GetMimeTypes();
			var ext = Path.GetExtension(path).ToLowerInvariant();
			return types[ext];
		}

		// Get mime types
		private Dictionary<string, string> GetMimeTypes()
		{
			return new Dictionary<string, string>
	{
		{".txt", "text/plain"},
		{".pdf", "application/pdf"},
		{".doc", "application/vnd.ms-word"},
		{".docx", "application/vnd.ms-word"},
		{".xls", "application/vnd.ms-excel"},
		{".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
		{".png", "image/png"},
		{".jpg", "image/jpeg"},
		{".jpeg", "image/jpeg"},
		{".gif", "image/gif"},
		{".csv", "text/csv"}
	};
		}
	}
}