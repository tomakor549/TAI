namespace TaiMvc.Models
{
    public class HomeModel
    {
        public List<string>? List { get; set; }

        public HomeModel()
        {
            List = new List<string>();
        }


        /// thid added now - nothing changed 24.01.2022
        public string Username { get; set; }
    }
}
