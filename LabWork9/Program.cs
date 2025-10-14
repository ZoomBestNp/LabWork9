public class Project
{
    public async Task ReadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found.");
            return;
        }
        const int bufferSize = 8192;

        using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8, true, bufferSize))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                Console.WriteLine(line);
            }
        }
    }

    public async Task WriteToFileAsync(string filePath, string data)
    {
        const int bufferSize = 8192;

        using (var writer = new StreamWriter(filePath, append: true, System.Text.Encoding.UTF8, bufferSize))
        {
            await writer.WriteLineAsync(data);
        }
        Console.WriteLine("Data successfully written to file");
    }

    private readonly Dictionary<int, int> _fibonacciCache = new Dictionary<int, int>();

    public int Fibonacci(int n)
    {
        if (_fibonacciCache.ContainsKey(n))
            return _fibonacciCache[n];

        int result;
        if (n <= 1)
        {
            result = n;
        }
        else
        {
            result = Fibonacci(n - 1) + Fibonacci(n - 2);
        }
        _fibonacciCache[n] = result;
        return result;

    }

    public static async Task Main()
    {
        Project project = new Project();
        string filePath = "data.txt";
        
        //5.2
        await project.WriteToFileAsync(filePath, "Some data to be written to the file");

        //5.1
        await project.ReadFromFileAsync(filePath);

        //5.3
        Console.WriteLine("Fibonacci sequence: ");
        for(int i = 0; i < 20; i++)
        {
            Console.WriteLine($"Fub({i}) = {project.Fibonacci(i)}");
        }

    }
}

