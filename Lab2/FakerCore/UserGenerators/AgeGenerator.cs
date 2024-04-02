namespace Faker.UserGenerators;

public class AgeGenerator : ICustomGenerator
{
    public object Generate(Type type, GeneratorContext context)
    {
        var age = context.Random.Next(10, 60);

        return age;
    }

    public bool CanGenerate(Type type)
    {
        return type == typeof(int);
    }
}