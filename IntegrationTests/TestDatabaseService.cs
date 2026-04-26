using ArraySortingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests
{

    // Расширенный сервис БД для тестов
    // Добавляет метод загрузки случайных записей (как в C++ версии)

    public class TestDatabaseService : IDisposable
    {
        private readonly AppDbContext _context;

        public TestDatabaseService(AppDbContext context)
        {
            _context = context;
        }

        // Регистрация пользователя

        public async Task<(bool success, long userId, string error)> RegisterUserAsync(string username, string password)
        {
            try
            {
                var existing = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (existing != null)
                {
                    return (false, -1, "Пользователь уже существует");
                }

                var user = new User { Username = username, Password = password };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return (true, user.Id, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, -1, ex.Message);
            }
        }


        // Авторизация

        public async Task<(bool success, long userId, string error)> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

                if (user == null)
                {
                    return (false, -1, "Неверный логин или пароль");
                }

                return (true, user.Id, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, -1, ex.Message);
            }
        }


        // Сохранение истории сортировки (без проверки дубликатов для скорости)

        public async Task<(bool success, string error)> SaveSortHistoryAsync(long userId, List<int> original, List<int> sorted)
        {
            try
            {
                var history = new SortHistory
                {
                    UserId = userId,
                    OriginalJson = Helpers.JsonHelper.SerializeArray(original),
                    SortedJson = Helpers.JsonHelper.SerializeArray(sorted),
                    CreatedAt = DateTime.Now
                };

                _context.SortHistories.Add(history);
                await _context.SaveChangesAsync();

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }


        // Загрузка истории (последние записи)

        public async Task<(List<SortHistory> histories, string error)> LoadSortHistoryAsync(long userId, int limit)
        {
            try
            {
                var histories = await _context.SortHistories
                    .Where(sh => sh.UserId == userId)
                    .OrderByDescending(sh => sh.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                return (histories, string.Empty);
            }
            catch (Exception ex)
            {
                return (new List<SortHistory>(), ex.Message);
            }
        }

        // Загрузка СЛУЧАЙНЫХ записей из истории
        // ORDER BY RANDOM() в SQLite
   
        public async Task<(List<SortHistory> histories, string error)> LoadRandomHistoryAsync(long userId, int limit)
        {
            try
            {
                // Используем сырой SQL запрос с ORDER BY RANDOM()
                // FromSqlRaw позволяет выполнить прямой SQL
                var histories = await _context.SortHistories
                    .FromSqlRaw(@"
                SELECT * FROM sort_history 
                WHERE user_id = {0} 
                ORDER BY RANDOM() 
                LIMIT {1}",
                        userId, limit)
                    .ToListAsync();

                if (histories.Count < limit)
                {
                    return (new List<SortHistory>(),
                        $"Недостаточно записей: нужно {limit}, получено {histories.Count}");
                }

                return (histories, string.Empty);
            }
            catch (Exception ex)
            {
                return (new List<SortHistory>(), ex.Message);
            }
        }

        /// Очистка истории пользователя
        
        public async Task<(bool success, string error)> ClearSortHistoryAsync(long userId)
        {
            try
            {
                var histories = await _context.SortHistories
                    .Where(sh => sh.UserId == userId)
                    .ToListAsync();

                _context.SortHistories.RemoveRange(histories);
                await _context.SaveChangesAsync();

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}