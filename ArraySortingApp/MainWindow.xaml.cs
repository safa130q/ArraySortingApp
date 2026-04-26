using ArraySortingApp.Services;  // Импортируем наши сервисы
using System.Windows;             // Для работы с WPF окнами
using System.Windows.Controls;   // Для элементов управления
using System.Windows.Media;      // Для работы с цветами (Brushes)

namespace ArraySortingApp
{
    // Главное окно приложения - окно авторизации
    public partial class MainWindow : Window
    {
        // Сервис для работы с БД
        private readonly DatabaseService _dbService;

        // Ссылка на окно сортировки (если открыто)
        private SortWindow? _sortWindow;

        // Конструктор окна
        public MainWindow()
        {
            InitializeComponent();  // Загружаем дизайн из XAML

            _dbService = new DatabaseService();  // Создаем сервис БД
        }

        // Обработчик изменения текста в поле логина
        // Срабатывает каждый раз, когда пользователь что-то печатает
        private void LoginTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Убираем красную рамку, если она была (пользователь начал исправлять ошибку)
            // ClearValue() - сбрасывает значение свойства к значению по умолчанию
            LoginTextBox.ClearValue(BorderBrushProperty);
            LoginTextBox.ClearValue(BorderThicknessProperty);
        }

        // Обработчик изменения пароля
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Аналогично убираем красную рамку при вводе
            PasswordBox.ClearValue(BorderBrushProperty);
            PasswordBox.ClearValue(BorderThicknessProperty);
        }

        // Проверка заполненности полей
        // Возвращает true, если оба поля не пустые
        private bool ValidateFields()
        {
            bool isValid = true;  // Флаг валидности

            // Проверяем логин
            // string.IsNullOrWhiteSpace() - проверяет, что строка не null и не состоит из пробелов
            if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
            {
                // Подсвечиваем поле красной рамкой
                LoginTextBox.BorderBrush = Brushes.Red;       // Красный цвет рамки
                LoginTextBox.BorderThickness = new Thickness(2);  // Толщина рамки 2 пикселя
                isValid = false;  // Поле невалидно
            }

            // Проверяем пароль
            // string.IsNullOrEmpty() - проверяет, что строка не null и не пустая
            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                PasswordBox.BorderBrush = Brushes.Red;
                PasswordBox.BorderThickness = new Thickness(2);
                isValid = false;
            }

            return isValid;  // Возвращаем результат проверки
        }

        // Обработчик кнопки "Очистить"
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем текстовые поля
            LoginTextBox.Text = string.Empty;
            PasswordBox.Password = string.Empty;

            // Ставим курсор в поле логина
            LoginTextBox.Focus();
        }

        // Обработчик кнопки "Зарегистрироваться"
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем заполненность полей
            if (!ValidateFields())
            {
                StatusText.Text = "Заполните все поля";  // Сообщение в статус-баре
                return;
            }

            // Вызываем метод регистрации
            // await - ожидаем завершения асинхронной операции
            // Trim() - удаляет пробелы в начале и конце строки
            var result = await _dbService.RegisterUserAsync(
                LoginTextBox.Text.Trim(),
                PasswordBox.Password);

            // Проверяем результат
            if (result.success)
            {
                // Успешная регистрация
                MessageBox.Show("Пользователь успешно зарегистрирован.", "Успех");
                ClearButton_Click(sender, e);  // Очищаем поля
                StatusText.Text = "Регистрация успешна";
            }
            else
            {
                // Ошибка регистрации
                MessageBox.Show($"Ошибка регистрации: {result.error}", "Ошибка");
                StatusText.Text = "Ошибка регистрации";
            }
        }

        // Обработчик кнопки "Войти" (авторизация)
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем заполненность полей
            if (!ValidateFields())
            {
                StatusText.Text = "Заполните все поля";
                return;
            }

            // Пытаемся авторизоваться
            var result = await _dbService.AuthenticateUserAsync(
                LoginTextBox.Text.Trim(),
                PasswordBox.Password);

            if (result.success)
            {
                // Успешный вход
                StatusText.Text = "Вход выполнен успешно";

                // Создаем окно сортировки, передавая ID пользователя и логин
                _sortWindow = new SortWindow(result.userId, LoginTextBox.Text.Trim());

                // Подписываемся на событие закрытия окна сортировки
                // Когда окно сортировки закроется - показываем снова окно авторизации
                _sortWindow.Closed += (s, args) => this.Show();

                // Показываем окно сортировки
                _sortWindow.Show();

                // Скрываем окно авторизации
                this.Hide();

                // Очищаем поля
                ClearButton_Click(sender, e);
            }
            else
            {
                // Ошибка входа
                MessageBox.Show($"Ошибка входа: {result.error}", "Ошибка");
                StatusText.Text = "Неверный логин или пароль";
                ClearButton_Click(sender, e);  // Очищаем поля при ошибке
            }
        }

        // Обработчик закрытия окна
        protected override void OnClosed(EventArgs e)
        {
            // Освобождаем ресурсы БД при закрытии приложения
            _dbService.Dispose();

            // Вызываем базовый метод (обязательно!)
            base.OnClosed(e);
        }
    }
}
