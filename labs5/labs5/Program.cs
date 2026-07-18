using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpellFix
{
    /// <summary>
    /// Словарь "ошибочных слов": хранит соответствие правильное слово -> список
    /// его распространённых опечаток (привет -> првиет, пирвет, превет, ...),
    /// а также обратный индекс опечатка -> правильное слово для быстрого поиска
    /// при исправлении текста.
    /// </summary>
    public class ErrorWordDictionary
    {
        private readonly Dictionary<string, List<string>> correctToMisspellings =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> misspellingToCorrect =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public int WordCount => correctToMisspellings.Count;
        public int MisspellingCount => misspellingToCorrect.Count;
        public IReadOnlyDictionary<string, List<string>> Entries => correctToMisspellings;

        public void AddWord(string correctWord, params string[] misspellings)
        {
            if (string.IsNullOrWhiteSpace(correctWord))
                throw new ArgumentException("Правильное слово не может быть пустым.", nameof(correctWord));

            if (!correctToMisspellings.TryGetValue(correctWord, out var list))
            {
                list = new List<string>();
                correctToMisspellings[correctWord] = list;
            }

            foreach (var m in misspellings)
            {
                if (string.IsNullOrWhiteSpace(m)) continue;
                if (!list.Contains(m, StringComparer.OrdinalIgnoreCase))
                    list.Add(m);
                misspellingToCorrect[m] = correctWord;
            }
        }

        public bool TryGetCorrection(string word, out string correctWord)
        {
            return misspellingToCorrect.TryGetValue(word, out correctWord);
        }

        /// <summary>Словарь-пример для демонстрации работы программы (те же данные, что в SpellFixDemo/dictionary.txt).</summary>
        public static ErrorWordDictionary CreateDefault()
        {
            var dict = new ErrorWordDictionary();
            dict.AddWord("привет", "првиет", "пирвет", "превет", "привед");
            dict.AddWord("спасибо", "спсибо", "спасиба", "спосибо", "спосиба");
            dict.AddWord("здравствуйте", "здраствуйте", "здрасте", "здраствуте", "здрасьте");
            dict.AddWord("пожалуйста", "пожалуста", "пожалуйсто", "пожалста");
            dict.AddWord("извините", "извените", "извенити", "извеняюсь");
            dict.AddWord("хорошо", "харашо", "хорошою", "хараше");
            dict.AddWord("до свидания", "досвидания", "до свидание", "до свиданья");
            dict.AddWord("конечно", "канешно", "конечна", "конечны");
            dict.AddWord("кажется", "кажеться", "кажеца", "кажится");
            dict.AddWord("человек", "чиловек", "человэк", "чоловек");
            dict.AddWord("телефон", "тилифон", "телифон", "тилефон");
            dict.AddWord("номер", "намер", "номэр", "номир");
            dict.AddWord("номеру", "намеру", "номэру", "номиру");
            dict.AddWord("номера", "намера", "номэра", "номира");
            return dict;
        }

        // -----------------------------------------------------------
        // Загрузка / сохранение словаря в текстовый файл.
        // Формат строки: правильное_слово=опечатка1,опечатка2,...
        // -----------------------------------------------------------

        public void SaveToFile(string path)
        {
            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            foreach (var kvp in correctToMisspellings)
                writer.WriteLine($"{kvp.Key}={string.Join(",", kvp.Value)}");
        }

        public static ErrorWordDictionary LoadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Файл словаря не найден: {path}");

            var dict = new ErrorWordDictionary();
            foreach (var rawLine in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                int eqIndex = line.IndexOf('=');
                if (eqIndex <= 0) continue;

                string correct = line.Substring(0, eqIndex).Trim();
                string[] misspellings = line.Substring(eqIndex + 1)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();

                dict.AddWord(correct, misspellings);
            }

            return dict;
        }
    }

    public class CorrectionResult
    {
        public string FilePath { get; set; }
        public int WordsCorrected { get; set; }
        public int PhoneNumbersReplaced { get; set; }

        public override string ToString() =>
            $"{FilePath}: исправлено слов — {WordsCorrected}, заменено номеров телефонов — {PhoneNumbersReplaced}";
    }

    /// <summary>
    /// Исправляет опечатки в тексте по словарю ошибочных слов и приводит
    /// номера мобильных телефонов вида "(012) 345-67-89" к формату
    /// "+380 12 345 67 89" с помощью регулярных выражений.
    /// </summary>
    public class TextCorrector
    {
        // (XXX) XXX-XX-XX  ->  +380 XX XXX XX XX (ведущий 0 в коде убирается)
        private static readonly Regex PhoneRegex =
            new Regex(@"\((\d{3})\)\s*(\d{3})-(\d{2})-(\d{2})", RegexOptions.Compiled);

        // Последовательность букв Unicode — подходит и для русских, и для латинских слов
        private static readonly Regex WordRegex = new Regex(@"\p{L}+", RegexOptions.Compiled);

        private readonly ErrorWordDictionary dictionary;

        public TextCorrector(ErrorWordDictionary dictionary)
        {
            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        /// <summary>Исправляет один файл. Если outputPath не задан — перезаписывает исходный файл.</summary>
        public CorrectionResult CorrectFile(string path, string outputPath = null)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Файл не найден: {path}");

            string text = File.ReadAllText(path, Encoding.UTF8);

            var (afterWords, wordCount) = CorrectWords(text);
            var (afterPhones, phoneCount) = ReplacePhoneNumbers(afterWords);

            string target = outputPath ?? path;
            File.WriteAllText(target, afterPhones, Encoding.UTF8);

            return new CorrectionResult
            {
                FilePath = path,
                WordsCorrected = wordCount,
                PhoneNumbersReplaced = phoneCount
            };
        }

        /// <summary>Исправляет все файлы каталога, подходящие под шаблон.</summary>
        public List<CorrectionResult> CorrectDirectory(string directory, string pattern = "*.txt", bool recursive = true)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Директория не найдена: {directory}");

            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var results = new List<CorrectionResult>();

            foreach (var file in Directory.GetFiles(directory, pattern, option))
            {
                try
                {
                    results.Add(CorrectFile(file));
                }
                catch (IOException)
                {
                    // Пропускаем файлы, недоступные для чтения/записи
                }
            }

            return results;
        }

        private (string text, int count) CorrectWords(string text)
        {
            int count = 0;

            string result = WordRegex.Replace(text, match =>
            {
                string word = match.Value;
                if (dictionary.TryGetCorrection(word, out string correct))
                {
                    count++;
                    return MatchCase(word, correct);
                }
                return word;
            });

            return (result, count);
        }

        private (string text, int count) ReplacePhoneNumbers(string text)
        {
            int count = 0;

            string result = PhoneRegex.Replace(text, match =>
            {
                count++;
                string areaCode = match.Groups[1].Value.TrimStart('0');
                if (areaCode.Length == 0) areaCode = "0"; // на случай кода из одних нулей
                string part2 = match.Groups[2].Value;
                string part3 = match.Groups[3].Value;
                string part4 = match.Groups[4].Value;
                return $"+380 {areaCode} {part2} {part3} {part4}";
            });

            return (result, count);
        }

        /// <summary>Подгоняет регистр исправленного слова под регистр оригинала.</summary>
        private static string MatchCase(string original, string replacement)
        {
            if (original.Length > 0 && original.All(char.IsUpper))
                return replacement.ToUpperInvariant();

            if (original.Length > 0 && char.IsUpper(original[0]))
                return char.ToUpperInvariant(replacement[0]) + replacement.Substring(1).ToLowerInvariant();

            return replacement.ToLowerInvariant();
        }
    }

    public static class Program
    {
        private static ErrorWordDictionary dictionary = ErrorWordDictionary.CreateDefault();

        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("==========================================");
            Console.WriteLine("   ИСПРАВЛЕНИЕ ОПЕЧАТОК И НОМЕРОВ ТЕЛЕФОНОВ");
            Console.WriteLine("==========================================");
            Console.WriteLine($"Загружен словарь по умолчанию: {dictionary.WordCount} слов(а), {dictionary.MisspellingCount} опечаток.");

            bool running = true;
            while (running)
            {
                PrintMenu();
                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1": ShowDictionary(); break;
                        case "2": AddWordToDictionary(); break;
                        case "3": LoadDictionaryFromFile(); break;
                        case "4": SaveDictionaryToFile(); break;
                        case "5": CorrectSingleFile(); break;
                        case "6": CorrectWholeDirectory(); break;
                        case "7": CreateDemoFiles(); break;
                        case "8":
                            running = false;
                            Console.WriteLine("Работа программы завершена.");
                            break;
                        default:
                            Console.WriteLine("Некорректный пункт меню. Попробуйте снова.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Ошибка] {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static void PrintMenu()
        {
            Console.WriteLine();
            Console.WriteLine("---------------- МЕНЮ ----------------");
            Console.WriteLine("1. Показать словарь ошибочных слов");
            Console.WriteLine("2. Добавить слово и его опечатки в словарь");
            Console.WriteLine("3. Загрузить словарь из файла");
            Console.WriteLine("4. Сохранить словарь в файл");
            Console.WriteLine("5. Исправить один текстовый файл");
            Console.WriteLine("6. Исправить все текстовые файлы в каталоге");
            Console.WriteLine("7. Создать тестовые файлы для проверки (демо)");
            Console.WriteLine("8. Выход");
            Console.Write("Выберите пункт меню: ");
        }

        // -----------------------------------------------------------
        // Работа со словарём
        // -----------------------------------------------------------

        private static void ShowDictionary()
        {
            if (dictionary.WordCount == 0)
            {
                Console.WriteLine("Словарь пуст.");
                return;
            }

            foreach (var kvp in dictionary.Entries)
                Console.WriteLine($"  {kvp.Key} <- {string.Join(", ", kvp.Value)}");
        }

        private static void AddWordToDictionary()
        {
            string correct = ReadNonEmpty("Правильное слово: ");
            string misspellingsLine = ReadNonEmpty("Опечатки через запятую (например: првиет,пирвет): ");
            string[] misspellings = misspellingsLine.Split(',', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < misspellings.Length; i++)
                misspellings[i] = misspellings[i].Trim();

            dictionary.AddWord(correct, misspellings);
            Console.WriteLine($"Добавлено: {correct} <- {string.Join(", ", misspellings)}");
        }

        private static void LoadDictionaryFromFile()
        {
            string path = ReadNonEmpty("Путь к файлу словаря: ");
            dictionary = ErrorWordDictionary.LoadFromFile(path);
            Console.WriteLine($"Словарь загружен: {dictionary.WordCount} слов(а), {dictionary.MisspellingCount} опечаток.");
        }

        private static void SaveDictionaryToFile()
        {
            string path = ReadNonEmpty("Путь для сохранения словаря: ");
            dictionary.SaveToFile(path);
            Console.WriteLine($"Словарь сохранён: {path}");
        }

        // -----------------------------------------------------------
        // Исправление файлов
        // -----------------------------------------------------------

        private static void CorrectSingleFile()
        {
            string path = ReadNonEmpty("Путь к файлу: ");
            var corrector = new TextCorrector(dictionary);
            var result = corrector.CorrectFile(path);
            Console.WriteLine("Готово.");
            Console.WriteLine(result);
        }

        private static void CorrectWholeDirectory()
        {
            string directory = ReadNonEmpty("Каталог с текстовыми файлами: ");
            string pattern = ReadOptional("Шаблон файлов (Enter для *.txt): ", "*.txt");
            bool recursive = ReadYesNo("Обрабатывать вложенные папки? (y/n): ");

            var corrector = new TextCorrector(dictionary);
            var results = corrector.CorrectDirectory(directory, pattern, recursive);

            if (results.Count == 0)
            {
                Console.WriteLine("Подходящие файлы не найдены.");
                return;
            }

            int totalWords = 0, totalPhones = 0;
            foreach (var result in results)
            {
                Console.WriteLine(result);
                totalWords += result.WordsCorrected;
                totalPhones += result.PhoneNumbersReplaced;
            }

            Console.WriteLine();
            Console.WriteLine($"Итого: обработано файлов — {results.Count}, исправлено слов — {totalWords}, заменено номеров — {totalPhones}.");
        }

        // -----------------------------------------------------------
        // Демонстрационные файлы (чтобы сразу проверить работу без своих файлов)
        // -----------------------------------------------------------

        private static void CreateDemoFiles()
        {
            string directory = ReadOptional("Каталог для демо-файлов (Enter для \"SpellFixDemo\"): ", "SpellFixDemo");
            string filesDir = Path.Combine(directory, "files");
            Directory.CreateDirectory(filesDir);

            // Тот же словарь, что и ErrorWordDictionary.CreateDefault(), сохранённый в файл —
            // чтобы можно было сразу протестировать и загрузку словаря из файла (пункт 3 меню).
            string dictPath = Path.Combine(directory, "dictionary.txt");
            ErrorWordDictionary.CreateDefault().SaveToFile(dictPath);

            File.WriteAllText(Path.Combine(filesDir, "note1.txt"),
                "Првиет! Как дела? Пожалуста, позвони мне по номеру (012) 345-67-89.\r\n" +
                "Спсибо большое за помощь, здраствуйте ещё раз.\r\n" +
                "Кажеться, я забыл сказать тебе харашо новости.\r\n" +
                "Мой второй тилифон: (044) 222-33-44.\r\n",
                Encoding.UTF8);

            File.WriteAllText(Path.Combine(filesDir, "note2.txt"),
                "Харашо, извените за опоздание. Мой номер (099) 111-22-33.\r\n" +
                "Пирвет, это ещё один тестовый файл со спосибо и здраствуйте.\r\n" +
                "Канешно я приду, досвидания!\r\n" +
                "Позвони по намеру (067) 555-44-33, если что.\r\n",
                Encoding.UTF8);

            File.WriteAllText(Path.Combine(filesDir, "note3.txt"),
                "Здраствуйте, чиловек! Пожалуйсто, свяжитесь со мной.\r\n" +
                "Мои намера: (093) 123-45-67 и (050) 987-65-43.\r\n" +
                "Конечна, спасиба за понимание. Досвидания!\r\n" +
                "Тилефон офиса: (044) 700-10-20.\r\n",
                Encoding.UTF8);

            Console.WriteLine($"Словарь и демонстрационные файлы созданы в каталоге \"{directory}\":");
            Console.WriteLine("  dictionary.txt");
            Console.WriteLine("  files/note1.txt, files/note2.txt, files/note3.txt");
            Console.WriteLine("Теперь можно выбрать пункт 3 (загрузить словарь) и пункт 6 (исправить каталог files).");
        }

        // -----------------------------------------------------------
        // Вспомогательные методы ввода
        // -----------------------------------------------------------

        private static string ReadNonEmpty(string prompt)
        {
            Console.Write(prompt);
            string input = Console.ReadLine();
            while (string.IsNullOrWhiteSpace(input))
            {
                Console.Write("Значение не может быть пустым. Повторите ввод: ");
                input = Console.ReadLine();
            }
            return input;
        }

        private static string ReadOptional(string prompt, string defaultValue)
        {
            Console.Write(prompt);
            string input = Console.ReadLine();
            return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
        }

        private static bool ReadYesNo(string prompt)
        {
            Console.Write(prompt);
            while (true)
            {
                string input = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (input == "y" || input == "yes" || input == "да" || input == "д")
                    return true;
                if (input == "n" || input == "no" || input == "нет" || input == "н")
                    return false;
                Console.Write("Введите 'y' или 'n': ");
            }
        }
    }
}
