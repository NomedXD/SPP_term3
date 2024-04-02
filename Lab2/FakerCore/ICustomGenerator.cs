namespace Faker;

public interface ICustomGenerator
{
    object Generate(Type type, GeneratorContext context);
    bool CanGenerate(Type type);
}