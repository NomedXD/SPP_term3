using System.Linq.Expressions;
using Classes;
using Faker;
using Faker.UserGenerators;

var fakerCustom = new FakerCustom();
// Передача кастомных генераторов под два поля
fakerCustom.Add<A, string, CountryGenerator>(a => a.Country);
fakerCustom.Add<A, int, AgeGenerator>(a => a.Age);

var faker = new Faker.Faker(fakerCustom);


var a = faker.Create<A>();

Console.WriteLine($"поле StringConstructor: {a.StringConstructor};\n" +
                  $"поле IntField: {a.IntField};\n" +
                  $"поле FieldWithoutSet: {a.FieldWithoutSet};\n" +
                  $"поле FieldWithPrivateSet: {a.FieldWithPrivateSet};\n" +
                  $"поле IntValue: {a.IntValue};\n" +
                  $"поле DecimalValue: {a.DecimalValue};\n" +
                  $"поле ShortValue: {a.ShortValue};\n" +
                  $"поле StringValue: {a.StringValue};\n" +
                  $"поле City: {a.Country};\n" +
                  $"поле Age: {a.Age};");

