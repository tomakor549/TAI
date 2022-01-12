using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using TaiMvc.Models;

namespace TaiMvc.Controllers
{
    public class FileOperationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public FileOperationController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public FileResult? DownloadFile(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            var len = new System.IO.FileInfo(path).Length;
            if (len > 2000000000)
                return null;
            //Read the File data into Byte Array.
            byte[] bytes = System.IO.File.ReadAllBytes(path);

            //Send the File to Download.
            return File(bytes, "application/octet-stream", fileName);
        }

        public FileStreamResult StreamDownload(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);

            Response.Headers.Add("content-disposition", "attachment; filename="+fileName);
            return File(new FileStream(path, FileMode.Open),
                        "application/octet-stream");
        }


    }
}
