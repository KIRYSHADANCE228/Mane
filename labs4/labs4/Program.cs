using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TextIO
{
    /// <summary>
    /// Класс, представляющий текстовый файл: путь, содержимое (построчно)
    /// и дату последнего изменения. Поддерживает загрузку/сохранение
    /// обычного текстового файла на диске, а также XML- и бинарную
    /// сериализацию/десериализацию самого объекта.
    /// </summary>
    [Serializable]
    [XmlRoot("TextFile")]
    public class TextFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public List<string> Lines { get; set; }
        public DateTime LastModified { get; set; }

        // Требуется XmlSerializer-у (публичный конструктор без параметров)
        public TextFile()
        {
            Lines = new List<string>();
        }

        public TextFile(string filePath) : this()
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            LastModified = DateTime.Now;
        }

        public string GetFullText() => string.Join(Environment.NewLine, Lines);

        public void SetFullText(string text)
        {
            text ??= string.Empty;
            Lines = new List<string>(text.Replace("\r\n", "\n").Split('\n'));
        }

        // -----------------------------------------------------------
        // Работа с обычным текстовым файлом на диске
        // -----------------------------------------------------------

        public void LoadFromDisk()
        {
            if (string.IsNullOrEmpty(FilePath))
                throw new InvalidOperationException("Не указан путь к файлу.");
            if (!File.Exists(FilePath))
                throw new FileNotFoundException($"Файл не найден: {FilePath}");

            Lines = new List<string>(File.ReadAllLines(FilePath, Encoding.UTF8));
            FileName = Path.GetFileName(FilePath);
            LastModified = File.GetLastWriteTime(FilePath);
        }

        public void SaveToDisk()
        {
            if (string.IsNullOrEmpty(FilePath))
                throw new InvalidOperationException("Не указан путь к файлу.");

            File.WriteAllLines(FilePath, Lines, Encoding.UTF8);
            LastModified = DateTime.Now;
        }

        // -----------------------------------------------------------
        // XML-сериализация объекта TextFile
        // -----------------------------------------------------------

        public void SerializeToXml(string path)
        {
            var serializer = new XmlSerializer(typeof(TextFile));
            using var writer = new StreamWriter(path, false, Encoding.UTF8);
            serializer.Serialize(writer, this);
        }

        public static TextFile DeserializeFromXml(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"XML-файл не найден: {path}");

            var serializer = new XmlSerializer(typeof(TextFile));
            using var reader = new StreamReader(path, Encoding.UTF8);
            return (TextFile)serializer.Deserialize(reader);
        }

        // -----------------------------------------------------------
        // Бинарная сериализация объекта TextFile.
        //
        // Реализована вручную через BinaryWriter/BinaryReader, а не через
        // System.Runtime.Serialization.Formatters.Binary.BinaryFormatter,
        // так как BinaryFormatter признан небезопасным и, начиная с .NET 8,
        // отключён по умолчанию (SYSLIB0011 / PlatformNotSupportedException).
        // -----------------------------------------------------------

        public void SerializeToBinary(string path)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream, Encoding.UTF8);

            writer.Write(FileName ?? string.Empty);
            writer.Write(FilePath ?? string.Empty);
            writer.Write(LastModified.ToBinary());
            writer.Write(Lines.Count);
            foreach (var line in Lines)
                writer.Write(line ?? string.Empty);
        }

        public static TextFile DeserializeFromBinary(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Бинарный файл не найден: {path}");

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            var file = new TextFile
            {
                FileName = reader.ReadString(),
                FilePath = reader.ReadString(),
                LastModified = DateTime.FromBinary(reader.ReadInt64())
            };

            int count = reader.ReadInt32();
            file.Lines = new List<string>(count);
            for (int i = 0; i < count; i++)
                file.Lines.Add(reader.ReadString());

            return file;
        }

        public override string ToString()
        {
            return $"{FileName ?? "(без имени)"} — {Lines.Count} строк(и), изменён {LastModified:g}";
        }
    }

    /// <summary>Совпадение по ключевым словам в одной строке файла.</summary>
    public class KeywordMatch
    {
        public int LineNumber { get; set; }
        public string LineText { get; set; }
        public List<string> MatchedKeywords { get; set; } = new List<string>();
    }

    /// <summary>Результат поиска ключевых слов в одном файле.</summary>
    public class SearchResult
    {
        public string FilePath { get; set; }
        public List<KeywordMatch> Matches { get; set; } = new List<KeywordMatch>();
        public int TotalOccurrences => Matches.Sum(m => m.MatchedKeywords.Count);
    }

    /// <summary>Класс для поиска текстовых файлов по ключевым словам.</summary>
    public class TextFileSearcher
    {
        public SearchResult SearchInFile(string filePath, IEnumerable<string> keywords, bool caseSensitive = false)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            var keywordList = (keywords ?? Enumerable.Empty<string>())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToList();

            if (keywordList.Count == 0)
                throw new ArgumentException("Список ключевых слов пуст.");

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var result = new SearchResult { FilePath = filePath };

            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                var matchedHere = keywordList.Where(k => lines[i].Contains(k, comparison)).ToList();
                if (matchedHere.Count > 0)
                {
                    result.Matches.Add(new KeywordMatch
                    {
                        LineNumber = i + 1,
                        LineText = lines[i],
                        MatchedKeywords = matchedHere
                    });
                }
            }

            return result;
        }

        /// <summary>Ищет ключевые слова во всех файлах каталога, подходящих под шаблон.</summary>
        public List<SearchResult> SearchInDirectory(
            string directory,
            IEnumerable<string> keywords,
            string searchPattern = "*.txt",
            bool recursive = true,
            bool caseSensitive = false)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Директория не найдена: {directory}");

            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(directory, searchPattern, option);

            var results = new List<SearchResult>();
            foreach (var file in files)
            {
                SearchResult result;
                try
                {
                    result = SearchInFile(file, keywords, caseSensitive);
                }
                catch (IOException)
                {
                    // Пропускаем файлы, которые не удалось прочитать (заняты, нет доступа и т.п.)
                    continue;
                }

                if (result.Matches.Count > 0)
                    results.Add(result);
            }

            return results;
        }
    }

    // =====================================================================
    // Паттерн "Memento" (Хранитель)
    // =====================================================================

    /// <summary>
    /// Хранитель (Memento) — неизменяемый снимок состояния текста редактора.
    /// Не позволяет менять своё содержимое извне после создания.
    /// </summary>
    public sealed class TextEditorMemento
    {
        public IReadOnlyList<string> Snapshot { get; }
        public string Description { get; }
        public DateTime Timestamp { get; }

        internal TextEditorMemento(IEnumerable<string> snapshot, string description)
        {
            Snapshot = new List<string>(snapshot).AsReadOnly();
            Description = description;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Создатель (Originator) — простой консольный редактор текста.
    /// Перед каждым изменяющим действием сохраняет снимок (Memento)
    /// текущего состояния, что позволяет откатывать (Undo) и повторять
    /// (Redo) изменения.
    /// </summary>
    public class TextEditor
    {
        private readonly TextFile file;

        // Caretaker (хранитель истории) реализован здесь же, в виде двух стеков
        private readonly Stack<TextEditorMemento> undoHistory = new Stack<TextEditorMemento>();
        private readonly Stack<TextEditorMemento> redoHistory = new Stack<TextEditorMemento>();

        public TextEditor(TextFile file)
        {
            this.file = file ?? throw new ArgumentNullException(nameof(file));
        }

        public TextFile File => file;
        public IReadOnlyList<string> Lines => file.Lines.AsReadOnly();
        public int UndoCount => undoHistory.Count;
        public int RedoCount => redoHistory.Count;

        private void SaveState(string description)
        {
            undoHistory.Push(new TextEditorMemento(file.Lines, description));
            redoHistory.Clear(); // новое действие делает "повтор" неактуальным
        }

        public void InsertLine(int index, string text)
        {
            ValidateInsertIndex(index);
            SaveState($"Вставка строки {index + 1}: \"{Truncate(text)}\"");
            file.Lines.Insert(index, text);
        }

        public void AppendLine(string text)
        {
            SaveState($"Добавление строки в конец: \"{Truncate(text)}\"");
            file.Lines.Add(text);
        }

        public void DeleteLine(int index)
        {
            ValidateLineIndex(index);
            SaveState($"Удаление строки {index + 1}");
            file.Lines.RemoveAt(index);
        }

        public void ReplaceLine(int index, string text)
        {
            ValidateLineIndex(index);
            SaveState($"Замена строки {index + 1}");
            file.Lines[index] = text;
        }

        /// <summary>Откатывает последнее изменение. Возвращает false, если истории отката нет.</summary>
        public bool Undo()
        {
            if (undoHistory.Count == 0) return false;

            redoHistory.Push(new TextEditorMemento(file.Lines, "снимок перед откатом"));
            var memento = undoHistory.Pop();
            file.Lines = new List<string>(memento.Snapshot);
            return true;
        }

        /// <summary>Повторяет ранее отменённое изменение. Возвращает false, если истории повтора нет.</summary>
        public bool Redo()
        {
            if (redoHistory.Count == 0) return false;

            undoHistory.Push(new TextEditorMemento(file.Lines, "снимок перед повтором"));
            var memento = redoHistory.Pop();
            file.Lines = new List<string>(memento.Snapshot);
            return true;
        }

        private void ValidateLineIndex(int index)
        {
            if (index < 0 || index >= file.Lines.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Некорректный номер строки.");
        }

        private void ValidateInsertIndex(int index)
        {
            if (index < 0 || index > file.Lines.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Некорректная позиция вставки.");
        }

        private static string Truncate(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= 30 ? text : text.Substring(0, 30) + "...";
        }
    }

    /// <summary>Одна запись индекса: сколько раз и в каких строках слово встретилось в конкретном файле.</summary>
    public class IndexEntry
    {
        public string FilePath { get; set; }
        public int Occurrences { get; set; }
        public List<int> LineNumbers { get; set; } = new List<int>();
    }

    /// <summary>
    /// Индекс текстовых файлов каталога по заданным ключевым словам:
    /// ключевое слово -> список файлов, где оно встречается, с количеством
    /// вхождений и номерами строк. Использует TextFileSearcher для поиска.
    /// </summary>
    public class FileIndex
    {
        public Dictionary<string, List<IndexEntry>> Entries { get; } = new Dictionary<string, List<IndexEntry>>();
        public DateTime BuiltAt { get; private set; }
        public string RootDirectory { get; private set; }

        public void Build(string directory, IEnumerable<string> keywords, string pattern = "*.txt", bool recursive = true)
        {
            var searcher = new TextFileSearcher();
            var results = searcher.SearchInDirectory(directory, keywords, pattern, recursive);

            Entries.Clear();
            RootDirectory = directory;
            BuiltAt = DateTime.Now;

            foreach (var result in results)
            {
                var byKeyword = result.Matches
                    .SelectMany(m => m.MatchedKeywords.Select(k => new { Keyword = k.ToLowerInvariant(), m.LineNumber }))
                    .GroupBy(x => x.Keyword);

                foreach (var group in byKeyword)
                {
                    if (!Entries.TryGetValue(group.Key, out var list))
                    {
                        list = new List<IndexEntry>();
                        Entries[group.Key] = list;
                    }

                    list.Add(new IndexEntry
                    {
                        FilePath = result.FilePath,
                        Occurrences = group.Count(),
                        LineNumbers = group.Select(x => x.LineNumber).ToList()
                    });
                }
            }
        }

        public void PrintReport()
        {
            if (Entries.Count == 0)
            {
                Console.WriteLine("Индекс пуст — совпадений не найдено.");
                return;
            }

            Console.WriteLine($"Индекс каталога \"{RootDirectory}\" (построен {BuiltAt:g}):");
            foreach (var kvp in Entries.OrderByDescending(e => e.Value.Sum(x => x.Occurrences)))
            {
                int total = kvp.Value.Sum(e => e.Occurrences);
                Console.WriteLine($"  Слово \"{kvp.Key}\": всего {total} вхожд. в {kvp.Value.Count} файле(ах)");
                foreach (var entry in kvp.Value)
                {
                    Console.WriteLine($"    - {entry.FilePath}: {entry.Occurrences} вх. (строки: {string.Join(", ", entry.LineNumbers)})");
                }
            }
        }

        /// <summary>Сохраняет отчёт по индексу в текстовый файл.</summary>
        public void SaveReportToFile(string path)
        {
            using var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
            writer.WriteLine($"Индекс каталога: {RootDirectory}");
            writer.WriteLine($"Построен: {BuiltAt:g}");
            writer.WriteLine();

            foreach (var kvp in Entries)
            {
                writer.WriteLine($"[{kvp.Key}]");
                foreach (var entry in kvp.Value)
                    writer.WriteLine($"{entry.FilePath};{entry.Occurrences};{string.Join(",", entry.LineNumbers)}");
                writer.WriteLine();
            }
        }
    }

    public static class Program
    {
        private static TextEditor editor;

        public static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("==========================================");
            Console.WriteLine("   ТЕКСТОВЫЙ РЕДАКТОР / ПОИСК / ИНДЕКСАЦИЯ");
            Console.WriteLine("==========================================");

            bool running = true;
            while (running)
            {
                PrintMainMenu();
                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1": CreateNewFile(); break;
                        case "2": OpenFileFromDisk(); break;
                        case "3": ShowContent(); break;
                        case "4": EditMenu(); break;
                        case "5": SaveToDisk(); break;
                        case "6": SerializeToXml(); break;
                        case "7": DeserializeFromXml(); break;
                        case "8": SerializeToBinary(); break;
                        case "9": DeserializeFromBinary(); break;
                        case "10": SearchByKeywords(); break;
                        case "11": IndexDirectory(); break;
                        case "12":
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

        private static void PrintMainMenu()
        {
            Console.WriteLine();
            Console.WriteLine("---------------- МЕНЮ ----------------");
            Console.WriteLine(" 1. Создать новый файл (в памяти)");
            Console.WriteLine(" 2. Открыть файл с диска");
            Console.WriteLine(" 3. Показать содержимое текущего файла");
            Console.WriteLine(" 4. Редактировать (вставка/замена/удаление, Undo/Redo)");
            Console.WriteLine(" 5. Сохранить текущий файл на диск");
            Console.WriteLine(" 6. Сериализовать текущий файл в XML");
            Console.WriteLine(" 7. Загрузить файл из XML");
            Console.WriteLine(" 8. Сериализовать текущий файл в бинарный формат");
            Console.WriteLine(" 9. Загрузить файл из бинарного формата");
            Console.WriteLine("10. Поиск текстовых файлов по ключевым словам");
            Console.WriteLine("11. Индексация каталога по ключевым словам");
            Console.WriteLine("12. Выход");
            Console.Write("Выберите пункт меню: ");
        }

        // -----------------------------------------------------------
        // Работа с текущим файлом / редактором
        // -----------------------------------------------------------

        private static void CreateNewFile()
        {
            string path = ReadNonEmpty("Введите путь для нового файла (для будущего сохранения): ");
            var file = new TextFile(path);
            editor = new TextEditor(file);
            Console.WriteLine("Новый пустой файл создан в памяти и готов к редактированию.");
        }

        private static void OpenFileFromDisk()
        {
            string path = ReadNonEmpty("Введите путь к файлу на диске: ");
            var file = new TextFile(path);
            file.LoadFromDisk();
            editor = new TextEditor(file);
            Console.WriteLine($"Файл загружен: {file}");
        }

        private static void ShowContent()
        {
            EnsureEditorExists();
            var lines = editor.Lines;
            if (lines.Count == 0)
            {
                Console.WriteLine("(файл пуст)");
                return;
            }

            for (int i = 0; i < lines.Count; i++)
                Console.WriteLine($"{i + 1,4}: {lines[i]}");
        }

        private static void EditMenu()
        {
            EnsureEditorExists();
            bool back = false;

            while (!back)
            {
                Console.WriteLine();
                Console.WriteLine("---- Редактирование ----");
                Console.WriteLine($"Строк: {editor.Lines.Count}, доступно откатов: {editor.UndoCount}, повторов: {editor.RedoCount}");
                Console.WriteLine("1. Добавить строку в конец");
                Console.WriteLine("2. Вставить строку по номеру");
                Console.WriteLine("3. Заменить строку");
                Console.WriteLine("4. Удалить строку");
                Console.WriteLine("5. Показать содержимое");
                Console.WriteLine("6. Отменить последнее изменение (Undo)");
                Console.WriteLine("7. Повторить отменённое изменение (Redo)");
                Console.WriteLine("8. Назад в главное меню");
                Console.Write("Выберите пункт: ");
                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            editor.AppendLine(ReadLineText("Текст новой строки: "));
                            break;
                        case "2":
                            int insertAt = ReadInt("Позиция вставки (номер строки, 1-based): ") - 1;
                            editor.InsertLine(insertAt, ReadLineText("Текст строки: "));
                            break;
                        case "3":
                            int replaceAt = ReadInt("Номер заменяемой строки: ") - 1;
                            editor.ReplaceLine(replaceAt, ReadLineText("Новый текст строки: "));
                            break;
                        case "4":
                            int deleteAt = ReadInt("Номер удаляемой строки: ") - 1;
                            editor.DeleteLine(deleteAt);
                            break;
                        case "5":
                            ShowContent();
                            break;
                        case "6":
                            Console.WriteLine(editor.Undo() ? "Изменение отменено." : "История отката пуста.");
                            break;
                        case "7":
                            Console.WriteLine(editor.Redo() ? "Изменение повторено." : "История повтора пуста.");
                            break;
                        case "8":
                            back = true;
                            break;
                        default:
                            Console.WriteLine("Некорректный пункт меню.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Ошибка] {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static void SaveToDisk()
        {
            EnsureEditorExists();
            editor.File.SaveToDisk();
            Console.WriteLine($"Файл сохранён: {editor.File.FilePath}");
        }

        // -----------------------------------------------------------
        // Сериализация / десериализация
        // -----------------------------------------------------------

        private static void SerializeToXml()
        {
            EnsureEditorExists();
            string path = ReadNonEmpty("Путь для сохранения XML: ");
            editor.File.SerializeToXml(path);
            Console.WriteLine($"Объект TextFile сериализован в XML: {path}");
        }

        private static void DeserializeFromXml()
        {
            string path = ReadNonEmpty("Путь к XML-файлу: ");
            var file = TextFile.DeserializeFromXml(path);
            editor = new TextEditor(file);
            Console.WriteLine($"Файл десериализован из XML: {file}");
        }

        private static void SerializeToBinary()
        {
            EnsureEditorExists();
            string path = ReadNonEmpty("Путь для сохранения бинарного файла: ");
            editor.File.SerializeToBinary(path);
            Console.WriteLine($"Объект TextFile сериализован в бинарный формат: {path}");
        }

        private static void DeserializeFromBinary()
        {
            string path = ReadNonEmpty("Путь к бинарному файлу: ");
            var file = TextFile.DeserializeFromBinary(path);
            editor = new TextEditor(file);
            Console.WriteLine($"Файл десериализован из бинарного формата: {file}");
        }

        // -----------------------------------------------------------
        // Поиск и индексация
        // -----------------------------------------------------------

        private static void SearchByKeywords()
        {
            string directory = ReadNonEmpty("Каталог для поиска: ");
            List<string> keywords = ReadKeywords();
            bool recursive = ReadYesNo("Искать во вложенных папках? (y/n): ");
            string pattern = ReadOptional("Шаблон файлов (Enter для *.txt): ", "*.txt");

            var searcher = new TextFileSearcher();
            var results = searcher.SearchInDirectory(directory, keywords, pattern, recursive);

            if (results.Count == 0)
            {
                Console.WriteLine("Совпадений не найдено.");
                return;
            }

            Console.WriteLine($"Найдено файлов с совпадениями: {results.Count}");
            foreach (var result in results.OrderByDescending(r => r.TotalOccurrences))
            {
                Console.WriteLine($"- {result.FilePath} ({result.TotalOccurrences} совпадений)");
                foreach (var match in result.Matches.Take(5))
                    Console.WriteLine($"    строка {match.LineNumber}: {match.LineText.Trim()}");
                if (result.Matches.Count > 5)
                    Console.WriteLine($"    ... и ещё {result.Matches.Count - 5} строк(и)");
            }
        }

        private static void IndexDirectory()
        {
            string directory = ReadNonEmpty("Каталог для индексации: ");
            List<string> keywords = ReadKeywords();
            bool recursive = ReadYesNo("Индексировать вложенные папки? (y/n): ");
            string pattern = ReadOptional("Шаблон файлов (Enter для *.txt): ", "*.txt");

            var index = new FileIndex();
            index.Build(directory, keywords, pattern, recursive);
            index.PrintReport();

            if (ReadYesNo("Сохранить отчёт по индексу в файл? (y/n): "))
            {
                string reportPath = ReadNonEmpty("Путь для отчёта: ");
                index.SaveReportToFile(reportPath);
                Console.WriteLine($"Отчёт сохранён: {reportPath}");
            }
        }

        // -----------------------------------------------------------
        // Вспомогательные методы ввода
        // -----------------------------------------------------------

        private static void EnsureEditorExists()
        {
            if (editor == null)
                throw new InvalidOperationException("Файл не открыт. Сначала создайте новый файл или откройте существующий.");
        }

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

        private static string ReadLineText(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? string.Empty;
        }

        private static string ReadOptional(string prompt, string defaultValue)
        {
            Console.Write(prompt);
            string input = Console.ReadLine();
            return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
        }

        private static int ReadInt(string prompt)
        {
            Console.Write(prompt);
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out int value))
                    return value;
                Console.Write("Некорректное число. Повторите ввод: ");
            }
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

        private static List<string> ReadKeywords()
        {
            Console.Write("Введите ключевые слова через пробел или запятую: ");
            string line = Console.ReadLine() ?? string.Empty;
            var keywords = line
                .Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            while (keywords.Count == 0)
            {
                Console.Write("Нужно ввести хотя бы одно ключевое слово: ");
                line = Console.ReadLine() ?? string.Empty;
                keywords = line
                    .Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            return keywords;
        }
    }
}
