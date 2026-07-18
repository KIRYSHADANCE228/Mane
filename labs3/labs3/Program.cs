using System;
using System.Globalization;
using System.Text;

namespace MatrixCalculator
{
    // ==========================================================
    // Базовое пользовательское исключение для всех ошибок,
    // связанных с классом SquareMatrix
    // ==========================================================
    [Serializable]
    public class MatrixException : Exception
    {
        public MatrixException() : base("Произошла ошибка при работе с матрицей.") { }
        public MatrixException(string message) : base(message) { }
        public MatrixException(string message, Exception innerException) : base(message, innerException) { }
    }

    // Некорректный размер матрицы (например, размер <= 0
    // или попытка создать матрицу из прямоугольного массива)
    [Serializable]
    public class InvalidMatrixSizeException : MatrixException
    {
        public InvalidMatrixSizeException() : base("Некорректный размер матрицы.") { }
        public InvalidMatrixSizeException(string message) : base(message) { }
        public InvalidMatrixSizeException(string message, Exception innerException) : base(message, innerException) { }
    }

    // Операция требует совпадения размеров матриц, а они не совпадают
    [Serializable]
    public class MatrixSizeMismatchException : MatrixException
    {
        public MatrixSizeMismatchException() : base("Размеры матриц не совпадают.") { }
        public MatrixSizeMismatchException(string message) : base(message) { }
        public MatrixSizeMismatchException(string message, Exception innerException) : base(message, innerException) { }
    }

    // Попытка найти обратную матрицу для вырожденной матрицы (det == 0)
    [Serializable]
    public class SingularMatrixException : MatrixException
    {
        public SingularMatrixException() : base("Матрица вырождена, обратной матрицы не существует.") { }
        public SingularMatrixException(string message) : base(message) { }
        public SingularMatrixException(string message, Exception innerException) : base(message, innerException) { }
    }

    // Обращение к элементу матрицы по некорректному индексу
    [Serializable]
    public class MatrixIndexOutOfRangeException : MatrixException
    {
        public MatrixIndexOutOfRangeException() : base("Индекс выходит за границы матрицы.") { }
        public MatrixIndexOutOfRangeException(string message) : base(message) { }
        public MatrixIndexOutOfRangeException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Квадратная матрица вещественных чисел.
    /// Поддерживает арифметические и логические операции через перегрузку
    /// операторов, а также паттерн "Прототип" (глубокое копирование).
    ///
    /// Соглашение по операциям сравнения (>, &lt;, >=, &lt;=, CompareTo):
    /// матрицы сравниваются по сумме всех своих элементов. Это условный,
    /// но чётко определённый критерий "величины" матрицы, применимый
    /// к матрицам любого (в т.ч. разного) размера.
    ///
    /// Соглашение по операторам true/false: матрица считается "истинной",
    /// если она невырождена (определитель не равен нулю, т.е. для неё
    /// существует обратная матрица), и "ложной" — если вырождена.
    /// Это позволяет писать конструкции вида: if (matrix) { ... }
    /// </summary>
    public sealed class SquareMatrix : ICloneable, IComparable<SquareMatrix>, IEquatable<SquareMatrix>
    {
        private readonly double[,] elements;
        private static readonly Random Rng = new Random();
        private const double Epsilon = 1e-9;

        public int Size { get; }

        // ---------------------------------------------------------
        // Конструкторы
        // ---------------------------------------------------------

        /// <summary>Создаёт матрицу заданного размера со случайными элементами в диапазоне [-10; 10].</summary>
        public SquareMatrix(int size)
        {
            if (size <= 0)
                throw new InvalidMatrixSizeException("Размер матрицы должен быть положительным числом.");

            Size = size;
            elements = new double[size, size];
            FillRandom(-10, 10);
        }

        /// <summary>Создаёт матрицу заданного размера со случайными элементами в указанном диапазоне.</summary>
        public SquareMatrix(int size, double minValue, double maxValue)
        {
            if (size <= 0)
                throw new InvalidMatrixSizeException("Размер матрицы должен быть положительным числом.");
            if (minValue > maxValue)
                throw new ArgumentException("Минимальное значение диапазона не может быть больше максимального.");

            Size = size;
            elements = new double[size, size];
            FillRandom(minValue, maxValue);
        }

