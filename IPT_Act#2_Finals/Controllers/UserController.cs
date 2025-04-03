using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MySql.Data.MySqlClient;
using StackExchange.Profiling;
using System;
using System.Threading.Tasks;
using System.Data;

namespace IPT_Act_2_Finals.Controllers
{
    public class UserController : Controller
    {
        private string connStr = "Server=localhost;Database=ipt_act2_finals;User=root;Password=;";
        private readonly IMemoryCache _cache;

        public UserController(IMemoryCache cache)
        {
            _cache = cache;
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterUser(string username, string password)
        {
            using (MiniProfiler.Current.Step("User Registration Process"))
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();

                // Check Cache First
                if (_cache.TryGetValue($"UserExists_{username}", out int userCount))
                {
                    if (userCount > 0)
                    {
                        ViewBag.Error = "This username is already taken.";
                        return View("Register");
                    }
                }
                else
                {
                    using (MiniProfiler.Current.Step("Check Username Availability"))
                    {
                        using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM user WHERE username = @username", conn);
                        checkCmd.Parameters.AddWithValue("@username", username);
                        userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                        _cache.Set($"UserExists_{username}", userCount, TimeSpan.FromMinutes(10));
                    }
                }

                if (userCount > 0)
                {
                    ViewBag.Error = "This username is already taken.";
                    return View("Register");
                }

                using (MiniProfiler.Current.Step("Create New User"))
                {
                    using var cmd = new MySqlCommand("INSERT INTO user (username, password) VALUES (@username, @password)", conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Authenticate(string username, string password)
        {
            using (MiniProfiler.Current.Step("User Authentication"))
            {
                using var conn = new MySqlConnection(connStr);
                await conn.OpenAsync();

                using (MiniProfiler.Current.Step("Retrieve User Credentials"))
                {
                    using var cmd = new MySqlCommand("SELECT password FROM user WHERE username = @username", conn);
                    cmd.Parameters.AddWithValue("@username", username);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        string storedPassword = reader.GetString("password");

                        using (MiniProfiler.Current.Step("Verify Password"))
                        {
                            if (password == storedPassword)
                            {
                                return RedirectToAction("UserDashboard");
                            }
                        }
                    }
                }
            }

            ViewBag.Error = "Incorrect username or password.";
            return View("Login");
        }

        public IActionResult UserDashboard()
        {
            ViewBag.ShowBanner = true;
            return View();
        }

        public IActionResult Logout()
        {
            // Clear any authentication or session data here if needed
            return RedirectToAction("Login");
        }

        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public IActionResult CachedView()
        {
            ViewBag.ShowBanner = true;
            return View();
        }

        public string GetCachedData()
        {
            if (!_cache.TryGetValue("cachedValue", out string cachedValue))
            {
                cachedValue = "This is cached data";
                _cache.Set("cachedValue", cachedValue, TimeSpan.FromMinutes(10));
            }

            return cachedValue;
        }
    }
}