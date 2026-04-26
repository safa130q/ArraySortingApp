using System.IO;
using Microsoft.EntityFrameworkCore;  // Entity Framework Core - ORM для работы с БД

namespace ArraySortingApp.Models
{
    // DbContext - основной класс для работы с БД через Entity Framework
    // Это как менеджер базы данных: через него мы сохраняем, загружаем, удаляем данные
    public class AppDbContext : DbContext
    {
        // DbSet<User> - представляет таблицу users в коде
        // Через это свойство мы будем делать запросы к таблице пользователей
        public DbSet<User> Users { get; set; }

        // DbSet<SortHistory> - представляет таблицу sort_history
        public DbSet<SortHistory> SortHistories { get; set; }

        // Путь к файлу базы данных
        public string DbPath { get; }

        // Конструктор - вызывается при создании контекста
        public AppDbContext()
        {
            // Определяем путь к папке с данными приложения
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);

            // Создаем полный путь к файлу БД
            // Path.Join() - правильно соединяет части пути (работает на любой ОС)
            DbPath = Path.Join(path, "arraysorting.db");
        }

        // Настройка подключения к БД
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // Используем SQLite как базу данных
            // Data Source - путь к файлу БД
            options.UseSqlite($"Data Source={DbPath}");
        }

        // Настройка структуры таблиц и связей
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Создаем уникальный индекс для поля Username
            // Это гарантирует, что не будет двух пользователей с одинаковым логином
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)  // Индекс по полю Username
                .IsUnique();                // Уникальный индекс

            // Создаем составной индекс для быстрого поиска истории
            // Индекс по паре (UserId, CreatedAt) ускоряет запросы истории пользователя
            modelBuilder.Entity<SortHistory>()
                .HasIndex(sh => new { sh.UserId, sh.CreatedAt })
                .HasDatabaseName("idx_user_created");  // Имя индекса в БД

            // Настройка каскадного удаления
            // При удалении пользователя, все его записи истории тоже удалятся
            modelBuilder.Entity<SortHistory>()
                .HasOne(sh => sh.User)           // Одна запись истории ссылается на...
                .WithMany(u => u.SortHistories)   // ...одного пользователя, у которого много записей
                .HasForeignKey(sh => sh.UserId)   // Внешний ключ - UserId
                .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление
        }
    }
}