
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
using System.Security.Cryptography;
using System.Text;
using TaiMvc.Models;
using TaiMvc.SpecialOperation.StreamUploadBinding;
using TaiMvc.Utilities;

namespace TaiMvc.Controllers
{
    [Authorize]
    [RequestSizeLimit(1509715200)]
    public class FileOperationController : Controller
    {
        private string _encryptionPassword = "Haslo234";

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
                    actionName.Contains("StreamEncryptionUpload") ||
                    actionName.Contains("UploadLargeFile")) 
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
                //koleność ma znaczenie xD
                if (actionName.Contains(name = "StreamEncodingDownloadFile") || 
                    actionName.Contains(name = "DownloadFile") ||
                    actionName.Contains(name = "DownloadEncodingFile") ||
                    actionName.Contains(name = "StreamDownloadFile") ||
                    actionName.Contains(name = "OperationUploadEncryption") ||
                    actionName.Contains(name = "OperationUpload") ||
                    actionName.Contains(name = "StreamUpload") ||
                    actionName.Contains(name = "StreamEncryptionUpload") ||
                    actionName.Contains(name = "UploadLargeFile"))
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

        public ActionResult setPassword(string passwd)
        {
            _encryptionPassword = passwd;
            return RedirectToAction("Operations");
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

            byte[] passwords = Encoding.UTF8.GetBytes(_encryptionPassword);
            byte[] salt = new byte[32];
            using (FileStream fsCrypt = inputStream)
            {
                fsCrypt.Read(salt, 0, salt.Length);
                Aes AES = Aes.Create();
                AES.KeySize = 256;//aes 256 bit encryption c#
                AES.BlockSize = 128;//aes 128 bit encryption c#
                var key = new Rfc2898DeriveBytes(passwords, salt, 50000);
                AES.Key = key.GetBytes(AES.KeySize / 8);
                AES.IV = key.GetBytes(AES.BlockSize / 8);
                AES.Padding = PaddingMode.PKCS7;
                AES.Mode = CipherMode.CFB;
                using (CryptoStream cryptoStream = new(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (outputStream)
                    {
                        int bytesRead;
                        byte[] buffer = new byte[bufferSize];
                        while ((bytesRead = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            await outputStream.WriteAsync(buffer, 0, bytesRead);
                            await outputStream.FlushAsync();
                        }
                    }
                }
            }
            //await outputStream.FlushAsync();
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

        //public async Task<IActionResult> newStreamUpload()
        //{
        //    var user = _userManager.GetUserAsync(HttpContext.User).Result;
        //    var reader = new FormReader(Request.Body);
        //    var section = await reader.ReadFormAsync();
        //    var defaultFormOptions = new FormOptions();
        //    var data = MultipartRequestHelper.GetBoundary(
        //        MediaTypeHeaderValue.Parse(Request.ContentType),
        //        defaultFormOptions.ValueLengthLimit);

        //    var size = ((int)Request.ContentLength);
        //    using (var fs = new FileStream("C:\\Users\\venet\\Desktop\\test.txt", FileMode.Create))//bazowo 4096 bajtów na rozmiar buffera
        //    {
        //        using (Stream fsIn = Request.HttpContext.Request.Body)
        //        {
        //            var buffer = new byte[size];
        //            int read;
        //            while ((read = await fsIn.ReadAsync(buffer, 0, buffer.Length)) > 0)
        //            {
        //                fs.Write(buffer, 0, read);
        //            }
        //        }
        //    }

        //    return RedirectToAction("Operations");
        //}

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

        /// <summary>
        /// Action for upload large file
        /// </summary>
        /// <remarks>
        /// Request to this action will not trigger any model binding or model validation,
        /// because this is a no-argument action
        /// </remarks>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UploadLargeFile()
        {
            var request = HttpContext.Request;

            // validation of Content-Type
            // 1. first, it must be a form-data request
            // 2. a boundary should be found in the Content-Type
            if (!request.HasFormContentType ||
                !MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaTypeHeader) ||
                string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value))
            {
                return new UnsupportedMediaTypeResult();
            }

            var reader = new MultipartReader(mediaTypeHeader.Boundary.Value, request.Body);
            var section = await reader.ReadNextSectionAsync();

            // This sample try to get the first file from request and save it
            // Make changes according to your needs in actual use
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition,
                    out var contentDisposition);

                if (hasContentDispositionHeader && contentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    // Don't trust any file name, file extension, and file data from the request unless you trust them completely
                    // Otherwise, it is very likely to cause problems such as virus uploading, disk filling, etc
                    // In short, it is necessary to restrict and verify the upload
                    // Here, we just use the temporary folder and a random file name

                    // Get the temporary folder, and combine a random file name with it
                    var fileName = Path.GetRandomFileName();
                    var saveToPath = Path.Combine(Path.GetTempPath(), fileName);


                    var user = _userManager.GetUserAsync(HttpContext.User).Result;
                    string path = FileHelpers.GetNonExistPath(user.Localization, fileName);

                    using (var targetStream = System.IO.File.Create(path))
                    {
                        await section.Body.CopyToAsync(targetStream, bufferSize: operationsBuffer);
                    }

                    return Ok();
                }

                section = await reader.ReadNextSectionAsync();
            }

            // If the code runs to this location, it means that no files have been saved
            return BadRequest("No files data in the request.");
        }

    }

}