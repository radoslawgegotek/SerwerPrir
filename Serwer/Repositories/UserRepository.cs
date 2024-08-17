using Microsoft.EntityFrameworkCore;
using Serwer.Data;
using Serwer.Models;

namespace Serwer.Repositories
{
    public class UserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.Include(u => u.Files).FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task AddUserAsync(User user)
        {
            var u = await _context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
            if (u == null)
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

            }
        }

    }

}
