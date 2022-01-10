using Microsoft.AspNetCore.Mvc;

namespace TaiMvc.Controllers
{
    public class OperationsController1cs : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
