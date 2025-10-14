using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace UserOptimizationExample
{
    // Модель User
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }

        public List<Order> Orders { get; set; } = new List<Order>();
    }

    // Модель Order
    public class Order
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }

    // Контекст базы данных
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("UserDb");
        }
    }

    // Сервис для работы с пользователями
    public class UserService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public UserService(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<List<User>> GetActiveUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<UserWithOrderInfo>> GetUsersWithOrdersAsync()
        {
            return await _context.Users
                .Where(u => u.Orders.Any())
                .Select(u => new UserWithOrderInfo
                {
                    Name = u.Name,
                    TotalOrders = u.Orders.Count
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddUsersAsync(List<User> users)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.Users.AddRange(users);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<List<User>> GetCachedActiveUsersAsync()
        {
            const string cacheKey = "ActiveUsersCache";

            if (!_cache.TryGetValue(cacheKey, out List<User> cachedUsers))
            {
                cachedUsers = await _context.Users
                    .Where(u => u.IsActive)
                    .AsNoTracking()
                    .ToListAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_cacheDuration);

                _cache.Set(cacheKey, cachedUsers, cacheEntryOptions);
            }

            return cachedUsers;
        }
    }


    // Класс для проекции пользователей с количеством заказов
    public class UserWithOrderInfo
    {
        public string Name { get; set; }
        public int TotalOrders { get; set; }
    }



    // Класс Program для запуска приложения
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>();
            services.AddMemoryCache();
            services.AddScoped<UserService>();

            var serviceProvider = services.BuildServiceProvider();

            // Инициализация данных и сервисов
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();

            // Добавление пользователей
            var usersToAdd = new List<User>
            {
                new User { Name = "Alice", Email = "alice@example.com", IsActive = true },
                new User { Name = "Bob", Email = "bob@example.com", IsActive = false },
                new User { Name = "Charlie", Email = "charlie@example.com", IsActive = true }
            };

            await userService.AddUsersAsync(usersToAdd);

            // Получение активных пользователей
            var activeUsers = await userService.GetActiveUsersAsync();
            Console.WriteLine("Active Users:");
            foreach (var user in activeUsers)
            {
                Console.WriteLine($"{user.Name} - {user.Email}");
            }

            // Получение пользователей и их заказов
            var usersWithOrders = await userService.GetUsersWithOrdersAsync();
            Console.WriteLine("Users with Orders:");
            foreach (var user in usersWithOrders)
            {
                Console.WriteLine($"{user.Name} - Orders: {user.TotalOrders}");
            }

        }
    }
}
