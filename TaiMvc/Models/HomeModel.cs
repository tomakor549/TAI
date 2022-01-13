namespace TaiMvc.Models
{
    public class HomeModel
    {
        public List<string>? List { get; set; }

        public HomeModel()
        {
            List = new List<string>();
        }
    }
}
