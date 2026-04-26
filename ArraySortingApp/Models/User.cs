using System.ComponentModel.DataAnnotations;  // Для атрибутов валидации [Key], [Required]
using System.ComponentModel.DataAnnotations.Schema;  // Для атрибутов [Table], [Column]

namespace ArraySortingApp.Models
{
    // [Table("users")] - указывает, что эта модель соответствует таблице "users" в БД
    [Table("users")]
    public class User
    {
        // [Key] - это первичный ключ (PRIMARY KEY)
        // [Column("id")] - имя столбца в таблице
        [Key]
        [Column("id")]
        public long Id { get; set; }  // Уникальный идентификатор пользователя

        // [Required] - поле обязательно для заполнения (NOT NULL)
        // [MaxLength(100)] - максимальная длина строки 100 символов
        [Required]
        [Column("username")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;  // Логин пользователя

        [Required]
        [Column("password")]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;  // Пароль пользователя

        // Навигационное свойство - связь один-ко-многим
        // У одного пользователя может быть много записей в истории сортировок
        // ICollection<SortHistory> - коллекция связанных записей
        public virtual ICollection<SortHistory> SortHistories { get; set; } = new List<SortHistory>();
    }
}