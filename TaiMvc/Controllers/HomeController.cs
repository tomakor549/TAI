using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TaiMvc.Data;
using TaiMvc.Models;

namespace TaiMvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        readonly ApplicationDbContext db;

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            if(userId == null)
            {
                return View(null);
            }
            var user = _userManager.FindByIdAsync(userId).Result;
            var list = GetFilesList(user.Localization);
            return View(list);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public FileResult DownloadFile(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            //Read the File data into Byte Array.
            byte[] bytes = System.IO.File.ReadAllBytes(path);

            //Send the File to Download.
            return File(bytes, "application/octet-stream", "filename.pdf");
        }

        private static List<string>? GetFilesList(string path)
        {
            var files = new List<string>();

            if (!Directory.Exists(path))
                return null;

            var filesPath = Directory.GetFiles(path);
            if(filesPath.Length <= 0)
                return null;

            foreach (var name in filesPath)
            {
                string[] subs = name.Split('\\', '/');
                files.Add(subs.Last());
            }

            return files;
        }
    }
}