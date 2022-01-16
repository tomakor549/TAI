using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace TaiMvc.SpecialOperation
{
    public class MyFileStreamResult : FileStreamResult
    {
        public MyFileStreamResult(Stream fileStream, string contentType) : base(fileStream, contentType)
        {
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            IActionResultExecutor<MyFileStreamResult>? executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<MyFileStreamResult>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
