namespace Generators;

public class CharGenerator
{
    private static object Generate(Type type)
    {
        var random = new Random();
        return (char)random.Next();
    }

    private static bool CanGenerate(Type type)
    {
        return type == typeof(char);
    }
}