using System.Text.Json;  // Библиотека для работы с JSON

namespace IntegrationTests.Helpers
{
    // Статический класс для преобразования массивов в JSON и обратно
    public static class JsonHelper
    {
        // Преобразует список чисел в JSON-строку
        // Например: [1, 2, 3, 4, 5] -> "[1,2,3,4,5]"
        public static string SerializeArray(List<int> array)
        {
            // JsonSerializer.Serialize() - преобразует объект C# в JSON-строку
            return JsonSerializer.Serialize(array);
        }

        // Преобразует JSON-строку обратно в список чисел
        // Возвращает null, если строка не является корректным JSON-массивом чисел
        public static List<int>? DeserializeArray(string json)
        {
            try
            {
                // JsonSerializer.Deserialize<List<int>>() - преобразует JSON в список
                // Может вернуть null, если JSON пустой
                return JsonSerializer.Deserialize<List<int>>(json);
            }
            catch
            {
                // Если произошла ошибка при разборе JSON - возвращаем null
                return null;
            }
        }
    }
}