        /// <summary>Создаёт матрицу на основе существующего двумерного массива (с глубоким копированием).</summary>
        public SquareMatrix(double[,] source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int rows = source.GetLength(0);
            int cols = source.GetLength(1);

            if (rows == 0 || cols == 0)
                throw new InvalidMatrixSizeException("Размер матрицы должен быть положительным числом.");
            if (rows != cols)
                throw new InvalidMatrixSizeException($"Матрица должна быть квадратной, получено {rows}x{cols}.");

            Size = rows;
            elements = new double[Size, Size];
            Array.Copy(source, elements, source.Length);
        }

        /// <summary>Конструктор копирования (глубокое копирование другой матрицы).</summary>
        public SquareMatrix(SquareMatrix source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Size = source.Size;
            elements = new double[Size, Size];
            Array.Copy(source.elements, elements, source.elements.Length);
        }

        private void FillRandom(double min, double max)
        {
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    elements[i, j] = min + Rng.NextDouble() * (max - min);
        }

        // ---------------------------------------------------------
        // Фабричные методы
        // ---------------------------------------------------------

        public static SquareMatrix Zero(int size)
        {
            if (size <= 0)
                throw new InvalidMatrixSizeException("Размер матрицы должен быть положительным числом.");
            return new SquareMatrix(new double[size, size]);
        }

        public static SquareMatrix Identity(int size)
        {
            if (size <= 0)
                throw new InvalidMatrixSizeException("Размер матрицы должен быть положительным числом.");

            double[,] data = new double[size, size];
            for (int i = 0; i < size; i++)
                data[i, i] = 1.0;

            return new SquareMatrix(data);
        }

        // ---------------------------------------------------------
        // Индексатор
        // ---------------------------------------------------------

        public double this[int row, int col]
        {
            get
            {
                ValidateIndex(row, col);
                return elements[row, col];
            }
            set
            {
                ValidateIndex(row, col);
                elements[row, col] = value;
            }
        }

        private void ValidateIndex(int row, int col)
        {
            if (row < 0 || row >= Size || col < 0 || col >= Size)
                throw new MatrixIndexOutOfRangeException(
                    $"Индекс [{row},{col}] выходит за границы матрицы размером {Size}x{Size}.");
        }

        // ---------------------------------------------------------
        // Арифметические операторы
        // ---------------------------------------------------------

        public static SquareMatrix operator +(SquareMatrix a, SquareMatrix b)
        {
            ValidateNotNull(a, b);
            ValidateSameSize(a, b, "сложить");

            double[,] result = new double[a.Size, a.Size];
            for (int i = 0; i < a.Size; i++)
                for (int j = 0; j < a.Size; j++)
                    result[i, j] = a.elements[i, j] + b.elements[i, j];

            return new SquareMatrix(result);
        }

        public static SquareMatrix operator -(SquareMatrix a, SquareMatrix b)
        {
            ValidateNotNull(a, b);
            ValidateSameSize(a, b, "вычесть");

            double[,] result = new double[a.Size, a.Size];
            for (int i = 0; i < a.Size; i++)
                for (int j = 0; j < a.Size; j++)
                    result[i, j] = a.elements[i, j] - b.elements[i, j];

            return new SquareMatrix(result);
        }

        public static SquareMatrix operator *(SquareMatrix a, SquareMatrix b)
        {
            ValidateNotNull(a, b);
            ValidateSameSize(a, b, "перемножить");

            int n = a.Size;
            double[,] result = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < n; k++)
                        sum += a.elements[i, k] * b.elements[k, j];
                    result[i, j] = sum;
                }
            }

