using ArraySortingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests
{
    
    // Специальный контекст БД для тестов
    // Позволяет использовать отдельный файл БД (не трогает основную)

    public class TestDbContext : AppDbContext
    {

        // Конструктор принимает настройки извне

        public TestDbContext(DbContextOptions<AppDbContext> options) : base()
        {
            // Сохраняем переданные настройки
        }
    }
}