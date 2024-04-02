namespace Faker.UserGenerators;

public class CountryGenerator : ICustomGenerator
{
    private readonly string[] _countries = { "Belarus", "Germany", "USA", "Poland" };

    public object Generate(Type type, GeneratorContext context)
    {
        var countryId = context.Random.Next(0, _countries.Length);

        return _countries[countryId];
    }

    public bool CanGenerate(Type type)
    {
        return type == typeof(string);
    }
}