using System.Globalization;
using System.Reflection;

namespace Faker
{
    public class Faker
    {
        private const string DllName = "Generators";
        private readonly string[] _generatorsFromDllNames =  { "Generators.CharGenerator", "Generators.ShortGenerator"};
        
        private readonly FakerCustom _fakerCustom;
        private readonly List<Type> _generatedTypes;
        private readonly GeneratorContext _context;
        
        private IEnumerable<IGenerator> _generators;

        public Faker(FakerCustom fakerCustom)
        {
            _fakerCustom = fakerCustom;
            //Получение всеч генераторов из проекта Generators(не кастомные)
            GetGenerators();
            _context = new GeneratorContext(this, new Random());

            _generatedTypes = new List<Type>();
        }
        
        public Faker() : this(new FakerCustom())
        { }
        
        public T Create<T>()
        {
            return (T) Create(typeof(T));
        }

        // Принимает тип создаваемого объекта(поля)
        public object Create(Type t)
        {
            object newObject;
            
            try
            {
                newObject = GenerateViaDll(t);
            }
            catch (Exception)
            {
                newObject = null;
            }
            // Из всех некастомных выбираем первый генератор, который подходит под наш тип(если это примитив)
            newObject ??= _generators.Where(g => g.CanGenerate(t)).
                    Select(g => g.Generate(t, _context)).FirstOrDefault();

            // Если переданный в этот метод тип t является типом объекта(dto), то вызываем метод заполнения класса
            if (newObject is null && IsDto(t) && !IsGenerated(t))
            {
                // Список сгенерированных типов
                _generatedTypes.Add(t);
                // Заполнение объекта данного типа t
                newObject = FillClass(t);
                _generatedTypes.Remove(t);
            }

            return newObject ?? GetDefaultValue(t);
        }

