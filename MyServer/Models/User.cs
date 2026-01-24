namespace MyServer.Models
{
    public class User
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public Role Role { get; set; }
        public User(string email, string password, Role role)
        {
            Email = email;
            Password = password;
            Role = role;
        }
    }
}
