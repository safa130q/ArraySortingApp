using ArraySortingApp.Models;
using ArraySortingApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace IntegrationTests
{
    // Главный класс интеграционных тестов
    // Тестирует работу с базой данных: добавление, загрузку с сортировкой, очистку

    class Program
    {
        // Константы для генерации случайных массивов
        private const int MinLength = 1;        // Минимальная длина массива
        private const int MaxLength = 300;      // Максимальная длина массива
        private const int MinValue = -1000000;  // Минимальное значение элемента
        private const int MaxValue = 1000000;   // Максимальное значение элемента


        // Точка входа в программу тестов

        static async Task Main(string[] args)
        {
            // Выводим заголовок
            Console.WriteLine("=========================================");
            Console.WriteLine("Интеграционные тесты ArraySortingApp");
            Console.WriteLine("=========================================\n");

            // Создаем путь к тестовой базе данных (отдельная от основной!)
            // Path.GetTempPath() - папка для временных файлов
            string testDbPath = Path.Combine(Path.GetTempPath(), "arraysorting_test.db");

            // Если файл тестовой БД уже существует - удаляем его (начинаем с чистого листа)
            if (File.Exists(testDbPath))
            {
                File.Delete(testDbPath);
                Console.WriteLine($"Удалена старая тестовая БД: {testDbPath}");
            }

            Console.WriteLine($"Тестовая БД: {testDbPath}\n");

            // Создаем контекст с отдельным файлом БД для тестов
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={testDbPath}")
                .Options;

            // Используем using для автоматического освобождения ресурсов
            using (var context = new TestDbContext(options))
            using (var dbService = new TestDatabaseService(context))
            {
                // Создаем структуру БД (таблицы, индексы)
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("Структура БД создана.\n");

                // Создаем тестового пользователя (один раз)
                long userId = await EnsureTestUserAsync(dbService);
                Console.WriteLine($"Тестовый пользователь ID={userId} готов.\n");

                // Общий флаг успешности всех тестов
                bool allTestsOk = true;

                // ========================================
                // ТЕСТ A: Добавление массивов в БД
                // ========================================
                Console.WriteLine("=== ТЕСТ A: Добавление массивов ===\n");

                // Тестируем для трех размеров баз: 100, 1000, 10000 записей
                allTestsOk &= await TestInsertAsync(dbService, userId, 100);
                allTestsOk &= await TestInsertAsync(dbService, userId, 1000);
                allTestsOk &= await TestInsertAsync(dbService, userId, 10000);

                // ========================================
                // ТЕСТ B: Выгрузка и сортировка 100 случайных массивов
                // ========================================
                Console.WriteLine("=== ТЕСТ B: Выгрузка и сортировка ===\n");

                // Запускаем для баз разного размера (по 3 запуска на каждый)
                allTestsOk &= await RunLoadSortTestSetAsync(dbService, userId, 100);
                allTestsOk &= await RunLoadSortTestSetAsync(dbService, userId, 1000);
                allTestsOk &= await RunLoadSortTestSetAsync(dbService, userId, 10000);

                // ========================================
                // ТЕСТ C: Очистка базы данных
                // ========================================
                Console.WriteLine("=== ТЕСТ C: Очистка базы данных ===\n");

                // Запускаем для баз разного размера (по 3 запуска на каждый)
                allTestsOk &= await RunClearTestSetAsync(dbService, userId, 100);
                allTestsOk &= await RunClearTestSetAsync(dbService, userId, 1000);
                allTestsOk &= await RunClearTestSetAsync(dbService, userId, 10000);

                // ========================================
                // ИТОГОВЫЙ РЕЗУЛЬТАТ
                // ========================================
                Console.WriteLine("=========================================");
                Console.WriteLine($"ИТОГОВЫЙ РЕЗУЛЬТАТ: {(allTestsOk ? "УСПЕШНО" : "ПРОВАЛ")}");
                Console.WriteLine($"Файл БД: {testDbPath}");
                Console.WriteLine("=========================================");

                // Возвращаем код ошибки: 0 - успех, 1 - ошибка
                Environment.ExitCode = allTestsOk ? 0 : 1;
            }
        }


        // Проверяет, существует ли тестовый пользователь, если нет - создает

        static async Task<long> EnsureTestUserAsync(TestDatabaseService dbService)
        {
            // Пытаемся зарегистрировать тестового пользователя
            var (success, userId, error) = await dbService.RegisterUserAsync("integration_test_user", "test");

            if (!success)
            {
                // Если пользователь уже существует - пробуем авторизоваться
                var authResult = await dbService.AuthenticateUserAsync("integration_test_user", "test");
                if (authResult.success)
                {
                    return authResult.userId;
                }
                else
                {
                    throw new Exception($"Не удалось создать или найти тестового пользователя: {error}");
                }
            }

            return userId;
        }


        // Генерация случайного массива целых чисел

        // <param name="minLen">Минимальная длина массива</param>
        // <param name="maxLen">Максимальная длина массива</param>
        // <param name="minVal">Минимальное значение элемента</param>
        // <param name="maxVal">Максимальное значение элемента</param>
        // <returns>Случайный массив</returns>
        static List<int> GenerateRandomArray(int minLen, int maxLen, int minVal, int maxVal)
        {
            // Random.Shared - потокобезопасный генератор случайных чисел (.NET 6+)
            var random = Random.Shared;

            // Случайная длина массива
            int length = random.Next(minLen, maxLen + 1);

            var array = new List<int>(length);

            for (int i = 0; i < length; i++)
            {
                // Случайное значение в заданном диапазоне
                int value = random.Next(minVal, maxVal + 1);
                array.Add(value);
            }

            return array;
        }


        // Подготовка БД: очистка и заполнение N записями
        // Аналог prepareDbWithN из C++ версии

        static async Task<(bool success, long timeMs, string error)> PrepareDbWithNAsync(
            TestDatabaseService dbService, long userId, int n)
        {
            try
            {
                // Очищаем историю перед заполнением
                var (clearSuccess, clearError) = await dbService.ClearSortHistoryAsync(userId);
                if (!clearSuccess)
                {
                    return (false, 0, $"Ошибка очистки: {clearError}");
                }

                // Засекаем время начала
                var stopwatch = Stopwatch.StartNew();

                // Вставляем N записей по одной (как в C++ версии)
                for (int i = 0; i < n; i++)
                {
                    // Генерируем случайный массив
                    var original = GenerateRandomArray(MinLength, MaxLength, MinValue, MaxValue);

                    // Сортируем его гномьей сортировкой
                    var sorted = SortService.GnomeSort(original);

                    // Сохраняем в БД
                    var (saveSuccess, saveError) = await dbService.SaveSortHistoryAsync(userId, original, sorted);
                    if (!saveSuccess)
                    {
                        return (false, stopwatch.ElapsedMilliseconds, $"Ошибка сохранения записи {i}: {saveError}");
                    }
                }

                // Останавливаем таймер
                stopwatch.Stop();

                // Проверяем, что записалось ровно N записей
                var history = await dbService.LoadSortHistoryAsync(userId, n + 1);
                if (history.histories.Count != n)
                {
                    return (false, stopwatch.ElapsedMilliseconds,
                        $"Несовпадение количества: ожидалось {n}, получено {history.histories.Count}");
                }

                return (true, stopwatch.ElapsedMilliseconds, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, 0, $"Исключение: {ex.Message}");
            }
        }

        // ТЕСТ A: Вставка N записей в БД
        // Выводит флаг успеха и время выполнения

        static async Task<bool> TestInsertAsync(TestDatabaseService dbService, long userId, int n)
        {
            // Выполняем подготовку БД
            var (ok, timeMs, error) = await PrepareDbWithNAsync(dbService, userId, n);

            // Выводим результат: [A] Insert N=<число> ok=<1/0> time_ms=<время>
            Console.WriteLine($"[A] Insert N={n} ok={(ok ? 1 : 0)} time_ms={timeMs}");

            // Если ошибка - выводим детали в stderr (красным)
            if (!ok)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"    error: {error}");
                Console.ForegroundColor = oldColor;
            }

            Console.WriteLine(); // Пустая строка для читаемости
            return ok;
        }


        // ТЕСТ B: Выгрузка и сортировка 100 случайных массивов

        static async Task<(bool success, long totalMs, double avgMs, string error)> TestLoadSort100Async(
            TestDatabaseService dbService, long userId)
        {
            try
            {
                // Засекаем время
                var stopwatch = Stopwatch.StartNew();

                // Загружаем 100 случайных записей из истории
                var (histories, error) = await dbService.LoadRandomHistoryAsync(userId, 100);

                if (!string.IsNullOrEmpty(error))
                {
                    return (false, 0, 0, error);
                }

                if (histories.Count != 100)
                {
                    return (false, stopwatch.ElapsedMilliseconds, 0,
                        $"Недостаточно записей: нужно 100, получено {histories.Count}");
                }

                // Для каждой записи проверяем, что сортировка дает тот же результат
                foreach (var history in histories)
                {
                    // Десериализуем исходный массив из JSON
                    var original = Helpers.JsonHelper.DeserializeArray(history.OriginalJson);
                    if (original == null)
                    {
                        return (false, stopwatch.ElapsedMilliseconds, 0, "Ошибка десериализации original_json");
                    }

                    // Десериализуем ожидаемый отсортированный массив
                    var expected = Helpers.JsonHelper.DeserializeArray(history.SortedJson);
                    if (expected == null)
                    {
                        return (false, stopwatch.ElapsedMilliseconds, 0, "Ошибка десериализации sorted_json");
                    }

                    // Выполняем сортировку заново
                    var got = SortService.GnomeSort(original);

                    // Сравниваем результат с ожидаемым
                    // SequenceEqual - сравнивает два списка поэлементно
                    if (!got.SequenceEqual(expected))
                    {
                        return (false, stopwatch.ElapsedMilliseconds, 0,
                            "Несовпадение сортировки: результат gnomeSort отличается от сохраненного в БД");
                    }
                }

                // Останавливаем таймер
                stopwatch.Stop();

                long totalMs = stopwatch.ElapsedMilliseconds;
                double avgMs = totalMs / 100.0; // Среднее время на один массив

                return (true, totalMs, avgMs, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, 0, 0, $"Исключение: {ex.Message}");
            }
        }

        // Запуск набора тестов B для базы определенного размера
        // Выполняет 3 запуска теста выгрузки и сортировки

        static async Task<bool> RunLoadSortTestSetAsync(TestDatabaseService dbService, long userId, int dbSize)
        {
            // Сначала заполняем БД нужным количеством записей
            var (prepOk, prepTimeMs, prepError) = await PrepareDbWithNAsync(dbService, userId, dbSize);

            if (!prepOk)
            {
                Console.WriteLine($"[B] Load+Sort (prepare) size={dbSize} ok=0");
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"    error: {prepError}");
                Console.ForegroundColor = oldColor;
                Console.WriteLine();
                return false;
            }

            bool allOk = true;

            // Запускаем тест 3 раза (как в C++ версии)
            for (int run = 1; run <= 3; run++)
            {
                var (ok, totalMs, avgMs, error) = await TestLoadSort100Async(dbService, userId);

                allOk &= ok;

                // Выводим результат в формате C++ версии
                Console.WriteLine($"[B] Load+Sort size={dbSize} run={run} ok={(ok ? 1 : 0)} " +
                    $"total_ms={totalMs} avg_ms={avgMs:F3}");

                if (!ok)
                {
                    var oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"    error: {error}");
                    Console.ForegroundColor = oldColor;
                }
            }

            Console.WriteLine(); // Пустая строка
            return allOk;
        }


        // ТЕСТ C: Очистка базы данных

        static async Task<(bool success, long timeMs, string error)> TestClearAsync(
            TestDatabaseService dbService, long userId)
        {
            try
            {
                // Засекаем время
                var stopwatch = Stopwatch.StartNew();

                // Выполняем очистку
                var (success, error) = await dbService.ClearSortHistoryAsync(userId);

                if (!success)
                {
                    return (false, stopwatch.ElapsedMilliseconds, error);
                }

                stopwatch.Stop();

                // Проверяем, что записей действительно 0
                var history = await dbService.LoadSortHistoryAsync(userId, 1);
                if (history.histories.Count != 0)
                {
                    return (false, stopwatch.ElapsedMilliseconds,
                        $"Несовпадение после очистки: ожидалось 0, получено {history.histories.Count}");
                }

                return (true, stopwatch.ElapsedMilliseconds, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, 0, $"Исключение: {ex.Message}");
            }
        }

        // Запуск набора тестов C для базы определенного размера
        // Выполняет 3 запуска теста очистки

        static async Task<bool> RunClearTestSetAsync(TestDatabaseService dbService, long userId, int dbSize)
        {
            bool allOk = true;

            // Запускаем тест 3 раза (как в C++ версии)
            for (int run = 1; run <= 3; run++)
            {
                // Сначала заполняем БД
                var (prepOk, prepTimeMs, prepError) = await PrepareDbWithNAsync(dbService, userId, dbSize);

                if (!prepOk)
                {
                    allOk = false;
                    Console.WriteLine($"[C] Clear size={dbSize} run={run} ok=0 time_ms=0");
                    var oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"    error (prepare): {prepError}");
                    Console.ForegroundColor = oldColor;
                    continue; // Переходим к следующему запуску
                }

                // Выполняем очистку
                var (ok, clearTimeMs, error) = await TestClearAsync(dbService, userId);
                allOk &= ok;

                // Выводим результат
                Console.WriteLine($"[C] Clear size={dbSize} run={run} ok={(ok ? 1 : 0)} time_ms={clearTimeMs}");

                if (!ok)
                {
                    var oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"    error: {error}");
                    Console.ForegroundColor = oldColor;
                }
            }

            Console.WriteLine(); // Пустая строка
            return allOk;
        }
    }
}