        private void GetGenerators()
        {
            //Получить все типы и выбрать из них те, которые имплементируют IGenerator
            _generators = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IGenerator)))
                .Select(t => (IGenerator)Activator.CreateInstance(t));
        }

        private object GenerateViaDll(Type t)
        {
            object result = null;

            // Загрузка модуля
            var assembly = Assembly.LoadFrom(DllName);

            // Проход по названиям всех генераторов из модуля
            foreach (var generatorName in _generatorsFromDllNames)
            {
                // Получить тип генератора
                var genType = assembly.GetType(generatorName);

                // Получение метода CanGenerate у генератора(проверка, что переданное поле является типом,
                // который этот генератор генерирует)
                var methodInfoCanGenerate = genType?.GetMethod("CanGenerate",  BindingFlags.NonPublic | BindingFlags.Static);

                // Приведение переданного типа к object(т.к. мы не знаем, что это за тип)
                var param = new object[] { t };

                // Проверить, что мы получили метод CanGenerate и вызываем его, чтобы проверить, можем
                // ли мы сгенерировать значение для переданного типам t
                if (methodInfoCanGenerate is not null && (bool?)methodInfoCanGenerate.Invoke(null, param) == true)
                {
                    // Получение и вызов метода генерации значения для переданного типа
                    var methodInfoGenerate = genType.GetMethod("Generate",  BindingFlags.NonPublic | BindingFlags.Static);
                    result = methodInfoGenerate?.Invoke(null, param);
                }
            }

            return result;
        }
        
        // Создаем дефолтное значение для переданного типа поля, для ссылочных это null
        private object GetDefaultValue(Type t)
        {
            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }
        
        private object FillClass(Type t)
        {
            /*
             * Через конструктор класса инициализируются только поля,
             * поэтому вызываем еще и FillProperties, которые инициализируются через сеттеры
             */
            var newObject = CreateClass(t);
            // Заполнение свойств объекта(не кастомные) через сеттеры
            FillProperties(newObject, t);
            // Заполнение свойств объекта(кастомные)
            FillPropertiesWithUserGenerators(newObject, t);
            // Заполнение всех полей, которые остались не заполнены
            // (из-за отсутствия параметра конструктора) или у которых дефолтное значение
            FillFields(newObject, t);
            FillFieldsWithUserGenerators(newObject, t);

            return newObject;
        }

        // Получение всех возможных для нас конструкторов типа t
        private ConstructorInfo GetConstructorForInvoke(Type t)
        {
            // Получение подходящих конструкторов
            var constructorsInfo = t.GetConstructors(BindingFlags.DeclaredOnly |
                                                     BindingFlags.Instance | BindingFlags.Public |
                                                     BindingFlags.NonPublic);
            
            // Получаем индекс конструктора с наибольшим количеством параметров
            var maxParamsCount = constructorsInfo[0].GetParameters().Length;
            var constructorWithMaxParams = 0;
            for (var i = 1; i < constructorsInfo.Length; i++)
            {
                var currParamsCount = constructorsInfo[i].GetParameters().Length;
                if (currParamsCount > maxParamsCount)
                {
                    constructorWithMaxParams = i;
                    maxParamsCount = currParamsCount;
                }
            }   
            // Возвращаем найденный конструктор
            return constructorsInfo[constructorWithMaxParams];
        }
        
        private object[] GetArguments(Type t, ParameterInfo[] paramsInfo)
        {
            // Параметры конструктора переданного класса
            var args = new object[paramsInfo.Length];
            var textInfo = new CultureInfo("en-US",false).TextInfo;
            // Проход по всем параметрам конструктора
            for (var i = 0; i < args.Length; i++)
            {
                // На всякий случай проверка
                if (paramsInfo[i].Name is not null)
                {
                    // Получение имени поля по имени параметра конструктора
                    var fieldName = textInfo.ToTitleCase(paramsInfo[i].Name);
                    // Если под тип поля и его имя есть генератор(в кастомных генераторах)
                    if (_fakerCustom.HasGenerator(t, fieldName))
                    {
                        // Получаем объект данного поля
                        var fieldInfo = t.GetField(fieldName, 
                            BindingFlags.Public | BindingFlags.Instance);
                        var propertyInfo = t.GetProperty(fieldName, 
                            BindingFlags.Public | BindingFlags.Instance);

                        if (fieldInfo is not null && fieldInfo.FieldType == paramsInfo[i].ParameterType ||
                            propertyInfo is not null && propertyInfo.PropertyType == paramsInfo[i].ParameterType)
                        {
                            // Получение подходящего генератора
                            var generator = _fakerCustom.GetGenerator(t, fieldName);
                            // Генерация значения данного поля(параметра конструктора)
                            args[i] = generator.Generate(paramsInfo[i].ParameterType, _context);
                            continue;
                        }
                    }
                }
                
                args[i] = Create(paramsInfo[i].ParameterType);
            }

            return args;
        }

        private object CreateClass(Type t)
        {
            // Получение подходящего конструктора
            var constructor = GetConstructorForInvoke(t);
            // Получение списка параметров конструктора
            var paramsInfo = constructor.GetParameters();
            var args = GetArguments(t, paramsInfo);
            // Передаем в конструктор объекта сгенераированные значения параметров конструктора
            return constructor.Invoke(args);
        }

        private void FillProperties(object obj, IReflect t)
        {
            // Получение свойств переданного типа
            var propertiesInfo = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in propertiesInfo)
            {
                // Если есть сеттер, чтобы установить свойство
                if (property.SetMethod is not null && property.SetMethod.IsPublic)
                {
                    // Получение объекта свойства для данного объекта obj
                    var getMethod = property.GetMethod?.Invoke(obj, null);
                    // Получение базового значения для свойства данного типа
                    var defValue = GetDefaultValue(property.PropertyType);
                    // Вызываем метод Create для данного типа свойства
                    if (getMethod is null || getMethod.Equals(defValue))
                        property.SetValue(obj, Create(property.PropertyType));
                }
            }
        }
        
        // Установка значений полей переданного объекта obj(его тип t)
        private void FillFields(object obj, IReflect t)
        {
            // Получение списка публичных полей
            var fieldsInfo = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            // Каждому полю присваиваем значение
            foreach (var field in fieldsInfo)
            {   
                // Значение этого поля в объекте obj
                var getMethod = field.GetValue(obj);

                if (getMethod is null || getMethod.Equals(GetDefaultValue(field.FieldType)))
                    field.SetValue(obj, Create(field.FieldType));
            }
        }

        // Аналогично FillProperties, но для кастомных генераторов
        private void FillPropertiesWithUserGenerators(object obj, Type t)
        {
            var propertiesInfo = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in propertiesInfo)
            {
                var propertyType = property.PropertyType;
                if (_fakerCustom.HasGenerator(t, property.Name) && property.CanWrite)
                {
                    var generator = _fakerCustom.GetGenerator(t, property.Name);

                    if (generator.CanGenerate(propertyType))
                        property.SetValue(obj, generator.Generate(propertyType, _context));
                }
            }
        }
        
        private void FillFieldsWithUserGenerators(object obj, Type t)
        {
            var fieldsInfo = t.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fieldsInfo)
            {
                if (_fakerCustom.HasGenerator(t, field.Name))
                {
                    var generator = _fakerCustom.GetGenerator(t, field.Name);
                    field.SetValue(obj, generator.Generate(field.FieldType, _context));
                }
            }
        }

        //Проверяет, что объект является dto(у каждого поля есть публичный геттер и сеттер)
        private bool IsDto(IReflect t)
        {
            var methods = t.GetMethods(BindingFlags.DeclaredOnly |
                                       BindingFlags.Instance | BindingFlags.Public);
            var methodsCount = methods.Length;

            var properties = t.GetProperties(BindingFlags.DeclaredOnly |
                                             BindingFlags.Instance | BindingFlags.Public);
            var propertiesCount = 0;
            foreach (var property in properties)
            {
                if (property.GetMethod is not null && property.GetMethod.IsPublic)
                    propertiesCount += 1;
                if (property.SetMethod is not null && property.SetMethod.IsPublic)
                    propertiesCount += 1;
            }

            return methodsCount - propertiesCount == 0;
        }

        private bool IsGenerated(Type t)
        {
            return _generatedTypes.Exists(genT => genT == t);
        }
    }
}