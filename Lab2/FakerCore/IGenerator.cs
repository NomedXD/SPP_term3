namespace Faker;

public interface IGenerator
{
    object Generate(Type type, GeneratorContext context);
    bool CanGenerate(Type type);
}