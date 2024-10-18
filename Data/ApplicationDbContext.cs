using Microsoft.EntityFrameworkCore;
using TCC_Web.Models;

namespace TCC_Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }

        // Método para buscar usuários via SQL
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            var sql = "SELECT * FROM Users WHERE Usuario = @p0";
            return await Users.FromSqlRaw(sql, username).FirstOrDefaultAsync(); // Use Users que representa a tabela
        }
    }
}
