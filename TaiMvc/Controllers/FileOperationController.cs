
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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

        private readonly long _fileSizeLimit = 1509715200;
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        private readonly string[] _permittedExtensions = { ".txt" };

        private readonly UserManager<ApplicationUser> _userManager;

        private Stopwatch stopWatch = new Stopwatch();

        private readonly ILogger<FileOperationController> _logger;

        private long lastFileSize;

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
        [RequestFormLimits(BufferBodyLengthLimit = 1509715200)]
        public IActionResult OperationUpload(IFormFile file)
        {
            if(file != null)
            {
                var user = _userManager.GetUserAsync(HttpContext.User).Result;

                string path = GetNonExistPath(user.Localization, file.FileName);

                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                }
            }

            return RedirectToAction("Operations");
        }
        //Encryption Upload
        [RequestFormLimits(BufferBodyLengthLimit = 1509715200)]
        public IActionResult OperationUploadEncryption(IFormFile file)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;

            if (file != null)
            {
                string path = GetNonExistPath(user.Localization, file.FileName);
                FileEncryptionOperations.SaveFileEncrypt(path, _encryptionPassword, file);
            }
            return RedirectToAction("Operations");
        }

        [RequestFormLimits(BufferBodyLengthLimit = 1509715200)]
        public async Task StreamUpload(IFormFile file)
        {
            if (file != null)
            {
                var user = _userManager.GetUserAsync(HttpContext.User).Result;
                string path = GetNonExistPath(user.Localization, file.FileName);

                byte[] buffer = new byte[16 * 1024];
                long totalBytes = file.Length;
                using FileStream output = System.IO.File.Create(path);
                using Stream input = file.OpenReadStream();
                int totalReadBytes = 0;
                int readBytes;

                while((readBytes = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    await output.WriteAsync(buffer, 0, readBytes);
                    totalReadBytes+=readBytes;
                    int progress=(int)((float)totalReadBytes/ (float)totalBytes * 100.0);
                }
            }
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhysical()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File",
                    $"The request couldn't be processed (Error 1).");
                // Log error

                return BadRequest(ModelState);
            }

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    // This check assumes that there's a file
                    // present without form data. If form data
                    // is present, this method immediately fails
                    // and returns the model error.
                    if (!MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        ModelState.AddModelError("File",
                            $"The request couldn't be processed (Error 2).");
                        // Log error

                        return BadRequest(ModelState);
                    }
                    else
                    {
                        var user = _userManager.GetUserAsync(HttpContext.User).Result;
                        string path = GetNonExistPath(user.Localization, contentDisposition.FileName.Value);

                        var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                    section, contentDisposition, ModelState,
                    _permittedExtensions, _fileSizeLimit);


                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        using (var targetStream = System.IO.File.Create(path))
                        {
                            await targetStream.WriteAsync(streamedFileContent);
                        }
                    }
                }

                // Drain any remaining section body that hasn't been consumed and
                // read the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            return Created(nameof(FileOperationController), null);
        }
    }

}