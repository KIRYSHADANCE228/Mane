using System;

namespace BasicCSharpTasks
{
    /// <summary>
    /// Консольное приложение с решением двух базовых заданий по C#:
    /// 1. Возведение в степень a^n с использованием только умножения.
    /// 2. Преобразование числа x в число n путём переноса второй цифры в конец числа.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("=== Задание 1. Вычисление a^n (только умножение) ===");
            RunTask1();

            Console.WriteLine();
            Console.WriteLine("=== Задание 2. Перенос второй цифры числа x в конец ===");
            RunTask2();

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        /// <summary>
        /// Считывает с клавиатуры натуральные числа a и n, вычисляет a^n
        /// и выводит результат на экран.
        /// </summary>
        private static void RunTask1()
        {
            int a = ReadNaturalNumber("Введите a (натуральное число): ");
            int n = ReadNaturalNumber("Введите n (натуральное число): ");

            long result = Power(a, n);

            Console.WriteLine($"{a}^{n} = {result}");
        }

        /// <summary>
        /// Вычисляет a^n, используя в качестве единственной арифметической
        /// операции над накопленным результатом операцию умножения.
        /// Счётчик цикла используется только для управления количеством
        /// итераций и не участвует в вычислении самого значения степени.
        /// </summary>
        /// <param name="a">Основание степени (натуральное число).</param>
        /// <param name="n">Показатель степени (натуральное число).</param>
        /// <returns>Значение a в степени n.</returns>
        private static long Power(int a, int n)
        {
            long result = 1;

            for (int i = 0; i < n; i++)
            {
                result *= a; // единственная арифметическая операция над результатом — умножение
            }

            return result;
        }

        /// <summary>
        /// Считывает с клавиатуры число x (x >= 100, более двух цифр)
        /// и вычисляет число n путём:
        /// 1) удаления второй цифры числа x;
        /// 2) приписывания этой же цифры в конец полученного числа.
        /// </summary>
        private static void RunTask2()
        {
            long x = ReadNumberWithCondition(
                "Введите x (натуральное число, x >= 100, более двух цифр): ",
                value => value >= 100 && value.ToString().Length > 2);

            long n = TransformBySecondDigit(x);

            Console.WriteLine($"x = {x}");
            Console.WriteLine($"n = {n}");
        }

        /// <summary>
        /// Удаляет вторую цифру числа x и приписывает её в конец числа.
        /// </summary>
        /// <param name="x">Исходное число (более двух цифр).</param>
        /// <returns>Преобразованное число n.</returns>
        private static long TransformBySecondDigit(long x)
        {
            string digits = x.ToString();

            char secondDigit = digits[1];
            string withoutSecondDigit = digits.Remove(1, 1);
            string transformed = withoutSecondDigit + secondDigit;

            return long.Parse(transformed);
        }

        /// <summary>
        /// Запрашивает у пользователя ввод натурального числа (целое, > 0),
        /// повторяя запрос до получения корректного значения.
        /// </summary>
        /// <param name="prompt">Текст подсказки для пользователя.</param>
        /// <returns>Введённое натуральное число.</returns>
        private static int ReadNaturalNumber(string prompt)
        {
            int value;

            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine();

                if (int.TryParse(input, out value) && value > 0)
                {
                    return value;
                }

                Console.WriteLine("Ошибка: введите натуральное число (целое, больше нуля).");
            }
        }

        /// <summary>
        /// Запрашивает у пользователя ввод числа, удовлетворяющего заданному условию,
        /// повторяя запрос до получения корректного значения.
        /// </summary>
        /// <param name="prompt">Текст подсказки для пользователя.</param>
        /// <param name="condition">Условие, которое должно выполняться для введённого числа.</param>
        /// <returns>Введённое число, удовлетворяющее условию.</returns>
        private static long ReadNumberWithCondition(string prompt, Func<long, bool> condition)
        {
            long value;

            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine();

                if (long.TryParse(input, out value) && condition(value))
                {
                    return value;
                }

                Console.WriteLine("Ошибка: введённое значение не удовлетворяет условиям задачи. Попробуйте снова.");
            }
        }
    }
}
