using System;

namespace Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Простой калькулятор");
            Console.WriteLine("Введите первое число:");
            double num1 = Convert.ToDouble(Console.ReadLine());

            Console.WriteLine("Введите оператор (+, -, *, /):");
            char operation = Convert.ToChar(Console.ReadLine());

            Console.WriteLine("Введите второе число:");
            double num2 = Convert.ToDouble(Console.ReadLine());

            double result = 0;

            switch (operation)
            {
                case '+':
                    result = num1 + num2;
                    break;
                case '-':
                    result = num1 - num2;
                    break;
                case '*':
                    result = num1 * num2;
                    break;
                case '/':
                    if (num2 != 0)
                        result = num1 / num2;
                    else
                        Console.WriteLine("Ошибка: деление на ноль!");
                    break;
                default:
                    Console.WriteLine("Неверный оператор!");
                    break;
            }

            Console.WriteLine($"Результат: {result}");
        }
    }
}