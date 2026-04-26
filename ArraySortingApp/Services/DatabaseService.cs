using ArraySortingApp.Helpers;
using ArraySortingApp.Models;
using Microsoft.EntityFrameworkCore;  // Для асинхронных методов (FirstOrDefaultAsync, ToListAsync)

namespace ArraySortingApp.Services
{
    // Сервис для работы с базой данных
    // Содержит всю бизнес-логику по работе с пользователями и историей
    public class DatabaseService : IDisposable  // IDisposable - для правильного освобождения ресурсов
    {
        private readonly AppDbContext _context;  // Контекст БД

        // Конструктор - создает контекст и обеспечивает создание таблиц
        public DatabaseService()
        {
            _context = new AppDbContext();
            // EnsureCreated() - создает БД и таблицы, если их еще нет
            _context.Database.EnsureCreated();
        }

        // Регистрация нового пользователя
        // (bool success, long userId, string error) - кортеж: результат, ID пользователя, ошибка
        public async Task<(bool success, long userId, string error)> RegisterUserAsync(
            string username,  // Логин
            string password)  // Пароль
        {
            try
            {
                // 1. Проверяем, существует ли уже пользователь с таким логином
                // FirstOrDefaultAsync() - ищет первую запись с условием или возвращает null
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                // Если пользователь найден - возвращаем ошибку
                if (existingUser != null)
                {
                    return (false, -1, "Пользователь с таким логином уже существует.");
                }

                // 2. Создаем нового пользователя
                var user = new User
                {
                    Username = username,
                    Password = password  // В реальных проектах пароль нужно хешировать!
                };

                // 3. Добавляем пользователя в контекст (подготавливаем к сохранению)
                _context.Users.Add(user);

                // 4. Сохраняем изменения в БД
                // SaveChangesAsync() - выполняет INSERT в таблицу
                await _context.SaveChangesAsync();

                // 5. Возвращаем успешный результат с ID нового пользователя
                return (true, user.Id, string.Empty);
            }
            catch (Exception ex)
            {
                // Если произошла ошибка - возвращаем её описание
                return (false, -1, ex.Message);
            }
        }

        // Авторизация пользователя (проверка логина и пароля)
        public async Task<(bool success, long userId, string error)> AuthenticateUserAsync(
            string username,
            string password)
        {
            try
            {
                // Ищем пользователя с указанным логином И паролем
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

                // Если не нашли - возвращаем ошибку
                if (user == null)
                {
                    return (false, -1, "Неверный логин или пароль.");
                }

                // Если нашли - возвращаем успех и ID пользователя
                return (true, user.Id, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, -1, ex.Message);
            }
        }

        // Сохранение истории сортировки
        public async Task<(bool success, string error)> SaveSortHistoryAsync(
            long userId,          // ID пользователя
            List<int> original,   // Исходный массив
            List<int> sorted)     // Отсортированный массив
        {
            try
            {
                // Преобразуем массивы в JSON-строки для хранения в БД
                var originalJson = JsonHelper.SerializeArray(original);
                var sortedJson = JsonHelper.SerializeArray(sorted);

                // Проверяем, нет ли уже точно такой же записи (защита от дубликатов)
                var duplicate = await _context.SortHistories
                    .FirstOrDefaultAsync(sh =>
                        sh.UserId == userId &&
                        sh.OriginalJson == originalJson &&
                        sh.SortedJson == sortedJson);

                // Если дубликат найден - не сохраняем, но ошибки нет
                if (duplicate != null)
                {
                    return (true, string.Empty);
                }

                // Создаем новую запись истории
                var history = new SortHistory
                {
                    UserId = userId,
                    OriginalJson = originalJson,
                    SortedJson = sortedJson,
                    CreatedAt = DateTime.Now
                };

                // Добавляем и сохраняем
                _context.SortHistories.Add(history);
                await _context.SaveChangesAsync();

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // Загрузка истории сортировок пользователя
        public async Task<(List<SortHistory> histories, string error)> LoadSortHistoryAsync(
            long userId,
            int limit = 200)  // По умолчанию загружаем последние 200 записей
        {
            try
            {
                // Запрашиваем историю:
                // - Где UserId равен указанному
                // - Сортируем по дате по убыванию (новые сверху)
                // - Берем указанное количество записей
                // ToListAsync() - выполняет запрос и возвращает список
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

        // Очистка всей истории сортировок пользователя
        public async Task<(bool success, string error)> ClearSortHistoryAsync(long userId)
        {
            try
            {
                // Находим все записи истории пользователя
                var histories = await _context.SortHistories
                    .Where(sh => sh.UserId == userId)
                    .ToListAsync();

                // Удаляем их все
                _context.SortHistories.RemoveRange(histories);

                // Сохраняем изменения
                await _context.SaveChangesAsync();

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // Освобождение ресурсов при закрытии
        // Dispose() вызывается автоматически или вручную для очистки
        public void Dispose()
        {
            _context.Dispose();  // Закрываем соединение с БД
        }
    }
}