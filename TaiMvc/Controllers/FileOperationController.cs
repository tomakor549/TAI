
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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Runtime.Remoting;
using System.IO;

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

        public int count = 0;

        public int count2;
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string? actionName = filterContext.ActionDescriptor.DisplayName;
            if (actionName != null)
            {
                if (actionName.Contains("DownloadFile") ||
                    actionName.Contains("DownloadEncodingFile") ||
                    actionName.Contains("StreamDownloadFile") ||
                    actionName.Contains("StreamEncodingDownloadFile") ||
                    actionName.Contains("UploadFile"))
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
                    Console.WriteLine(message);
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

        private static string GetNonExistPath(string localization, string fileName)
        {
            var path = Path.Join(localization, fileName);
            string fileNameOnly, extension, newFilename, newPath = path;
            int count = 1;
            while (System.IO.File.Exists(newPath))
            {
                fileNameOnly = Path.GetFileNameWithoutExtension(path);
                extension = Path.GetExtension(path);
                newFilename = string.Format("{0}({1}){2}", fileNameOnly, count, extension);
                newPath = Path.Join(localization, newFilename);
                count++;
            }
            return newPath;
        }
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
        public IActionResult OperationUpload(IFormFile file)
        {
            if(file != null)
            {
                var user = _userManager.GetUserAsync(HttpContext.User).Result;

                string pathToCheck = GetNonExistPath(user.Localization, file.FileName);

                using (var fileStream = new FileStream(pathToCheck, FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                }
                ViewData["UploadStatus"] = "plik wgrano - jupiii";
            }
            else
            {
                ViewData["UploadStatus"] = "plik wgrano - jupiii";
            }

            return RedirectToAction("Operations");
        }
        //Encryption Upload
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
        public IActionResult OperationUploadEncryption(IFormFile file)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;

            if (file != null)
            {
                string path = GetNonExistPath(user.Localization, file.FileName);
                FileEncryptionOperations.SaveFileEncrypt(path, _encryptionPassword, file);
                ViewData["UploadStatus"] = "plik wgrano - jupiii";
            }
            else
            {
                ViewData["UploadStatus"] = "plik wgrano - jupiii";
            }

            return RedirectToAction("Operations");
        }
    }

}