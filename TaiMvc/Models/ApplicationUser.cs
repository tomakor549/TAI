using Microsoft.AspNetCore.Identity;

namespace TaiMvc.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Localization { get; set;}


    }
}
