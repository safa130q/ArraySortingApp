using System.Windows;

namespace ArraySortingApp.Dialogs
{
    public partial class SizeInputDialog : Window
    {
        // Свойство для получения введенного размера
        public int ArraySize { get; private set; }

        public SizeInputDialog()
        {
            InitializeComponent();

            // Устанавливаем значение по умолчанию
            SizeTextBox.Text = "10";

            // Ставим фокус на поле ввода (курсор будет там)
            SizeTextBox.Focus();

            // Выделяем весь текст для удобства (можно сразу начать печатать)
            SizeTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что введено число
            if (!int.TryParse(SizeTextBox.Text, out int size))
            {
                MessageBox.Show("Введите корректное число.", "Ошибка");
                return;
            }

            // Проверяем диапазон (от 1 до 1000)
            if (size <= 0 || size > 1000)
            {
                MessageBox.Show("Размер массива должен быть от 1 до 1000.", "Ошибка");
                return;
            }

            // Сохраняем размер
            ArraySize = size;

            // Закрываем с результатом OK
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Закрываем с результатом Отмена
            DialogResult = false;
            Close();
        }
    }
}