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

namespace TaiMvc.Controllers
{
    [Authorize]
    public class FileOperationController : Controller
    {
        private const string _encryptionPassword = "Haslo";

        private readonly UserManager<ApplicationUser> _userManager;

        private Stopwatch stopWatch = new Stopwatch();

        private readonly ILogger<FileOperationController> _logger;

        private readonly IWebHostEnvironment webHostEnvironment;

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
            if (actionName != null)
            {
                if (actionName.Contains("DownloadFile") || actionName.Contains("OperationUpload") || actionName.Contains("StreamDownloadFile2") || actionName.Contains("FileUpload"))
                {
                    stopWatch.Stop();
                    var time = stopWatch.ElapsedMilliseconds;
                    _logger.LogInformation("Time: " + time.ToString() + "ms");
                    Debug.WriteLine("Time: " + time.ToString() + "ms");
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

        //normal short version stream download file
        public FileStreamResult StreamDownloadFile(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);

            Response.Headers.Add("content-disposition", "attachment; filename=" + fileName);
            //bufferSize 4096
            return File(new MyFileStream(path, FileMode.Open), "application/octet-stream");

            //other
            //return File(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096),
            //"application/octet-stream");

        }

        //abnormal long version stream download file - greater user control 
        public async Task StreamDownloadFile2(string fileName)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            var path = Path.Join(user.Localization, fileName);
            fileName = fileName.Remove(fileName.Length - 4);

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

        public IActionResult OperationUpload(IFormFile file)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            if (file != null)
            {
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
        public IActionResult OperationUploadEncryption(IFormFile file)
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            if (file != null)
            {
                var path = Path.Join(user.Localization, file.FileName);
                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    file.CopyToAsync(fileStream);
                }
            }
            else
            {
                ViewData["Message"] = "Wybierz jakiś plik do uploadu";
            }

            return RedirectToAction("Operations");
        }
        //stream upload
        //[HttpPost("UploadFile")]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> UploadFile(IEnumerable<IFormFile> iFormFile)
        //{
        //    var user = _userManager.GetUserAsync(HttpContext.User).Result;

        //    if (iFormFile == null)
        //    {
        //        ViewData["Message"] = "Wybierz jakiś plik do uploadu";
        //    }
        //    else
        //    {
        //        foreach (var file in iFormFile)
        //        {
        //            var fileContent = ContentDispositionHeaderValue.Parse(file.ContentDisposition);
        //            var path = Path.Join(user.Localization, fileContent.FileName);
        //            using (var fileStream = new FileStream(path, FileMode.Create))
        //            {
        //                await file.CopyToAsync(fileStream);
        //            }

        //        }
        //    }

        //    return  RedirectToAction("Operations");
        //}


        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> UploadFile()
        {
            var user = _userManager.GetUserAsync(HttpContext.User).Result;
            FormValueProvider formModel;
            var file = HttpContext.Request.Form.Files[0];
            string path = Path.Join(user.Localization, file.FileName);
            using (var stream = new FileStream (path, FileMode.Create))
            {
                formModel = await Request.StreamFile(stream);
                
            }

            var viewModel = new MyViewModel();

            var bindingSuccessful = await TryUpdateModelAsync(viewModel, prefix: "",
               valueProvider: formModel);

            if (!bindingSuccessful)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(formModel);
                }
            }
            return Ok(formModel);

        }
    }
    
}
