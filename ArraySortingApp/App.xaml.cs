using System.Windows;

namespace ArraySortingApp
{
    // Класс приложения - создается при запуске программы
    // partial - часть класса определена в App.xaml (дизайн), часть здесь (логика)
    public partial class App : Application
    {
        // Конструктор
        public App()
        {
            // InitializeComponent() - метод, который соединяет XAML и C# код
            // Он автоматически генерируется из App.xaml
            InitializeComponent();
        }
    }
}