            return new SquareMatrix(result);
        }

        public static SquareMatrix operator *(SquareMatrix a, double scalar)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));

            double[,] result = new double[a.Size, a.Size];
            for (int i = 0; i < a.Size; i++)
                for (int j = 0; j < a.Size; j++)
                    result[i, j] = a.elements[i, j] * scalar;

            return new SquareMatrix(result);
        }

        public static SquareMatrix operator *(double scalar, SquareMatrix a) => a * scalar;

        // ---------------------------------------------------------
        // Операторы сравнения (по сумме элементов, см. комментарий к классу)
        // ---------------------------------------------------------

        public static bool operator >(SquareMatrix a, SquareMatrix b)
        {
            ValidateNotNull(a, b);
            return a.SumElements() > b.SumElements();
        }

        public static bool operator <(SquareMatrix a, SquareMatrix b)
        {
            ValidateNotNull(a, b);
            return a.SumElements() < b.SumElements();
        }

        public static bool operator >=(SquareMatrix a, SquareMatrix b)
        {
            ValidateNotNull(a, b);
            return a.SumElements() >= b.SumElements();
        }

        public static bool operator <=(SquareMatrix a, SquareMatrix b)
        {
            ValidateNotNull(a, b);
            return a.SumElements() <= b.SumElements();
        }

        public static bool operator ==(SquareMatrix a, SquareMatrix b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(SquareMatrix a, SquareMatrix b) => !(a == b);

        // ---------------------------------------------------------
        // Операторы true / false (истинность матрицы = обратимость)
        // ---------------------------------------------------------

        public static bool operator true(SquareMatrix m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            return Math.Abs(m.Determinant()) > Epsilon;
        }

        public static bool operator false(SquareMatrix m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            return Math.Abs(m.Determinant()) <= Epsilon;
        }

        // ---------------------------------------------------------
        // Операторы приведения типов
        // ---------------------------------------------------------

        /// <summary>Неявное приведение из double[,] в SquareMatrix.</summary>
        public static implicit operator SquareMatrix(double[,] array) => new SquareMatrix(array);

        /// <summary>Явное приведение SquareMatrix в double[,] (копия внутреннего массива).</summary>
        public static explicit operator double[,](SquareMatrix m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            double[,] copy = new double[m.Size, m.Size];
            Array.Copy(m.elements, copy, m.elements.Length);
            return copy;
        }

        /// <summary>Явное приведение SquareMatrix в double: возвращает определитель матрицы.</summary>
        public static explicit operator double(SquareMatrix m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            return m.Determinant();
        }

        // ---------------------------------------------------------
        // Определитель и обратная матрица
        // ---------------------------------------------------------

        public double Determinant() => CalculateDeterminant(elements, Size);

        private static double CalculateDeterminant(double[,] matrix, int n)
        {
            if (n == 1) return matrix[0, 0];
            if (n == 2) return matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];

            double det = 0;
            int sign = 1;

            for (int col = 0; col < n; col++)
            {
                double[,] minor = GetMinor(matrix, n, 0, col);
                det += sign * matrix[0, col] * CalculateDeterminant(minor, n - 1);
                sign = -sign;
            }

            return det;
        }

        private static double[,] GetMinor(double[,] matrix, int n, int excludeRow, int excludeCol)
        {
            double[,] minor = new double[n - 1, n - 1];
            int r = 0;

            for (int i = 0; i < n; i++)
            {
                if (i == excludeRow) continue;
                int c = 0;
                for (int j = 0; j < n; j++)
                {
                    if (j == excludeCol) continue;
                    minor[r, c] = matrix[i, j];
                    c++;
                }
                r++;
            }

            return minor;
        }

        /// <summary>Вычисляет обратную матрицу методом присоединённой (союзной) матрицы.</summary>
        public SquareMatrix Inverse()
        {
            double det = Determinant();
            if (Math.Abs(det) < Epsilon)
                throw new SingularMatrixException(
                    "Матрица вырождена (определитель равен нулю) — обратная матрица не существует.");

            if (Size == 1)
                return new SquareMatrix(new double[,] { { 1.0 / elements[0, 0] } });

            double[,] cofactors = new double[Size, Size];
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    double[,] minor = GetMinor(elements, Size, i, j);
                    double minorDet = CalculateDeterminant(minor, Size - 1);
                    cofactors[i, j] = ((i + j) % 2 == 0 ? 1 : -1) * minorDet;
                }
            }

            // Присоединённая матрица — транспонированная матрица алгебраических дополнений
            double[,] inverseData = new double[Size, Size];
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    inverseData[i, j] = cofactors[j, i] / det;

            return new SquareMatrix(inverseData);
        }

        // ---------------------------------------------------------
        // ToString, CompareTo, Equals, GetHashCode
        // ---------------------------------------------------------

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Матрица {Size}x{Size}:");

            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                    sb.Append(elements[i, j].ToString("F2", CultureInfo.InvariantCulture).PadLeft(9));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public int CompareTo(SquareMatrix other)
        {
            if (other is null) return 1;
            return SumElements().CompareTo(other.SumElements());
        }

        public bool Equals(SquareMatrix other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Size != other.Size) return false;

            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    if (Math.Abs(elements[i, j] - other.elements[i, j]) > Epsilon)
                        return false;

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as SquareMatrix);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Size;
                for (int i = 0; i < Size; i++)
                    for (int j = 0; j < Size; j++)
                        hash = hash * 23 + Math.Round(elements[i, j], 6).GetHashCode();
                return hash;
            }
        }

        private double SumElements()
        {
            double sum = 0;
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    sum += elements[i, j];
            return sum;
        }

        // ---------------------------------------------------------
        // Паттерн "Прототип" — глубокое копирование
        // ---------------------------------------------------------

        public object Clone()
        {
            double[,] copy = new double[Size, Size];
            Array.Copy(elements, copy, elements.Length);
            return new SquareMatrix(copy);
        }

        /// <summary>Строго типизированная обёртка над Clone() для удобства использования.</summary>
        public SquareMatrix DeepCopy() => (SquareMatrix)Clone();

        // ---------------------------------------------------------
        // Вспомогательные проверки
        // ---------------------------------------------------------

        private static void ValidateNotNull(SquareMatrix a, SquareMatrix b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
        }

        private static void ValidateSameSize(SquareMatrix a, SquareMatrix b, string operationName)
        {
            if (a.Size != b.Size)
                throw new MatrixSizeMismatchException(
                    $"Невозможно {operationName} матрицы разного размера: {a.Size}x{a.Size} и {b.Size}x{b.Size}.");
        }
    }

    public static class Program
    {
        private static SquareMatrix matrixA;
        private static SquareMatrix matrixB;

        public static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("      МАТРИЧНЫЙ КАЛЬКУЛЯТОР");
            Console.WriteLine("========================================");

            bool running = true;
            while (running)
            {
                PrintMenu();
                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1": CreateMatrix(ref matrixA, "A"); break;
                        case "2": CreateMatrix(ref matrixB, "B"); break;
                        case "3": ShowMatrices(); break;
                        case "4": ShowResult("A + B", () => matrixA + matrixB); break;
                        case "5": ShowResult("A - B", () => matrixA - matrixB); break;
                        case "6": ShowResult("A * B", () => matrixA * matrixB); break;
                        case "7": MultiplyByScalar(); break;
                        case "8": CompareMatrices(); break;
                        case "9": ShowDeterminant(); break;
                        case "10": ShowInverse(); break;
                        case "11": DemoPrototype(); break;
                        case "12": DemoTypeCasts(); break;
                        case "13":
                            running = false;
                            Console.WriteLine("Работа программы завершена.");
                            break;
                        default:
                            Console.WriteLine("Некорректный пункт меню. Попробуйте снова.");
                            break;
                    }
                }
                // Сначала перехватываем собственные исключения, чтобы дать пользователю
                // понятное сообщение об ошибке предметной области
                catch (MatrixException ex)
                {
                    Console.WriteLine($"[Ошибка матрицы] {ex.GetType().Name}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Непредвиденная ошибка] {ex.Message}");
                }
            }
        }

        private static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("---------------- МЕНЮ ----------------");
            Console.WriteLine(" 1. Создать матрицу A");
            Console.WriteLine(" 2. Создать матрицу B");
            Console.WriteLine(" 3. Показать матрицы A и B");
            Console.WriteLine(" 4. A + B");
            Console.WriteLine(" 5. A - B");
            Console.WriteLine(" 6. A * B");
            Console.WriteLine(" 7. A * скаляр");
            Console.WriteLine(" 8. Сравнить A и B (>, <, >=, <=, ==, !=)");
            Console.WriteLine(" 9. Определитель A");
            Console.WriteLine("10. Обратная матрица A");
            Console.WriteLine("11. Демонстрация паттерна «Прототип» (клонирование A)");
            Console.WriteLine("12. Демонстрация приведения типов");
            Console.WriteLine("13. Выход");
            Console.Write("Выберите пункт меню: ");
        }

        // -------------------------------------------------------------
        // Создание матрицы: вручную или случайно
        // -------------------------------------------------------------

        private static void CreateMatrix(ref SquareMatrix target, string label)
        {
            Console.WriteLine($"Создание матрицы {label}:");
            Console.WriteLine("1. Ввести вручную");
            Console.WriteLine("2. Сгенерировать случайно (диапазон по умолчанию: -10..10)");
            Console.WriteLine("3. Сгенерировать случайно с заданным диапазоном");
            Console.Write("Ваш выбор: ");
            string mode = Console.ReadLine();

            int size = ReadInt("Введите размер матрицы: ", 1, int.MaxValue);

            switch (mode)
            {
                case "1":
                    double[,] data = new double[size, size];
                    Console.WriteLine("Введите элементы матрицы построчно (числа через пробел):");
                    for (int i = 0; i < size; i++)
                    {
                        double[] row = ReadRow(size, i);
                        for (int j = 0; j < size; j++)
                            data[i, j] = row[j];
                    }
                    target = new SquareMatrix(data);
                    break;

                case "2":
                    target = new SquareMatrix(size);
                    break;

                case "3":
                    double min = ReadDouble("Минимальное значение: ");
                    double max = ReadDouble("Максимальное значение: ");
                    target = new SquareMatrix(size, min, max);
                    break;

                default:
                    Console.WriteLine("Некорректный выбор. Матрица не создана.");
                    return;
            }

            Console.WriteLine($"Матрица {label} успешно создана:");
            Console.WriteLine(target);
        }

        private static double[] ReadRow(int size, int rowIndex)
        {
            while (true)
            {
                Console.Write($"Строка {rowIndex + 1}: ");
                string line = Console.ReadLine();
                string[] parts = (line ?? string.Empty)
                    .Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != size)
                {
                    Console.WriteLine($"Ожидалось {size} чисел, введено {parts.Length}. Повторите ввод.");
                    continue;
                }

                double[] row = new double[size];
                bool ok = true;
                for (int j = 0; j < size; j++)
                {
                    if (!double.TryParse(parts[j], NumberStyles.Any, CultureInfo.InvariantCulture, out row[j]) &&
                        !double.TryParse(parts[j], out row[j]))
                    {
                        ok = false;
                        break;
                    }
                }

                if (!ok)
                {
                    Console.WriteLine("Не удалось распознать числа в строке. Повторите ввод.");
                    continue;
                }

                return row;
            }
        }

        // -------------------------------------------------------------
        // Операции
        // -------------------------------------------------------------

        private static void ShowMatrices()
        {
            Console.WriteLine("Матрица A:");
            Console.WriteLine(matrixA != null ? matrixA.ToString() : "не создана");
            Console.WriteLine("Матрица B:");
            Console.WriteLine(matrixB != null ? matrixB.ToString() : "не создана");
        }

        private static void ShowResult(string title, Func<SquareMatrix> operation)
        {
            EnsureBothMatricesExist();
            SquareMatrix result = operation();
            Console.WriteLine($"Результат {title}:");
            Console.WriteLine(result);
        }

        private static void MultiplyByScalar()
        {
            EnsureMatrixExists(matrixA, "A");
            double scalar = ReadDouble("Введите скаляр: ");
            SquareMatrix result = matrixA * scalar;
            Console.WriteLine($"Результат A * {scalar}:");
            Console.WriteLine(result);
        }

        private static void CompareMatrices()
        {
            EnsureBothMatricesExist();
            Console.WriteLine($"A > B  : {matrixA > matrixB}");
            Console.WriteLine($"A < B  : {matrixA < matrixB}");
            Console.WriteLine($"A >= B : {matrixA >= matrixB}");
            Console.WriteLine($"A <= B : {matrixA <= matrixB}");
            Console.WriteLine($"A == B : {matrixA == matrixB}");
            Console.WriteLine($"A != B : {matrixA != matrixB}");
            Console.WriteLine($"A.CompareTo(B) = {matrixA.CompareTo(matrixB)}");
        }

        private static void ShowDeterminant()
        {
            EnsureMatrixExists(matrixA, "A");
            Console.WriteLine($"Определитель матрицы A = {matrixA.Determinant():F4}");
        }

        private static void ShowInverse()
        {
            EnsureMatrixExists(matrixA, "A");
            SquareMatrix inverse = matrixA.Inverse(); // при вырожденной матрице выбросит SingularMatrixException
            Console.WriteLine("Обратная матрица A^-1:");
            Console.WriteLine(inverse);
        }

        private static void DemoPrototype()
        {
            EnsureMatrixExists(matrixA, "A");

            SquareMatrix clone = matrixA.DeepCopy(); // паттерн "Прототип"
            Console.WriteLine("Клон матрицы A создан (глубокая копия).");

            // Меняем один элемент в клоне и показываем, что оригинал не изменился
            clone[0, 0] = clone[0, 0] + 1000;

            Console.WriteLine("Оригинал A:");
            Console.WriteLine(matrixA);
            Console.WriteLine("Изменённый клон:");
            Console.WriteLine(clone);
            Console.WriteLine($"A равна клону? {matrixA.Equals(clone)} (ожидается False — копия независима от оригинала)");
        }

        private static void DemoTypeCasts()
        {
            EnsureMatrixExists(matrixA, "A");

            // Явное приведение SquareMatrix -> double[,]
            double[,] rawArray = (double[,])matrixA;
            Console.WriteLine($"Явное приведение A в double[,] выполнено, элемент [0,0] = {rawArray[0, 0]:F2}");

            // Явное приведение SquareMatrix -> double (определитель)
            double det = (double)matrixA;
            Console.WriteLine($"Явное приведение A в double (определитель) = {det:F4}");

            // Неявное приведение double[,] -> SquareMatrix
            SquareMatrix fromArray = rawArray; // implicit
            Console.WriteLine("Неявное приведение double[,] в SquareMatrix выполнено:");
            Console.WriteLine(fromArray);

            // Операторы true/false
            if (matrixA)
                Console.WriteLine("Матрица A невырождена (operator true сработал) — обратная матрица существует.");
            else
                Console.WriteLine("Матрица A вырождена (operator false сработал) — обратной матрицы не существует.");
        }

        // -------------------------------------------------------------
        // Вспомогательные методы проверки и ввода
        // -------------------------------------------------------------

        private static void EnsureMatrixExists(SquareMatrix m, string label)
        {
            if (m == null)
                throw new InvalidMatrixSizeException($"Матрица {label} ещё не создана. Сначала создайте её через меню.");
        }

        private static void EnsureBothMatricesExist()
        {
            EnsureMatrixExists(matrixA, "A");
            EnsureMatrixExists(matrixB, "B");
        }

        private static int ReadInt(string prompt, int min, int max)
        {
            Console.Write(prompt);
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
                    return value;
                Console.Write("Некорректное значение. Повторите ввод: ");
            }
        }

        private static double ReadDouble(string prompt)
        {
            Console.Write(prompt);
            while (true)
            {
                string input = Console.ReadLine();
                if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double result) ||
                    double.TryParse(input, out result))
                {
                    return result;
                }
                Console.Write("Некорректное число. Повторите ввод: ");
            }
        }
    }
}
