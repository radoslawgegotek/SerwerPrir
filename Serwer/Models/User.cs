namespace Serwer.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public ICollection<FileRecord> Files { get; set; }
    }
}
