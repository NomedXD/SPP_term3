using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AssemblyBrowserLibrary
{
    public class AssemblyBrowser : IAssemblyBrowser
    {
        // Список всех методов расширения
        private readonly List<MethodInfo> _extensionMethods = new List<MethodInfo>();

        // Добавление всех методов расширения
        private void AddExtensionMethods(ContainerInfo[] containers)
        {
            foreach (var method in _extensionMethods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length < 0) continue;
                var param = parameters[0];
                var extentedType = param.ParameterType;
                foreach (var container in containers)
                {
                    if (container.DeclarationName != extentedType.Namespace) continue;
                    var types = container.Members;
                    foreach (var type in types)
                    {
                        if (type.DeclarationName == GetTypeDeclaration(extentedType.GetTypeInfo()))
                        {
                            ((TypeInfo)type).AddMember(new MemberInfo() { DeclarationName = "ext. method: " + CreateExtensionMethodDeclarationString(method), Name = method.Name });
                        }
                    }
                }
            }
        }

        public ContainerInfo[] GetNamespaces(string assemblyPath)
        {
            // Загрузка сборки(dll), которая парсится
            var assembly = Assembly.LoadFile(assemblyPath);
            // Все типы, объявленные в сборке
            var types = assembly.GetTypes();
            // Словарь всех пространств имен(имя пространства имен, его внутренности)
            var namespaces = new Dictionary<string, ContainerInfo>();
            // Проходим по всем типам в загруженной сборки(dll)
            foreach (var type in types)
            {
                // Получаем прострнство имен текущего типа
                var typeNamespace = type.Namespace;
                // null, если экземпляр не имеет пространства имен
                if (typeNamespace == null) continue;
                // Новый контейнер, для пространства имен текущего типа
                ContainerInfo namespaceInfo;
                // Если пока что словарь всех пространств имен не содержин пространства обрабатываемого типа
                if (!namespaces.ContainsKey(typeNamespace))
                {
                    // Тогда новый контейнер инициализируется именем пространства текущего типа
                    namespaceInfo = new NamespaceInfo { DeclarationName = typeNamespace };
                    // И добавляется в общую map всех пространств загруженной dll
                    namespaces.Add(typeNamespace, namespaceInfo);
                }
                else
                {
                    namespaces.TryGetValue(typeNamespace, out namespaceInfo);
                }
                // Получение полной информации текущего типа
                var typeInfo = GetTypeInfo(type);
                // Добавляем данный внутренний элемент пространства имен в массив внутренних
                // элементов данного пространства
                namespaceInfo?.AddMember(typeInfo);
            }
            // Получение массива полной информации о всех пространствах имен
            // На основе сформированной Map пространств
            ContainerInfo[] result = namespaces.Values.ToArray();
            AddExtensionMethods(result);

            return result;
        }

        // Возвращает имя типа
        private string GetTypeName(Type type)
        {
            // Пространство имет типа + наименование типа
            var result = $"{type.Namespace}.{type.Name}";
            // Если переданный тип дженерик, то добавляем еще и эту информацию
            if (type.IsGenericType)
            {
                result += GetGenericArgumentsString(type.GetGenericArguments());
            }
            return result;
        }

        // Получние имени метода
        private string GetMethodName(MethodBase method)
        {
            // Если переданный метод обобщенный
            if (method.IsGenericMethod)
            {
                // Все обобщенные параметры данного дженерик метода
                return method.Name + GetGenericArgumentsString(method.GetGenericArguments());
            }
            return method.Name;
        }
        
        // Получение дженерик параметров данного типа
        private string GetGenericArgumentsString(Type[] arguments)
        {
            var genericArgumentsString = new StringBuilder("<");
            for (int i = 0; i < arguments.Length; i++)
            {
                // Добавляем эти параметры в общую строку
                genericArgumentsString.Append(GetTypeName(arguments[i]));
                if (i != arguments.Length - 1)
                {
                    genericArgumentsString.Append(", ");
                }
            }
            genericArgumentsString.Append(">");

            return genericArgumentsString.ToString();
        }

        // Возвращает DeclarationName метода
        private string CreateMethodDeclarationString(MethodInfo methodInfo)
        {
            // Получение имени типа возвращаемого значения
            var returnType = GetTypeName(methodInfo.ReturnType);
            // Параметры метода
            var parameters = methodInfo.GetParameters();
            // Возвращает объявление метода(модификатор доступа) + имя метода + имена типов параметров
            var declaration =
                $"{GetMethodDeclaration(methodInfo)} {returnType} {GetMethodName(methodInfo)} {GetMethodParametersString(parameters)}";

            return declaration;
        }

        private string CreateExtensionMethodDeclarationString(MethodInfo methodInfo)
        {
            var returnType = GetTypeName(methodInfo.ReturnType);
            var parameters = new List<ParameterInfo>(methodInfo.GetParameters());
            parameters.RemoveAt(0);
            var declaration =
                $"{GetMethodDeclaration(methodInfo)} {returnType} {GetMethodName(methodInfo)} {GetMethodParametersString(parameters.ToArray())}";

            return declaration;
        }

        // Получение названий всех типов параметров метода
        private string GetMethodParametersString(ParameterInfo[] parameters)
        {
            var parametersString = new StringBuilder("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                parametersString.Append(GetTypeName(parameters[i].ParameterType));
                if (i != parameters.Length - 1)
                {
                    parametersString.Append(", ");
                }
            }
            parametersString.Append(")");

            return parametersString.ToString();
        }

        // Возвращает часть сигнатуры(модификатор доступа + информация, если это класс/делегат)
        // Внутрь передается объяявление типа, дял данного типа
        private string GetTypeDeclaration(System.Reflection.TypeInfo typeInfo)
        {
            var result = new StringBuilder();
            // Модификаторы доступа
            if (typeInfo.IsNestedPublic || typeInfo.IsPublic)
                result.Append("public ");
            else if (typeInfo.IsNestedPrivate)
                result.Append("private ");
            else if (typeInfo.IsNestedFamily)
                result.Append("protected ");
            else if (typeInfo.IsNestedAssembly)
                result.Append("internal ");
            else if (typeInfo.IsNestedFamORAssem)
                result.Append("protected internal ");
            else if (typeInfo.IsNestedFamANDAssem)
                result.Append("private protected ");
            else if (typeInfo.IsNotPublic)
                result.Append("private ");
            // Абстрактность
            if (typeInfo.IsAbstract && typeInfo.IsSealed)
                result.Append("static ");
            else if (typeInfo.IsAbstract)
                result.Append("abstract ");
            else if (typeInfo.IsSealed)
                result.Append("sealed ");
            // Если это класс или делегат(не интерфейс и не тип значения)
            if (typeInfo.IsClass)
                result.Append("class ");
            else if (typeInfo.IsEnum)
                result.Append("enum ");
            else if (typeInfo.IsInterface)
                result.Append("interface ");
            else if (typeInfo.IsGenericType)
                result.Append("generic ");
            else if (typeInfo.IsValueType && !typeInfo.IsPrimitive)
                result.Append("struct ");

            result.Append($"{GetTypeName(typeInfo.AsType())} ");

            return result.ToString();
        }

        // Возвращает объявление метода(модификатор доступа)
        private string GetMethodDeclaration(MethodBase methodBase)
        {
            var result = new StringBuilder();
            // Модификатор доступа метода
            if (methodBase.IsAssembly)
                result.Append("internal ");
            else if (methodBase.IsFamily)
                result.Append("protected ");
            else if (methodBase.IsFamilyOrAssembly)
                result.Append("protected internal ");
            else if (methodBase.IsFamilyAndAssembly)
                result.Append("private protected ");
            else if (methodBase.IsPrivate)
                result.Append("private ");
            else if (methodBase.IsPublic)
                result.Append("public ");

            if (methodBase.IsStatic)
                result.Append("static ");
            else if (methodBase.IsAbstract)
                result.Append("abstract ");
            else if (methodBase.IsVirtual)
                result.Append("virtual ");

            return result.ToString();
        }

        // Возвращает полное объявление свойства
        private string GetPropertyDeclaration(PropertyInfo propertyInfo)
        {
            var result = new StringBuilder(GetTypeName(propertyInfo.PropertyType));
            result.Append(" ");
            // Имя самого свойства
            result.Append(propertyInfo.Name);
            // геттеры и сеттеры свойства
            var accessors = propertyInfo.GetAccessors(true);
            foreach (var accessor in accessors)
            {
                // Если геттер/сеттер свойства имеет специальное(не базовое) имя
                if (accessor.IsSpecialName)
                {
                    result.Append(" { ");
                    // Добавление также к строке и этого уникального имени
                    result.Append(accessor.Name);
                    result.Append(" } ");
                }
            }

            return result.ToString();
        }

        // Возвращает полное объявление ивента
        private string GetEventDeclaration(EventInfo eventInfo)
        {
            var result = new StringBuilder();
            // Тип ивента + имя ивента
            result.Append($"{GetTypeName(eventInfo.EventHandlerType)} {eventInfo.Name}");
            result.Append($" [{eventInfo.AddMethod.Name}] ");
            result.Append($" [{eventInfo.RemoveMethod.Name}] ");

            return result.ToString();
        }

        // Возвращает полное объявление поля
        private string GetFieldDeclaration(FieldInfo fieldInfo)
        {
            var result = new StringBuilder();
            // Модификатор доступа поля
            if (fieldInfo.IsAssembly)
                result.Append("internal ");
            else if (fieldInfo.IsFamily)
                result.Append("protected ");
            else if (fieldInfo.IsFamilyOrAssembly)
                result.Append("protected internal ");
            else if (fieldInfo.IsFamilyAndAssembly)
                result.Append("private protected ");
            else if (fieldInfo.IsPrivate)
                result.Append("private ");
            else if (fieldInfo.IsPublic)
                result.Append("public ");

            if (fieldInfo.IsInitOnly)
                result.Append("readonly ");
            if (fieldInfo.IsStatic)
                result.Append("static ");
            // Также добавляем имя типа поля
            result.Append(GetTypeName(fieldInfo.FieldType));
            result.Append(" ");
            // Добавляем имя самого поля
            result.Append(fieldInfo.Name);

            return result.ToString();
        }

        //Возвращает полное объявление конструктора
        private string GetConstructorDeclaration(ConstructorInfo constructorInfo)
        {
            // Модификатор доступа конструктора + его имя + его параметры
            return
                $"{GetMethodDeclaration(constructorInfo)} {GetMethodName(constructorInfo)} {GetMethodParametersString(constructorInfo.GetParameters())}";
        }

        private TypeInfo GetTypeInfo(Type type)
        {
            var typeInfo = new TypeInfo() 
            {
                // Получение объявления данного типа
                // В метод передается представление данного типа, дял данного типа
                DeclarationName = GetTypeDeclaration(type.GetTypeInfo()),
                // Имя типа
                Name = type.Name
            };
            // Поиск внутренних членов переданного типа
            var members = type.GetMembers(BindingFlags.NonPublic
                                          | BindingFlags.Instance
                                          | BindingFlags.Public
                                          | BindingFlags.Static);
            
            foreach (var member in members)
            {
                // Создаем переменную-информацию о внутреннем member
                var memberInfo = new MemberInfo();
                // Если внутренний элемент - метод
                if (member.MemberType == MemberTypes.Method)
                {
                    // Приводим внутренний элмент к типу информации о методе
                    var method = (MethodInfo)member;
                    // Если данный метод является методом расширения
                    if (method.IsDefined(typeof(ExtensionAttribute), false))
                    {
                        // Общий массив таких методов
                        _extensionMethods.Add(method);
                    }
                    // Получаем полную сигнатуру метода
                    memberInfo.DeclarationName = CreateMethodDeclarationString(method);
                }
                // Если внутренний элемент - свойство
                else if (member.MemberType == MemberTypes.Property)
                {
                    // Получаем полное объявление сойства
                    memberInfo.DeclarationName = GetPropertyDeclaration((PropertyInfo)member);
                }
                // Если внутренний элемент - поле
                else if (member.MemberType == MemberTypes.Field)
                {
                    // Получаем полное объявление поля
                    memberInfo.DeclarationName = GetFieldDeclaration(((FieldInfo)member));
                }
                // Если внутренний элемент - событие
                else if (member.MemberType == MemberTypes.Event)
                {
                    // Получение полного объявления события
                    memberInfo.DeclarationName = GetEventDeclaration((EventInfo)member);
                }
                // Если внутренний элемент - конструктор
                else if (member.MemberType == MemberTypes.Constructor)
                {
                    // Получение полного объявления конструктора
                    memberInfo.DeclarationName = GetConstructorDeclaration((ConstructorInfo)member);
                }
                else
                {
                    memberInfo.DeclarationName = GetTypeDeclaration((System.Reflection.TypeInfo)member);
                }
                // Если получилось сформировать DeclarationName внутреннего элемента
                if (memberInfo.DeclarationName != null)
                {
                    // Добавляем его в массив всех member элемента уровня выше
                    memberInfo.Name = member.Name;
                    typeInfo.AddMember(memberInfo);
                }
            }

            return typeInfo;
        }
    }
}
