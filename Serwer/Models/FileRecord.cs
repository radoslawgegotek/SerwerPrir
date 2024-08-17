namespace Serwer.Models
{
    public class FileRecord
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}