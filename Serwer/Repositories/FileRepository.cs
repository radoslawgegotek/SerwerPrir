using Microsoft.EntityFrameworkCore;
using Serwer.Data;
using Serwer.Models;

namespace Serwer.Repositories
{
    public class FileRepository
    {
        private readonly ApplicationDbContext _context;

        public FileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FileRecord> GetFileByIdAsync(int id)
        {
            return await _context.FileRecords.Include(f => f.User).FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<List<FileRecordDto>> GetUserFiles(string username)
        {
            return await _context.FileRecords
                .Where(x => x.User.Username == username)
                .Select(x => new FileRecordDto
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    FilePath = x.FilePath,
                    UserId = x.UserId
                })
                .ToListAsync();
        }

        public async Task AddFileAsync(FileRecord file)
        {
            _context.FileRecords.Add(file);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteFileAsync(int id)
        {
            var file = await _context.FileRecords.FindAsync(id);
            if (file != null)
            {
                _context.FileRecords.Remove(file);
                await _context.SaveChangesAsync();
            }
        }
    }
}
