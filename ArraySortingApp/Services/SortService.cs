namespace ArraySortingApp.Services
{
    // Статический класс - не нужно создавать экземпляр, используем напрямую SortService.GnomeSort()
    public static class SortService
    {
        // Гномья сортировка (Gnome Sort)
        // Принимает список чисел, возвращает новый отсортированный список
        // Исходный список не изменяется!
        public static List<int> GnomeSort(List<int> input)
        {
            // Если массив пустой или из одного элемента - он уже отсортирован
            if (input.Count < 2)
            {
                return new List<int>(input);  // Возвращаем копию
            }

            // Создаем копию входного массива, чтобы не менять оригинал
            var result = new List<int>(input);

            // Начинаем с индекса 1 (второй элемент)
            int i = 1;

            // Идем по массиву, пока не дойдем до конца
            while (i < result.Count)
            {
                // Если мы в начале массива ИЛИ текущий элемент >= предыдущего
                // значит порядок правильный, идем дальше
                if (i == 0 || result[i - 1] <= result[i])
                {
                    i++;  // Переходим к следующему элементу
                }
                else
                {
                    // Если текущий элемент меньше предыдущего - меняем их местами
                    // Кортежный обмен (современный способ в C#)
                    (result[i - 1], result[i]) = (result[i], result[i - 1]);

                    // Возвращаемся на шаг назад, чтобы проверить правильность порядка
                    i--;
                }
            }

            // Возвращаем отсортированный массив
            return result;
        }

        // Генерация случайного массива чисел
        public static List<int> GenerateRandomArray(int min, int max, int count)
        {
            // Random - генератор случайных чисел
            var random = new Random();

            // Создаем пустой список
            var result = new List<int>();

            // Генерируем count случайных чисел
            for (int i = 0; i < count; i++)
            {
                // random.Next(min, max + 1) - случайное число от min до max включительно
                // max + 1 потому что верхняя граница не включается
                result.Add(random.Next(min, max + 1));
            }

            return result;
        }
    }
}