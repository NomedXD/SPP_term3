namespace Faker;

public class GeneratorContext
{
    public Random Random { get; }
    public Faker Faker { get; }

    public GeneratorContext(Faker faker, Random random)
    {
        Random = random;
        Faker = faker;
    }
}