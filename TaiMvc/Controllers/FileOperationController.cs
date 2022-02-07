using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using TaiMvc.Models;
using TaiMvc.SpecialOperation;
using System.Web;

namespace TaiMvc.Controllers
{
    [Authorize]
    public class FileOperationController : Controller
    {
        private const string _encryptionPassword = "Haslo";

        private readonly UserManager<ApplicationUser> _userManager;

        private Stopwatch stopWatch = new Stopwatch();

        private readonly ILogger<FileOperationController> _logger;

        private long lastFileSize;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string? actionName = filterContext.ActionDescriptor.DisplayName;
            if (actionName != null)
            {
                if (actionName.Contains("DownloadFile") || actionName.Contains("OperationUpload") || actionName.Contains("StreamDownloadFile2"))
                {
                    stopWatch.Reset();
                    stopWatch.Start();
                }
            }
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            string? actionName = filterContext.ActionDescriptor.DisplayName;

            string fileName = "Time operations.txt";
            var path = Path.Join(Environment.CurrentDirectory, fileName);
            if (actionName != null)
            {
                string name;
                if (actionName.Contains(name = "DownloadFile") ||
                    actionName.Contains(name = "DownloadEncodingFile") ||
                    actionName.Contains(name = "StreamDownloadFile") ||
                    actionName.Contains(name = "StreamEncodingDownloadFile") ||
                    actionName.Contains(name = "UploadFile"))
                {
                    if (!System.IO.File.Exists(path))
                        System.IO.File.Create(fileName).Dispose();

                    using StreamWriter file = new(path, append: true);

                    stopWatch.Stop();

                    var time = stopWatch.ElapsedMilliseconds;
                    string message = $"{name}:\nTime: {time.ToString()}ms, file size: {lastFileSize}B";
                    //message do analizy danych
                    //string message = $"{name}\t{time.ToString()}\t{lastFileSize}";
                    file.WriteLine(message);
                    file.Close();
                }
            }
        }

        public FileOperationController(ILogger<FileOperationController> logger, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _logger = logger;
            lastFileSize = 0;
        }

        public FileResult? DownloadFile(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            lastFileSize = new FileInfo(path).Length;
            if (lastFileSize > 2000000000)
                return null;
            byte[] bytes = System.IO.File.ReadAllBytes(path);

            //Send the File to Download.
            return File(bytes, "application/octet-stream", fileName);
        }

        public FileResult? DownloadEncodingFile(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            lastFileSize = new FileInfo(path).Length;
            if (lastFileSize > 2000000000)
                return null;
            byte[] bytes;

            bytes = FileEncryptionOperations.FileDecrypt(path, _encryptionPassword);
            fileName = fileName.Remove(fileName.Length - 4);

            //Send the File to Download.
            return File(bytes, "application/octet-stream", fileName);
        }

        //abnormal long version stream download file - greater user control 
        public async Task StreamDownloadFile(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            lastFileSize = new FileInfo(path).Length;

            this.Response.StatusCode = 200;
            this.Response.Headers.Add(HeaderNames.ContentDisposition, $"attachment; filename=\"{fileName}\"");
            this.Response.Headers.Add(HeaderNames.ContentType, "application/octet-stream");
            var inputStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var outputStream = this.Response.Body;
            const int bufferSize = 1024;
            var buffer = new byte[bufferSize];
            while (true)
            {
                var bytesRead = await inputStream.ReadAsync(buffer, 0, bufferSize);
                if (bytesRead == 0) break;
                await outputStream.WriteAsync(buffer, 0, bytesRead);
            }
            await outputStream.FlushAsync();
        }

        //source: https://dogschasingsquirrels.com/2020/06/02/streaming-a-response-in-net-core-webapi/
        public async Task StreamEncodingDownloadFile(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            lastFileSize = new FileInfo(path).Length;
            fileName = fileName.Remove(fileName.Length - 4);

            this.Response.StatusCode = 200;
            this.Response.Headers.Add(HeaderNames.ContentDisposition, $"attachment; filename=\"{fileName}\"");
            this.Response.Headers.Add(HeaderNames.ContentType, "application/octet-stream");
            var inputStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var outputStream = this.Response.Body;
            const int bufferSize = 1024;

            FileEncryptionOperations.FileDecrypt(ref inputStream, ref outputStream, _encryptionPassword, bufferSize);
            await outputStream.FlushAsync();
        }


        /*    ALL UPLOADS      */


        //upload traditional
        public IActionResult Operations() => View();

        public IActionResult UploadFile(IFormFile file)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            if (file != null)
            {
                lastFileSize = file.Length;
                var path = Path.Join(user.Localization, file.FileName);
                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                }
            }
            else
            {
                ViewData["Message"] = "Wybierz jakiś plik do uploadu";
            }
            return RedirectToAction("Operations");
        }
        //Encryption Upload
        public IActionResult UploadEncryptionFile(IFormFile file)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            if (file != null)
            {
                lastFileSize = file.Length;
                var path = Path.Join(user.Localization, file.FileName);
                FileEncryptionOperations.SaveFileEncrypt(path, _encryptionPassword, file);
            }
            else
            {
                ViewData["Message"] = "Wybierz jakiś plik do uploadu";
            }

            return RedirectToAction("Operations");
        }
    }
}
