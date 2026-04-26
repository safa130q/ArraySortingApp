using System.Windows;

namespace ArraySortingApp.Dialogs
{
    // Класс диалога параметров генерации
    // Частичный класс: часть определена в XAML (дизайн), часть здесь (логика)
    public partial class ParametersDialog : Window
    {
        // Свойства для получения введенных значений
        // Они доступны после закрытия диалога с DialogResult = true
        public int MinValue { get; private set; }      // Минимальное значение
        public int MaxValue { get; private set; }      // Максимальное значение
        public int Quantity { get; private set; }       // Количество элементов

        // Конструктор - вызывается при создании диалога
        public ParametersDialog()
        {
            InitializeComponent();  // Соединяет XAML с кодом (обязательно!)

            // Устанавливаем значения по умолчанию
            MinTextBox.Text = "-10";
            MaxTextBox.Text = "10";
            QuantityTextBox.Text = "10";
        }

        // Обработчик нажатия кнопки OK
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // int.TryParse() - пытается преобразовать строку в число
            // Если не получается - возвращает false
            if (!int.TryParse(MinTextBox.Text, out int min))
            {
                MessageBox.Show("Введите корректное минимальное значение.", "Ошибка");
                return;  // Прерываем выполнение, диалог не закрывается
            }

            if (!int.TryParse(MaxTextBox.Text, out int max))
            {
                MessageBox.Show("Введите корректное максимальное значение.", "Ошибка");
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity))
            {
                MessageBox.Show("Введите корректное количество элементов.", "Ошибка");
                return;
            }

            // Проверка: минимум должен быть меньше максимума
            if (min >= max)
            {
                MessageBox.Show("Минимальное значение должно быть строго меньше максимального!",
                    "Ошибка ввода");
                return;
            }

            // Проверка: количество должно быть положительным
            if (quantity <= 0)
            {
                MessageBox.Show("Количество элементов должно быть больше 0.", "Ошибка");
                return;
            }

            // Сохраняем введенные значения в свойства
            MinValue = min;
            MaxValue = max;
            Quantity = quantity;

            // Устанавливаем результат диалога как "OK" (true)
            DialogResult = true;

            // Закрываем окно
            Close();
        }

        // Обработчик нажатия кнопки Отмена
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Устанавливаем результат как "Отмена" (false)
            DialogResult = false;
            Close();
        }
    }
}