using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TaiMvc.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Web;
using Abp.AspNetCore.Mvc.Authorization;


namespace TaiMvc.Controllers
{
    
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;
        private Microsoft.AspNetCore.Hosting.IHostingEnvironment _env;
        private string _dir;


        public HomeController(ILogger<HomeController> logger, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            _logger = logger;
            _env = env;
            _dir = _env.ContentRootPath;
        }

        public IActionResult Index()
        {
            return View();
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

        //tu zaczyna się upload normalny
        public IActionResult Operations() => View();
        public IActionResult OperationUpload(IFormFile file)
        {
            using (var fileStream = new FileStream("path", FileMode.Create, FileAccess.Write))
            {
                if (file != null)
                {
                    file.CopyTo(fileStream);
                }
            }
            return RedirectToAction("Operations");
        }
    }
}