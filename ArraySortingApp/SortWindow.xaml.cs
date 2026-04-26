// Импортируем необходимые библиотеки
using ArraySortingApp.Dialogs;    // Наши диалоговые окна
using ArraySortingApp.Models;     // Модели данных
using ArraySortingApp.Helpers;    // JSON помощник
using ArraySortingApp.Services;   // Сервисы (БД и сортировка)
using System.Collections.ObjectModel;  // ObservableCollection
using System.ComponentModel;      // INotifyPropertyChanged
using System.Windows;             // WPF окна
using System.Windows.Controls;    // Элементы интерфейса
using System.Windows.Data;       // Привязка данных (Binding)

namespace ArraySortingApp
{
    // Окно сортировки массива - открывается после входа в систему
    public partial class SortWindow : Window
    {
        // Приватные поля класса
        private readonly long _userId;                    // ID пользователя
        private readonly string _username;                // Логин пользователя
        private readonly DatabaseService _dbService;      // Сервис работы с БД
        private ObservableCollection<ArrayElement> _arrayElements = null!;  // Элементы для таблицы
        private List<int> _currentArray = new List<int>(); // Текущий массив чисел
        private bool _isUpdatingCell = false;              // Флаг программного обновления

        // Конструктор окна - вызывается при создании
        public SortWindow(long userId, string username)
        {
            // Обязательный метод для загрузки XAML дизайна
            InitializeComponent();

            // Сохраняем переданные параметры
            _userId = userId;
            _username = username;

            // Создаем сервис для работы с БД
            _dbService = new DatabaseService();

            // Устанавливаем заголовок окна
            Title = $"Сортировка массива - {username}";

            // Инициализируем таблицу
            InitializeArrayGrid();
        }

        // Инициализация таблицы для отображения массива
        private void InitializeArrayGrid()
        {
            // Создаем коллекцию, которая автоматически обновляет UI
            _arrayElements = new ObservableCollection<ArrayElement>();

            // Привязываем коллекцию к DataGrid (таблице)
            ArrayDataGrid.ItemsSource = _arrayElements;

            // Обновляем информационный текст
            UpdateArrayInfo();
        }

        // Обновление текста с информацией о массиве
        private void UpdateArrayInfo()
        {
            if (_currentArray.Count == 0)
            {
                ArrayInfoText.Text = "Массив не задан";
            }
            else
            {
                ArrayInfoText.Text = $"Размер массива: {_currentArray.Count} элементов";
            }
        }

        // Обработчик начала редактирования ячейки в таблице
        private void ArrayDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Сохраняем старое значение перед редактированием
            if (e.Row.Item is ArrayElement element)
            {
                // Запоминаем в Tag для восстановления при ошибке
                e.Row.Tag = element.Value;
            }
        }

        // Обработчик окончания редактирования ячейки - проверка ввода
        private void ArrayDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Если это программное изменение (не рукой) - пропускаем проверку
            if (_isUpdatingCell)
            {
                return;
            }

            // Получаем элемент, который редактировали
            if (e.Row.Item is ArrayElement element)
            {
                // Получаем текстовое поле, в котором было редактирование
                if (e.EditingElement is TextBox textBox)
                {
                    string newText = textBox.Text.Trim();

                    // Проверка: нельзя оставить поле пустым
                    if (string.IsNullOrEmpty(newText))
                    {
                        // Восстанавливаем предыдущее значение
                        if (e.Row.Tag is int oldValue)
                        {
                            element.Value = oldValue;
                        }
                        else
                        {
                            element.Value = 0;
                        }
                        StatusText.Text = "Значение не может быть пустым";
                        return;
                    }

                    // Проверка: введено ли целое число
                    if (!int.TryParse(newText, out int newValue))
                    {
                        // Показываем сообщение об ошибке
                        MessageBox.Show($"\"{newText}\" не является целым числом!\nВведите целое число.",
                            "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Восстанавливаем старое значение
                        if (e.Row.Tag is int oldValue)
                        {
                            element.Value = oldValue;
                        }
                        else
                        {
                            element.Value = 0;
                        }

                        // Отменяем изменения в таблице
                        e.Cancel = true;
                        StatusText.Text = "Ошибка: введите целое число";
                        return;
                    }

                    // Проверка диапазона: от -1 000 000 до 1 000 000
                    if (newValue < -1000000 || newValue > 1000000)
                    {
                        MessageBox.Show("Значение должно быть в диапазоне от -1 000 000 до 1 000 000.",
                            "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Восстанавливаем старое значение
                        if (e.Row.Tag is int oldValue)
                        {
                            element.Value = oldValue;
                        }
                        else
                        {
                            element.Value = 0;
                        }

                        e.Cancel = true;
                        StatusText.Text = "Ошибка: значение вне диапазона";
                        return;
                    }

                    // Все проверки пройдены - сохраняем новое значение
                    element.Value = newValue;
                    StatusText.Text = "Значение изменено";
                }
            }
        }

        // Обработчик кнопки "Задать размер" - создание пустого массива
        private void SetSizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем диалог ввода размера
            var dialog = new SizeInputDialog();
            dialog.Owner = this;  // Родительское окно для центрирования

            // Показываем диалог и ждем результата
            if (dialog.ShowDialog() == true)
            {
                int size = dialog.ArraySize;  // Получаем введенный размер
                CreateEmptyArray(size);       // Создаем пустой массив
                StatusText.Text = $"Создан пустой массив из {size} элементов. Введите значения.";
            }
        }

