using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using TaiMvc.Models;
using TaiMvc.SpecialOperation;

namespace TaiMvc.Controllers
{
    public class FileOperationController : Controller
    {
        private const string _encryptionPassword = "Haslo";

        private readonly UserManager<ApplicationUser> _userManager;

        private Stopwatch stopWatch = new Stopwatch();

        private readonly ILogger<FileOperationController> _logger;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string? actionName = filterContext.ActionDescriptor.DisplayName;
            if (actionName != null)
            {
                if (actionName.Contains("DownloadFile") || actionName.Contains("StreamDownload"))
                {
                    stopWatch.Reset();
                    stopWatch.Start();
                }
            }
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            string? actionName = filterContext.ActionDescriptor.DisplayName;
            if (actionName != null)
            {
                if (actionName.Contains("DownloadFile") || actionName.Contains("StreamDownload"))
                {
                    stopWatch.Stop();
                    var time = stopWatch.ElapsedMilliseconds;
                    _logger.LogInformation("Time: " + time.ToString() + "ms");
                    System.Diagnostics.Debug.WriteLine("Time: " + time.ToString() + "ms");
                }
            }
        }
        public FileOperationController(ILogger<FileOperationController> logger, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public FileResult? DownloadFile(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            var len = new FileInfo(path).Length;
            if (len > 2000000000)
                return null;
            byte[] bytes;
            if (fileName.EndsWith(".aes"))
            {
                bytes = FileEncryptionOperations.FileDecrypt(path, _encryptionPassword);
                fileName = fileName.Remove(fileName.Length - 4);
            }
            else
                bytes = System.IO.File.ReadAllBytes(path);

            //Send the File to Download.
            return File(bytes, "application/octet-stream", fileName);
        }

        public FileStreamResult StreamDownload(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);

            Response.Headers.Add("content-disposition", "attachment; filename=" + fileName);

            long time = 0;
            //bufferSize 4096
            return File(new MyFileStream(path, FileMode.Open), "application/octet-stream");

            //other
            //return File(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096),
            //"application/octet-stream");

        }

        //tu zaczyna się upload normalny
        public IActionResult Operations() => View();
        public IActionResult OperationUpload(IFormFile file)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, file.FileName);
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                if (file != null)
                {
                    file.CopyTo(fileStream);
                }
            }
            return RedirectToAction("Operations");
        }
        //Encryption Download
        public IActionResult OperationUploadEncryption(IFormFile file)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, file.FileName);
            FileEncryptionOperations.FileEncrypt(path, "Haslo", file);
            return RedirectToAction("Operations");
        }
    }
}
