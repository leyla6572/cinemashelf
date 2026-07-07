using CinemaShelf.Data;
using CinemaShelf.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CinemaShelf.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ==================== KAYIT OL (REGISTER) ====================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Lütfen tüm alanları doldurun.";
                return View();
            }

            // Kullanıcı adı veya E-posta daha önce alınmış mı?
            var userExists = await _context.AppUsers.AnyAsync(u => u.Username == username || u.Email == email);
            if (userExists)
            {
                ViewBag.Error = "Bu kullanıcı adı veya e-posta zaten kullanımda.";
                return View();
            }

            // Şifreyi şimdilik basitçe tutuyoruz (Gerçek projede BCrypt vb. ile hashlenir)
            var newUser = new AppUser
            {
                Username = username.Trim(),
                Email = email.Trim(),
                PasswordHash = password // Şifreyi doğrudan atıyoruz
            };

            _context.AppUsers.Add(newUser);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // ==================== GİRİŞ YAP (LOGIN) ====================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Lütfen tüm alanları doldurun.";
                return View();
            }

            // Kullanıcıyı veri tabanında ara
            var user = await _context.AppUsers.FirstOrDefaultAsync(u =>
                (u.Username == usernameOrEmail || u.Email == usernameOrEmail) && u.PasswordHash == password);

            if (user == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }

            // .NET Core Cookie Tabanlı Kimlik Doğrulama (Oturum Açma)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home"); // Giriş başarılıysa ana sayfaya uçur
        }

        // ==================== ÇIKIŞ YAP (LOGOUT) ====================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}