using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArraySortingApp.Models
{
    // Указываем, что этот класс соответствует таблице "sort_history" в БД
    [Table("sort_history")]
    public class SortHistory
    {
        // Первичный ключ - уникальный идентификатор записи истории
        [Key]
        [Column("id")]
        public long Id { get; set; }

        // Внешний ключ - связь с таблицей users
        // Указывает, какому пользователю принадлежит эта запись истории
        [Required]
        [Column("user_id")]
        public long UserId { get; set; }

        // JSON-строка с исходным массивом (до сортировки)
        // Храним массив как JSON, потому что SQLite не умеет хранить списки напрямую
        [Required]
        [Column("original_json")]
        public string OriginalJson { get; set; } = string.Empty;

        // JSON-строка с отсортированным массивом
        [Required]
        [Column("sorted_json")]
        public string SortedJson { get; set; } = string.Empty;

        // Дата и время сохранения записи
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Навигационное свойство - связь с пользователем
        // [ForeignKey("UserId")] - указывает, какое свойство является внешним ключом
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }  // ? означает, что может быть null
    }
}