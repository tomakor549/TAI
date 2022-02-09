using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TaiMvc.Models;
using TaiMvc.SpecialOperation.StreamUploadBinding;

namespace TaiMvc.Controllers
{
    public class HomeController : Controller
    {
        private HomeModel _homeModel;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
            _homeModel = new HomeModel();
        }

        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            if(userId == null)
            {
                return View(null);
            }
            var user = _userManager.FindByIdAsync(userId).Result;
            _homeModel.List = GetFilesList(user.Localization);
            return View(_homeModel);
        }

        public IActionResult Privacy()
        {
            return View();
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