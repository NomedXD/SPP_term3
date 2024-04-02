using System.Linq.Expressions;

namespace Faker;

// Конфигурация генератора под кастомные генераторы
public class FakerCustom
    // Словарь под генераторы разных классов<Класс(Его тип), словарь генераторов этого класса>
{
    private readonly Dictionary<Type, Dictionary<string, ICustomGenerator>> _generators;
    // Так как поле readonly, оно должно быть инициализированно в конструкторе
    public FakerCustom()
    {
        _generators = new Dictionary<Type, Dictionary<string, ICustomGenerator>>();
    }
    // Добавление генератора под определенное поле определенного класса
    // TTypeName - тип класса, TFieldType - тип поля, для которого добавляется генератор, TGenerator - тип самого генератора
    // getField - лямбда, которая всего лишь возвращает поле класса TTypeName
    public void Add<TTypeName, TFieldType, TGenerator>(Expression<Func<TTypeName, TFieldType>> getField)
    {
        // Если нет ни одного генератора поля для переданного класса в общей мапе,
        // добавляем пустой словарь генераторов под этот класс
        if (!_generators.ContainsKey(typeof(TTypeName)))
        {
            _generators.Add(typeof(TTypeName), new Dictionary<string, ICustomGenerator>());
        }
        // Теперь получаем поле и имя самого поля, под которое будет добавлен генератор
        var member = getField.Body as MemberExpression ?? throw new ArgumentException("Invalid expression");
        var fieldName = member.Member.Name;
        // Создание генератора, с помощью переданного типа генератора
        var generator = (ICustomGenerator)Activator.CreateInstance(typeof(TGenerator));
        // Добавление генератора под определенный класс, для определенного имени поля
        _generators[typeof(TTypeName)].Add(fieldName, generator);
    }
    // Есть ли генератор определенного класса, для определенного имени поля
    public bool HasGenerator(Type type, string fieldName)
    {
        return _generators.ContainsKey(type) && _generators[type].ContainsKey(fieldName);
    }

    public ICustomGenerator GetGenerator(Type type, string fieldName)
    {
        return HasGenerator(type, fieldName) ? _generators[type][fieldName] : null;
    }
    
}