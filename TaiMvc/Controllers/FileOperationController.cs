
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using TaiMvc.Models;
using TaiMvc.SpecialOperation.StreamUploadBinding;
using TaiMvc.Utilities;

namespace TaiMvc.Controllers
{
    [Authorize]
    [RequestSizeLimit(1509715200)]
    public class FileOperationController : Controller
    {
        private const string _encryptionPassword = "Haslo";

        private readonly UserManager<ApplicationUser> _userManager;

        private Stopwatch stopWatch = new Stopwatch();

        private readonly ILogger<FileOperationController> _logger;

        private long lastFileSize;

        private readonly int operationsBuffer = 10 * 1024;

        public FileOperationController(ILogger<FileOperationController> logger, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _logger = logger;
            lastFileSize = 0;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string? actionName = filterContext.ActionDescriptor.DisplayName;
            if (actionName != null)
            {
                if (actionName.Contains("DownloadFile") ||
                    actionName.Contains("DownloadEncodingFile") ||
                    actionName.Contains("StreamDownloadFile") ||
                    actionName.Contains("StreamEncodingDownloadFile") ||
                    actionName.Contains("OperationUpload") ||
                    actionName.Contains("OperationUploadEncryption") ||
                    actionName.Contains("StreamUpload") ||
                    actionName.Contains("StreamEncryptionUpload")) 
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
                string name;
                if (actionName.Contains(name = "DownloadFile") ||
                    actionName.Contains(name = "DownloadEncodingFile") ||
                    actionName.Contains(name = "StreamDownloadFile") ||
                    actionName.Contains(name = "StreamEncodingDownloadFile") ||
                    actionName.Contains(name = "OperationUpload") ||
                    actionName.Contains(name = "OperationUploadEncryption") ||
                    actionName.Contains(name = "StreamUpload") ||
                    actionName.Contains(name = "StreamEncryptionUpload"))
                {
                    stopWatch.Stop();

                    var time = stopWatch.ElapsedMilliseconds;
                    string message = $"{name}:\nTime: {time}ms, file size: {lastFileSize}B";
                    Console.WriteLine(message);
                    _logger.LogInformation(message);

                    string fileName = "OperationsData.txt";
                    var path = Path.Join(Environment.CurrentDirectory, fileName);
                    if (!System.IO.File.Exists(path))
                        System.IO.File.Create(fileName).Dispose();
                    try
                    {
                        using (var file = new StreamWriter(path, append: true))
                        {
                            if (file == null)
                                return;
                            file.WriteLine(message);
                            file.Close();
                        }
                    }
                    catch(UnauthorizedAccessException ex) 
                    {
                        Console.WriteLine(ex.Message);
                        _logger.LogInformation(ex.Message);
                    }

                    
                }
            }
        }

        public FileResult? DownloadFile(string fileName)
        {
            lastFileSize = -1;
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            lastFileSize = new FileInfo(path).Length;

            byte[] bytes = System.IO.File.ReadAllBytes(path);

            //Send the File to Download.
            return File(bytes, "application/octet-stream", fileName);
        }

        public FileResult? DownloadEncodingFile(string fileName)
        {
            lastFileSize = -1;
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            lastFileSize = new FileInfo(path).Length;
            byte[] bytes;

            bytes = FileEncryptionOperations.FileDecrypt(path, _encryptionPassword);
            fileName = fileName.Remove(fileName.Length - 4);

            //Send the File to Download.
            return File(bytes, "application/octet-stream", fileName);
        }

        //abnormal long version stream download file - greater user control 
        public async Task StreamDownloadFile(string fileName)
        {
            lastFileSize = -1;
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            lastFileSize = new FileInfo(path).Length;

            this.Response.StatusCode = 200;
            this.Response.Headers.Add(HeaderNames.ContentDisposition, $"attachment; filename=\"{fileName}\"");
            this.Response.Headers.Add(HeaderNames.ContentType, "application/octet-stream");
            var inputStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var outputStream = this.Response.Body;
            int bufferSize = operationsBuffer;
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
            lastFileSize = -1;
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            lastFileSize = new FileInfo(path).Length;
            fileName = fileName.Remove(fileName.Length - 4);

            this.Response.StatusCode = 200;
            this.Response.Headers.Add(HeaderNames.ContentDisposition, $"attachment; filename=\"{fileName}\"");
            this.Response.Headers.Add(HeaderNames.ContentType, "application/octet-stream");
            var inputStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var outputStream = this.Response.Body;
            int bufferSize = operationsBuffer;

            FileEncryptionOperations.FileDecrypt(ref inputStream, ref outputStream, _encryptionPassword, bufferSize);
            await outputStream.FlushAsync();
        }


        /*    ALL UPLOADS      */
        //upload traditional
        public IActionResult Operations() => View();

        [RequestFormLimits(BufferBodyLengthLimit = 1509715200)]
        public IActionResult OperationUpload(IFormFile file)
        {
            lastFileSize = -1;
            if (file != null)
            {
                var user = _userManager.GetUserAsync(HttpContext.User).Result;
                
                string path = FileHelpers.GetNonExistPath(user.Localization, file.FileName);

                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                }
                lastFileSize = file.Length;
            }

            return RedirectToAction("Operations");
        }
        //Encryption Upload
        [RequestFormLimits(BufferBodyLengthLimit = 1509715200)]
        public IActionResult OperationUploadEncryption(IFormFile file)
        {
            lastFileSize = -1;

            if (file != null)
            {
                var user = _userManager.GetUserAsync(HttpContext.User).Result;
                string path = FileHelpers.GetNonExistPath(user.Localization, file.FileName);
                FileEncryptionOperations.SaveFileEncrypt(path, _encryptionPassword, file);
                lastFileSize = file.Length;
            }
            return RedirectToAction("Operations");
        }

        public class MyViewModel
        {
            public string Username { get; set; }
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> StreamUpload()
        {
            lastFileSize = -1;
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var lastPath = await FileStreamingHelper.StreamFiles(Request, user.Localization, operationsBuffer);
            lastFileSize = new FileInfo(lastPath).Length;
            return RedirectToAction("Operations");
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> StreamEncryptionUpload()
        {
            lastFileSize = -1;
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var lastPath = await FileStreamingHelper.StreamEncodingFiles(Request, user.Localization, _encryptionPassword, operationsBuffer);
            lastFileSize = new FileInfo(lastPath).Length;
            return RedirectToAction("Operations");
        }

    }

}