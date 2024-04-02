namespace Faker.Generators;

public class UShortGenerator : IGenerator
{
    public object Generate(Type type, GeneratorContext context)
    {
        return (ushort)context.Random.Next(ushort.MinValue, ushort.MaxValue);
    }

    public bool CanGenerate(Type type)
    {
        return type == typeof(ushort);
    }
}
