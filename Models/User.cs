using System.ComponentModel.DataAnnotations;

namespace MonAppGestion.Models
{
    public class User
    {
        [Key] 
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Rank { get; set; } = "User";
    }
}