        // Создание пустого массива нужного размера
        private void CreateEmptyArray(int size)
        {
            // Создаем список, заполненный нулями
            _currentArray = new List<int>(new int[size]);

            // Очищаем коллекцию для отображения
            _arrayElements.Clear();

            // Заполняем таблицу элементами с индексами
            for (int i = 0; i < size; i++)
            {
                // ArrayElement - объект для отображения в строке таблицы
                _arrayElements.Add(new ArrayElement { Index = i, Value = 0 });
            }

            // Обновляем информационную панель
            UpdateArrayInfo();
        }

        // Обработчик кнопки "Очистить" - полная очистка массива
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _currentArray.Clear();      // Очищаем массив
            _arrayElements.Clear();     // Очищаем таблицу
            UpdateArrayInfo();          // Обновляем информацию
            StatusText.Text = "Массив очищен";
        }

        // Обработчик кнопки "Сгенерировать" - случайный массив
        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем диалог параметров генерации
            var dialog = new ParametersDialog();
            dialog.Owner = this;

            // Если нажали OK
            if (dialog.ShowDialog() == true)
            {
                // Генерируем случайный массив с указанными параметрами
                var array = SortService.GenerateRandomArray(
                    dialog.MinValue,    // Минимальное значение
                    dialog.MaxValue,    // Максимальное значение
                    dialog.Quantity);   // Количество элементов

                // Отображаем массив в таблице
                DisplayArray(array);
                StatusText.Text = $"Сгенерирован массив из {dialog.Quantity} элементов";
            }
        }

        // Отображение массива в таблице
        private void DisplayArray(List<int> array)
        {
            _isUpdatingCell = true;  // Блокируем обработчики редактирования

            _currentArray = new List<int>(array);  // Сохраняем массив
            _arrayElements.Clear();                // Очищаем таблицу

            // Заполняем таблицу элементами
            for (int i = 0; i < array.Count; i++)
            {
                _arrayElements.Add(new ArrayElement
                {
                    Index = i,        // Порядковый номер
                    Value = array[i]  // Значение элемента
                });
            }

            // Принудительно обновляем DataGrid
            ArrayDataGrid.Items.Refresh();

            _isUpdatingCell = false;  // Разблокируем обработчики

            UpdateArrayInfo();
        }

        // Чтение массива из таблицы в память
        private bool ReadArrayFromGrid()
        {
            try
            {
                _currentArray.Clear();  // Очищаем текущий массив

                // Проходим по всем элементам в таблице
                foreach (var element in _arrayElements)
                {
                    _currentArray.Add(element.Value);  // Добавляем значение
                }

                return true;  // Успешно прочитано
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении массива: {ex.Message}", "Ошибка");
                return false;
            }
        }

        // Обработчик кнопки "Отсортировать"
        private async void SortButton_Click(object sender, RoutedEventArgs e)
        {
            // Читаем массив из таблицы
            if (!ReadArrayFromGrid())
            {
                MessageBox.Show("Проверьте правильность ввода всех значений.", "Ошибка");
                return;
            }

            // Проверяем, что массив не пустой
            if (_currentArray.Count == 0)
            {
                MessageBox.Show("Массив пуст.", "Сортировка");
                return;
            }

            // Сохраняем копию исходного массива (до сортировки)
            var originalArray = new List<int>(_currentArray);

            // Выполняем гномью сортировку
            var sorted = SortService.GnomeSort(_currentArray);

            // Отображаем отсортированный массив
            DisplayArray(sorted);

            // Сохраняем в историю в БД (асинхронно, без зависания окна)
            var result = await _dbService.SaveSortHistoryAsync(_userId, originalArray, sorted);

            if (result.success)
            {
                StatusText.Text = "Массив отсортирован и сохранен в истории";
            }
            else
            {
                StatusText.Text = "Массив отсортирован, но не сохранен в истории";
            }
        }

        // Обработчик кнопки "История" - просмотр истории сортировок
        private async void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Загружаем историю из БД (последние 200 записей)
            var result = await _dbService.LoadSortHistoryAsync(_userId, 200);

            if (!string.IsNullOrEmpty(result.error))
            {
                MessageBox.Show($"Ошибка загрузки истории: {result.error}", "Ошибка");
                return;
            }

            // Создаем новое окно для отображения истории
            var historyWindow = new Window
            {
                Title = "История сортировок",
                Width = 950,
                Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            // Создаем сетку для размещения элементов
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });  // Таблица
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // Кнопки

            // Создаем таблицу для истории
            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                Margin = new Thickness(10)
            };

            // Добавляем колонки
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Дата",
                Binding = new Binding("CreatedAt") { StringFormat = "yyyy-MM-dd HH:mm:ss" },
                Width = 150
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Исходный массив",
                Binding = new Binding("OriginalJson"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Отсортированный массив",
                Binding = new Binding("SortedJson"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            // Привязываем данные к таблице
            dataGrid.ItemsSource = result.histories;

            // Обработчик двойного клика для просмотра деталей
            dataGrid.MouseDoubleClick += (s, ev) =>
            {
                if (dataGrid.SelectedItem is Models.SortHistory selected)
                {
                    ShowArrayDetails(selected);
                }
            };

            // Панель с кнопками
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            // Кнопка "Очистить историю"
            var clearButton = new Button
            {
                Content = "Очистить историю",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };

            clearButton.Click += async (s, ev) =>
            {
                // Диалог подтверждения
                if (MessageBox.Show("Вы действительно хотите очистить всю историю сортировок?",
                    "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    var clearResult = await _dbService.ClearSortHistoryAsync(_userId);
                    if (clearResult.success)
                    {
                        dataGrid.ItemsSource = null;  // Очищаем таблицу
                        StatusText.Text = "История очищена";
                    }
                }
            };

            // Кнопка "Загрузить в редактор" - восстановить массив из истории
            var loadButton = new Button
            {
                Content = "Загрузить в редактор",
                Width = 150,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };

            loadButton.Click += (s, ev) =>
            {
                if (dataGrid.SelectedItem is Models.SortHistory selected)
                {
                    // Преобразуем JSON обратно в массив чисел
                    var array = Helpers.JsonHelper.DeserializeArray(selected.OriginalJson);
                    if (array != null)
                    {
                        DisplayArray(array);      // Отображаем в редакторе
                        historyWindow.Close();    // Закрываем окно истории
                        StatusText.Text = "Массив загружен в редактор";
                    }
                }
                else
                {
                    MessageBox.Show("Выберите запись из истории.", "Внимание");
                }
            };

            // Кнопка "Закрыть"
            var closeButton = new Button
            {
                Content = "Закрыть",
                Width = 120,
                Height = 30
            };

            closeButton.Click += (s, ev) => historyWindow.Close();

            // Добавляем кнопки на панель
            buttonPanel.Children.Add(clearButton);
            buttonPanel.Children.Add(loadButton);
            buttonPanel.Children.Add(closeButton);

            // Размещаем элементы по строкам сетки
            Grid.SetRow(dataGrid, 0);     // Таблица - первая строка
            Grid.SetRow(buttonPanel, 1);   // Кнопки - вторая строка

            // Добавляем в сетку
            grid.Children.Add(dataGrid);
            grid.Children.Add(buttonPanel);

            // Показываем окно
            historyWindow.Content = grid;
            historyWindow.ShowDialog();
        }

        // Показ детальной информации о записи истории
        private void ShowArrayDetails(Models.SortHistory history)
        {
            var detailWindow = new Window
            {
                Title = "Просмотр массива",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Преобразуем JSON в массивы чисел
            var originalArray = Helpers.JsonHelper.DeserializeArray(history.OriginalJson);
            var sortedArray = Helpers.JsonHelper.DeserializeArray(history.SortedJson);

            // Формируем текст для отображения
            var textBlock = new TextBlock
            {
                Text = $"Дата: {history.CreatedAt:yyyy-MM-dd HH:mm:ss}\n\n" +
                       $"Исходный массив: {string.Join(", ", originalArray ?? new List<int>())}\n\n" +
                       $"Отсортированный массив: {string.Join(", ", sortedArray ?? new List<int>())}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20),
                FontSize = 14
            };

            // Прокручиваемая область
            var scrollViewer = new ScrollViewer
            {
                Content = textBlock,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // Кнопка закрытия
            var closeButton = new Button
            {
                Content = "Закрыть",
                Width = 100,
                Height = 30,
                Margin = new Thickness(10)
            };

            closeButton.Click += (s, ev) => detailWindow.Close();

            // Размещаем элементы
            Grid.SetRow(scrollViewer, 1);
            Grid.SetRow(closeButton, 2);

            grid.Children.Add(scrollViewer);
            grid.Children.Add(closeButton);

            detailWindow.Content = grid;
            detailWindow.ShowDialog();
        }

        // Обработчик кнопки "Справка"
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new Window
            {
                Title = "Справка",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var textBlock = new TextBlock
            {
                Text = GetHelpText(),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20),
                FontSize = 14
            };

            var scrollViewer = new ScrollViewer
            {
                Content = textBlock,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            helpWindow.Content = scrollViewer;
            helpWindow.ShowDialog();
        }

        // Текст справки
        private string GetHelpText()
        {
            return @"
СПРАВКА ПО ПРОГРАММЕ

АВТОРИЗАЦИЯ:
• Введите логин и пароль для входа в систему
• Если у вас нет аккаунта, нажмите 'Зарегистрироваться'

ВВОД МАССИВА ВРУЧНУЮ:
1. Нажмите кнопку 'Задать размер'
2. Введите желаемое количество элементов (от 1 до 1000)
3. В появившейся таблице введите значения
• Разрешены только целые числа
• При вводе букв значение вернется к предыдущему

ГЕНЕРАЦИЯ МАССИВА:
• Нажмите 'Сгенерировать'
• Укажите минимальное значение
• Укажите максимальное значение
• Укажите количество элементов

СОРТИРОВКА:
• Нажмите 'Отсортировать' для выполнения гномьей сортировки
• Результат автоматически сохраняется в историю

ИСТОРИЯ:
• Просмотр всех сохраненных сортировок
• Двойной клик по записи - просмотр деталей
• Кнопка 'Загрузить в редактор' - восстановить массив из истории
• Кнопка 'Очистить историю' - удалить все записи";
        }

        // Обработчик закрытия окна - освобождаем ресурсы
        protected override void OnClosed(EventArgs e)
        {
            _dbService.Dispose();  // Закрываем соединение с БД
            base.OnClosed(e);      // Вызываем базовый метод
        }
    }

    // ============================================================
    // Класс для отображения одной строки в таблице массива
    // ============================================================
    public class ArrayElement : INotifyPropertyChanged
    {
        // Номер элемента (индекс) - не редактируется
        public int Index { get; set; }

        // Значение элемента (приватное поле + публичное свойство)
        private int _value;
        public int Value
        {
            get => _value;  // Вернуть значение
            set
            {
                _value = value;                   // Установить новое значение
                OnPropertyChanged(nameof(Value));  // Уведомить UI об изменении
            }
        }

        // Событие для уведомления интерфейса об изменении свойства
        public event PropertyChangedEventHandler? PropertyChanged;

        // Метод вызова события изменения свойства
        protected void OnPropertyChanged(string propertyName)
        {
            // ?.Invoke - вызвать событие только если есть подписчики
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}