using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TaiMvc.Models;
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

        
    }
}