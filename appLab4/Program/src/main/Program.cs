namespace Program.src.main;
using Generator.src.main;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks.Dataflow;

class Program
{
    public static void Main()
    {
        // Читаем путь к классу, тесты которого создаем
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        basePath = Directory.GetParent(basePath)!.FullName;
        basePath = Directory.GetParent(basePath)!.FullName;
        basePath = Directory.GetParent(basePath)!.FullName;
        basePath = Directory.GetParent(basePath)!.FullName;
        basePath = Path.Combine(basePath, "src", "main");
        var filePath = Path.Combine(basePath, "Example.cs");
        var pathes = new string[] { filePath };
        generateTestClasses(pathes, 4, 4, 4);
    }

    // Главный метод создания тестовых методов
    static void generateTestClasses(string[] pathes, int parallel1, int parallel2, int parallel3)
    {
        var generator = new Generator();
        var bufferBlock = new BufferBlock<string>();

        /*
         * Предоставляет параметры, используемые для настройки обработки, 
         * выполняемой блоками потока данных, которые обрабатывают каждое 
         * сообщение с помощью вызова указанного пользователем делегата
         */
        // Максимальная степень параллелизма для блока чтения
        var readerOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = parallel1,
        };
        var readerBlock = new TransformBlock<string, string>(read, readerOptions);

        // Максимальная степень параллельзима для блока генерации(нашего метода генератора)
        var generatorOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = parallel2,
        };
        var generatorBlock = new TransformBlock<string, ConcurrentDictionary<string, string>>(generator.getNamesAndContents, generatorOptions);

        // Максимальная степень параллельзма для блока записи
        var writerOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = parallel3,
        };
        var writer = new ActionBlock<ConcurrentDictionary<string, string>>(write, writerOptions);

        // Соединяем блоки так, чтобы данные передавались по цепочке в нужном порядке(в конце запись)
        bufferBlock.LinkTo(readerBlock);
        readerBlock.LinkTo(generatorBlock);
        generatorBlock.LinkTo(writer);

        // И соответственно, в том же порядке задаем начало следующего блока(по завершению предыдущего)
        bufferBlock.Completion.ContinueWith(task => readerBlock.Complete());
        readerBlock.Completion.ContinueWith(task => generatorBlock.Complete());
        generatorBlock.Completion.ContinueWith(task => writer.Complete());

        // Все пути классов, для которых создаются тесты, кладуться в стартовый блок-буфер 
        foreach (var path in pathes)
        {
            bufferBlock.Post(path);
        }

        // Для начала выполнения цепочки, нужно установить Complete для буфера, так как это начальный блок
        bufferBlock.Complete();

        // Ожидание выполнения всех остальных блоков(последний writer)
        writer.Completion.Wait();
    }

    // Метод для блока чтения(в блок передается ссылка на этот метод). Метод читает весь текст файла(класса)
    static async Task<string> read(string path)
    {
        return await File.ReadAllTextAsync(path);
    }

    // Метод для блока чтения(в блок передается ссылка на этот метод). Метод 
    static async Task write(ConcurrentDictionary<string, string> map)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        basePath = Directory.GetParent(basePath)!.FullName;
        basePath = Directory.GetParent(basePath)!.FullName;
        basePath = Directory.GetParent(basePath)!.FullName;
        basePath = Directory.GetParent(basePath)!.FullName;
        basePath = Path.Combine(basePath, "src", "main");

        foreach (var entry in map)
        {
            var fileName = entry.Key;
            var fileContent = entry.Value;

            var copyNumber = 1;

            var filePath = Path.Combine(basePath, $"{fileName}.cs");

            while (File.Exists(filePath))
            {
                filePath = Path.Combine(basePath, $"{fileName} [{copyNumber++}].cs");
            }
            var file = File.Create(filePath);
            var stream = new StreamWriter(file);
            await stream.WriteLineAsync(fileContent);
            await stream.FlushAsync();
            stream.Close();
        }
    }
}