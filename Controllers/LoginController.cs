using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using TCC_Web.Data;
using TCC_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;

namespace TCC_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public LoginController(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        // POST: api/Login/Register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterUser newUser)
        {
            // Verifica se o objeto é válido
            if (newUser == null || string.IsNullOrWhiteSpace(newUser.Usuario) || string.IsNullOrWhiteSpace(newUser.Password))
            {
                return BadRequest("Nome de usuário e senha são obrigatórios.");
            }

            try
            {
                // Cria o hash da senha
                var hashedPassword = _passwordHasher.HashPassword(null, newUser.Password);

                // Cria um novo usuário com o hash da senha
                var user = new User // A classe que representa o usuário no banco de dados
                {
                    Usuario = newUser.Usuario,
                    Password_Hash = hashedPassword, // Armazena o hash da senha
                    Data_Ativacao = DateTime.Now,
                    Data_Desativacao = null
                };

                // Adiciona o usuário ao contexto e salva
                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Registro bem-sucedido." });
                }
                catch (DbUpdateException dbEx)
                {
                    return StatusCode(500, $"Erro ao registrar usuário: {dbEx.Message} - Inner Exception: {dbEx.InnerException?.Message}");
                }
            }
            catch (Exception ex)
            {
                // Captura a mensagem completa e a inner exception, se houver
                var errorMessage = $"Erro ao registrar usuário: {ex.Message}";

                if (ex.InnerException != null)
                {
                    errorMessage += $" - Inner Exception: {ex.InnerException.Message}";
                }

                return StatusCode(500, errorMessage);
            }
        }
        // POST: api/Login/Login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] Login loginRequest)
        {
            if (string.IsNullOrWhiteSpace(loginRequest.Usuario) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                return BadRequest("Nome de usuário e senha são obrigatórios.");
            }

            // Busca o usuário pelo nome
            var user = await GetUserByUsernameADOAsync(loginRequest.Usuario);

            if (user == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            // Verifica se o Password_Hash está nulo antes de verificar a senha
            if (user.Password_Hash == null)
            {
                return StatusCode(500, "Ocorreu um erro interno: a senha hash não pode ser nula.");
            }

            // Verifica o hash da senha
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password_Hash, loginRequest.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Senha incorreta.");
            }

            // Se tudo estiver certo, retorna uma resposta de sucesso
            return Ok(new { message = "Login bem-sucedido." });
        }

        public async Task<User> GetUserByUsernameADOAsync(string username)
        {
            User user = null;

            using (var connection = new SqlConnection("Server=DESKTOP-QLE7REI\\SQLEXPRESS;Database=DataBaseCode;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"))
            {
                await connection.OpenAsync();

                var command = new SqlCommand("SELECT * FROM Users WHERE Usuario = @usuario", connection);
                command.Parameters.AddWithValue("@usuario", username);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        user = new User
                        {
                            // Mapeie as colunas do seu banco de dados para os atributos do seu modelo User
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Usuario = reader.GetString(reader.GetOrdinal("Usuario")),
                            Password_Hash = reader.GetString(reader.GetOrdinal("Password_Hash")),
                            // Adicione outros campos conforme necessário
                        };
                    }
                }
            }

            return user;
        }







        //// PUT: api/Login/Update
        //[HttpPut("update/{id}")]
        //public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        //{
        //    var user = await _context.Users.FindAsync(id);
        //    if (user == null || user.Data_Desativacao != null)
        //    {
        //        return NotFound("Usuário não encontrado ou excluído.");
        //    }

        //    // Atualiza o hash da nova senha
        //    user.Password_Hash = _passwordHasher.HashPassword(user, newPassword);
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Senha atualizada com sucesso." });
        //}

        //// DELETE: api/Login/Delete
        //[HttpDelete("delete/{id}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var user = await _context.Users.FindAsync(id);
        //    if (user == null || user.Data_Desativacao != null)
        //    {
        //        return NotFound("Usuário não encontrado ou já excluído.");
        //    }

        //    // Marca o usuário como excluído
        //    user.Data_Desativacao = DateTime.UtcNow;
        //    await _context.SaveChangesAsync();

        //    return Ok(new { message = "Usuário excluído com sucesso." });
        //}

        //// GET: api/Login/CheckUsername
        //[HttpGet("check-username/{username}")]
        //public async Task<IActionResult> CheckUsername(string username)
        //{
        //    var userExists = await _context.Users.AnyAsync(u => u.Usuario == username);
        //    if (userExists)
        //    {
        //        return Conflict("Nome de usuário já está em uso.");
        //    }

        //    return Ok(new { message = "Nome de usuário disponível." });
        //}
    }
}
