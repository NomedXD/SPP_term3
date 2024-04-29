namespace Generator.src.main;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Formatter = Microsoft.CodeAnalysis.Formatting.Formatter;
using System.Collections.Concurrent;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

#pragma warning disable CS1998

public class Generator
{
    // Главный метод генератора, который работает с полным текстом файла класса(параметр text)
    public async Task<ConcurrentDictionary<string, string>> getNamesAndContents(string text)
    {
        return await getNamesAndContentsAsync(text);
    }

    private async Task<ConcurrentDictionary<string, string>> getNamesAndContentsAsync(string text)
    {
        // Получение дерева документа с классом
        var syntaxTree = CSharpSyntaxTree.ParseText(text);
        // Получение кореня синтаксического дерева
        var root = syntaxTree.GetRoot();
        // Создание новой компиляции с нуля(на основе полученного дерева класса)
        var compilation = CSharpCompilation.Create("MyCompilation").AddSyntaxTrees(syntaxTree);
        // Семантическая модель для нашего указанного дерева
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        // Также получаем все классы, которые объявлены внутри корня дерева(у нас один класс)
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        // Пустой словарь(потокобезопасный)
        var namesAndContents = new ConcurrentDictionary<string, string>();
        // Parallel.ForEach позволяет выполнять итерации цикла параллельно всеми потоками блока
        Parallel.ForEach(classes, clazz =>
        {
            // Создаем юнит класса
            var unit = generateUnit(clazz, semanticModel);
            // Создаем полное представление класса на основе юнита
            var unitView = getUnitView(unit);
            // Добавляем созданный класс в общую map(имя класса, представление)
            namesAndContents.TryAdd(clazz.Identifier.ValueText, unitView);
        });
        return namesAndContents;
    }

    // На основе полученного дерева деклараций(юнита) строим полное представление полученного класса
    private string getUnitView(CompilationUnitSyntax compilationUnitSyn)
    {
        var workspace = new AdhocWorkspace();
        var options = workspace.Options;
        options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, false);
        options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInTypes, false);
        var formattedNode = Formatter.Format(compilationUnitSyn, workspace, options);
        using
        var stringWriter = new StringWriter();
        formattedNode.WriteTo(stringWriter);
        return stringWriter.ToString();
    }

    // WRAPPER GENERATOR
    public CompilationUnitSyntax generateUnit(ClassDeclarationSyntax Class, SemanticModel semanticModel)
    {
        // Создание пустого юнита нашего тестового класса
        var compilationUnit = CompilationUnit().AddUsings(UsingDirective(IdentifierName("System"))).AddUsings(UsingDirective(IdentifierName("System.Data"))).AddUsings(UsingDirective(IdentifierName("System.Collections.Generic"))).AddUsings(UsingDirective(IdentifierName("System.Linq"))).AddUsings(UsingDirective(IdentifierName("System.Text"))).AddUsings(UsingDirective(IdentifierName("System.Runtime.Serialization"))).AddUsings(UsingDirective(IdentifierName("Program.src.main"))).AddUsings(UsingDirective(IdentifierName("Moq"))).AddUsings(UsingDirective(IdentifierName("NUnit.Framework")));
        // Получение узлов дерева, а именна конструкторов
        var constructorSyns = Class.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
        ConstructorDeclarationSyntax? savedConstructorSyn = null;
        List<ParameterSyntax>? savedParameterSyns = null;
        var interfaceMembersMaxAmount = -1;
        var interfaceMembersAmount = 0;
        // Если для класса, для которого создается тест, есть хоть один конструктор
        if (constructorSyns.Any())
        {
            // Смотрим каждый объявленный конструктор
            foreach (var constructorSyn in constructorSyns)
            {
                // Получаем синтаксисы объявленных параметров консруктора
                var tempParameterSyns = new List<ParameterSyntax>();
                interfaceMembersAmount = 0;
                // Смотрим каждый такой параметр
                foreach (var parameterSyn in constructorSyn.ParameterList.Parameters)
                {
                    // Получаем из узла дерева символ параметра(токен)
                    var parameterSymbol = semanticModel.GetDeclaredSymbol(parameterSyn);
                    // Если этот параметр = интерфейс
                    if (parameterSymbol!.Type.TypeKind == TypeKind.Interface || parameterSymbol.Type.Name.Length > 2 && parameterSymbol.Type.Name[0] == 'I' && char.IsUpper(parameterSymbol.Type.Name[1]))
                    {
                        // Сохраняем этот параметр во временные(чтобы дальше использовать для создания Mock параметров-интерфейсов)
                        tempParameterSyns.Add(parameterSyn);
                        // Также увеличиваем общее количество таких параметров
                        interfaceMembersAmount++;
                    }
                }
                // Если хотя бы один конструктор является публичным
                if (constructorSyn.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
                {
                    // Если были найдены интерфейсы в параметрах
                    if (interfaceMembersAmount > interfaceMembersMaxAmount)
                    {
                        // То нам нужно использовать тот конструктор, где были эти интерфейсы в параметрах, а не другие конструкторы
                        interfaceMembersMaxAmount = interfaceMembersAmount;
                        savedConstructorSyn = constructorSyn;
                        savedParameterSyns = tempParameterSyns;
                    }
                }
            }
        }
        // Если конструкторы не были найдены, то создаем пустой публичный конструктор
        else
        {
            savedConstructorSyn = ConstructorDeclaration(Identifier(Class.Identifier.ValueText)).WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword))).WithBody(Block());
            savedParameterSyns = new List<ParameterSyntax>();
        }
        // Пространство имен для нашего тестовго класса(узел дерева)
        var namespaceSyn = NamespaceDeclaration(IdentifierName("Tests"));
        // Генерация имени тестовго класса(узел дерева)
        var classSyn = generateTestClass(Class.Identifier.ValueText);
        var parameterSynQuantity = 1;
        // В случае, если были параметры интерфейсы, то создаем для них моки
        foreach (var parameterSyn in savedParameterSyns!)
        {
            // Добавляем моки в создаваемый тестовый класс(для нужного типа интефейса, количество параметров = 1)
            classSyn = classSyn.AddMembers(generateField($"Mock<{parameterSyn.Type}>", $"_dependency{parameterSynQuantity}"));
            parameterSynQuantity++;
        }
        // Добавляем поле - ссылка на экземпляр тестируемого класса
        classSyn = classSyn.AddMembers(generateField(Class.Identifier.ValueText, $"_myClassUnderTest"));
        // Добавление в класс метода инициализации данных(полей)
        classSyn = classSyn.AddMembers(generateSetUpMethod(savedConstructorSyn!, semanticModel, Class));
        // Получаем узлы(декларации) всех публичных методов, для которых нужно сгенерировать тестовые методы
        var publicMethods = Class.Members.OfType<MethodDeclarationSyntax>().Where(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)));
        foreach (var publicMethod in publicMethods)
        {
            // Создаем и добавляем в класс тестовые методы
            classSyn = classSyn.AddMembers(generateTestMethod(publicMethod, semanticModel));
        }
        // Добавляем в созданное пространство имен созданный класс
        namespaceSyn = namespaceSyn.AddMembers(classSyn);
        // Добавляем в итоговый юнит созданное пространство имен
        compilationUnit = compilationUnit.AddMembers(namespaceSyn);
        return compilationUnit;
    }

    // Генерирует узел синтаксического дерева(объявление имени тестовго класса)
    private ClassDeclarationSyntax generateTestClass(string name)
    {
        // Также добавляем ключевое слово public для тестового класса с указанным атрибутов(в квадратных скобочках над классом)
        var classSyn = ClassDeclaration(name + "Tests").AddModifiers(Token(SyntaxKind.PublicKeyword)).WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("TestFixture"))))));
        return classSyn;
    }

    // PRIMARY GENERATORS
    private FieldDeclarationSyntax generateField(string type, string name)
    {
        var variableSyn = VariableDeclaration(IdentifierName(type)).AddVariables(VariableDeclarator(Identifier(name)));
        var fieldSyn = FieldDeclaration(variableSyn).AddModifiers(Token(SyntaxKind.PrivateKeyword));
        return fieldSyn;
    }

    // Метод создает узел(синтаксис) метода, который инициализирует моки и другие зависимости класса
    private MethodDeclarationSyntax generateSetUpMethod(ConstructorDeclarationSyntax constructorSyn, SemanticModel semanticModel, ClassDeclarationSyntax classSyn)
    {
        var statementSyns = new List<StatementSyntax>();
        var argumentSyns = new List<ArgumentSyntax>();
        int ifaceQuantity = 1;
        int paramQuantity = 1;
        // Все параметры нужного нам конструктора(со всеми зависимостями)
        var parameterSyns = constructorSyn.ParameterList.Parameters;
        // Смотрим все такие параметры
        foreach (var parameterSyn in parameterSyns)
        {
            // Получаем символ параметра(токен)
            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameterSyn);
            // Если этот параметр интерфейс
            if (parameterSymbol!.Type.TypeKind == TypeKind.Interface || parameterSymbol.Type.Name.Length > 2 && parameterSymbol.Type.Name[0] == 'I' && char.IsUpper(parameterSymbol.Type.Name[1]))
            {
                // Генерация выражения присваивания
                var statementSyn = generateMoqType(parameterSyn.Type!, $"_dependency{ifaceQuantity}");
                // Генерация аргумента, который будет передан при создании экземпляра-зависимости тестируемого класса
                var argumentSyn = Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName($"_dependency{ifaceQuantity}"), IdentifierName("Object")));
                // Добавление созданного в нужные списки
                argumentSyns.Add(argumentSyn);
                statementSyns.Add(statementSyn);
                ifaceQuantity++;
            }
            // Если этот параметр примитив
            else
            {
                // Генерация выражения присваивания для примитива
                var variableSyn = generatePrimitiveType(parameterSyn.Type!, $"param{paramQuantity}");
                // Генерация аргумента, который будет передан при создании экземпляра-зависимости тестируемого класса
                var argumentSyn = Argument(IdentifierName($"param{paramQuantity}"));
                // Добавление созданного в нужные списки
                argumentSyns.Add(argumentSyn);
                paramQuantity++;
                statementSyns.Add(LocalDeclarationStatement(variableSyn));
            }
        }
        // Получаем спиоск отдельных синтаксических узлов 
        var argumentList = ArgumentList(SeparatedList(argumentSyns));
        // Генерируем сигнатуру выражения присвоения для зависимости тестируемого класса
        var expressionStatementSyn = ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("_myClassUnderTest"), ObjectCreationExpression(IdentifierName(classSyn.Identifier.ValueText)).WithArgumentList(argumentList)));
        // Добавляем созданную сигнатуру в общий список
        statementSyns.Add(expressionStatementSyn);
        // Создаем декларацию метода установки всех зависимостей
        var methodSynResult = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier("SetUp")).AddModifiers(Token(SyntaxKind.PublicKeyword)).AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("SetUp"))))).WithBody(Block(statementSyns));
        return methodSynResult;
    }

    // Метод создает узел(синтаксис) тестового метода
    private MethodDeclarationSyntax generateTestMethod(MethodDeclarationSyntax methodSyn, SemanticModel semanticModel)
    {
        var statementSyns = new List<StatementSyntax>();
        var argumentSyns = new List<ArgumentSyntax>();
        // Все параметры нужного нам метода
        var parameterSyns = methodSyn.ParameterList.Parameters;
        // Смотрим каждый из узлов-параметров
        foreach (var parameterSyn in parameterSyns)
        {
            // Получаем символ параметра метода(токен)
            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameterSyn);
            // Если типом этого параметра этого метода является тип интерфейс, то для него также нужно создать мок
            if (parameterSymbol!.Type.TypeKind == TypeKind.Interface || parameterSymbol.Type.Name.Length > 2 && parameterSymbol.Type.Name[0] == 'I' && char.IsUpper(parameterSymbol.Type.Name[1]))
            {
                // Генерация выражения присваивания для инициализации и создания данного параметра
                var statementSyn = generateMoqType(parameterSyn.Type!, parameterSyn.Identifier.ValueText);
                // Генерация самого аргумента, который будет передан в метод
                var argumentSyn = Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(parameterSyn.Identifier.ValueText), IdentifierName("Object")));
                // Добавление созданного в нужные списки
                argumentSyns.Add(argumentSyn);
                statementSyns.Add(statementSyn);
            }
            // Если этот параметр примитив
            else
            {
                // Генерация выражения присваивания для примитива
                var variableSyn = generatePrimitiveType(parameterSyn.Type!, parameterSyn.Identifier.ValueText);
                // Генерация самого аргумента, который будет передан в метод
                var argumentSyn = Argument(IdentifierName(parameterSyn.Identifier.ValueText));
                // Добавление созданного в нужные списки
                argumentSyns.Add(argumentSyn);
                statementSyns.Add(LocalDeclarationStatement(variableSyn));
            }
        }
        // Получаем спиоск отдельных синтаксических узлов-параметров метода
        var argumentList = ArgumentList(SeparatedList(argumentSyns));
        // Если генерируемый метод ничего не возвращает
        if (methodSyn.ReturnType.ToString() == "void")
        {
            // Создаем выражение передачи в Assert.DoesNotThrow лямбды вызова нашего метода
            var statementSyn = ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Assert"), IdentifierName("DoesNotThrow"))).WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(ParenthesizedLambdaExpression().WithBlock(Block(SingletonList<StatementSyntax>(ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("_myClassUnderTest"), IdentifierName(methodSyn.Identifier.ValueText))).WithArgumentList(argumentList))))))))));
            statementSyns.Add(statementSyn);
        }
        // Если метод имеет возвращаемое значение
        else
        {
            // Получение типа возвращаемого значения
            var typeSyntax = methodSyn.ReturnType;
            // Создание синтаксиса-декларации объявления и инициализации переменной, результат которой - вызов создаваемого здесь метода
            var variableSyn = VariableDeclaration(typeSyntax, SingletonSeparatedList(VariableDeclarator(Identifier("actual")).WithInitializer(EqualsValueClause(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("_myClassUnderTest"), IdentifierName(methodSyn.Identifier.ValueText))).WithArgumentList(argumentList)))));
            // Добавление этого выражения в общий список
            statementSyns.Add(LocalDeclarationStatement(variableSyn));
            // Аналогично с ожидаемым результатом(делаем его для простоты примитивом)
            var statementSyn1 = generatePrimitiveType(typeSyntax, "expected");
            statementSyns.Add(LocalDeclarationStatement(statementSyn1));
            // Создание выражение Assert.That для сравнение полученных результатов выполнения тестового метода
            var statementSyn2 = ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Assert"), IdentifierName("That"))).WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[]
            {
                Argument(IdentifierName("actual")),
                    Token(SyntaxKind.CommaToken),
                    Argument(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Is"), IdentifierName("EqualTo"))).WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(IdentifierName("expected"))))))
            }))));
            statementSyns.Add(statementSyn2);
        }
        // Создание выражения Assert.Fail(просто как заглушка с доп логикой)
        var expressionStatementSyn = ExpressionStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Assert"), IdentifierName("Fail"))).WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("autogenerated")))))));
        statementSyns.Add(expressionStatementSyn);
        // Создаем декларацию метода
        var methodSynResult = MethodDeclaration(PredefinedType(Token(SyntaxKind.VoidKeyword)), Identifier(methodSyn.Identifier.ValueText + "Test")).AddModifiers(Token(SyntaxKind.PublicKeyword)).AddAttributeLists(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("Test"))))).WithBody(Block(statementSyns));
        return methodSynResult;
    }

    // Генерирует сигнатуру выражения присвоения(для примитивов)
    private VariableDeclarationSyntax generatePrimitiveType(TypeSyntax typeSyn, string name)
    {
        return VariableDeclaration(typeSyn, SingletonSeparatedList(VariableDeclarator(Identifier(name)).WithInitializer(EqualsValueClause(DefaultExpression(typeSyn)))));
    }

    // Генерирует сигнатуру выражения присвоения(для ссылочных зависимостей, а именно интерфейсов)
    private ExpressionStatementSyntax generateMoqType(TypeSyntax typeSyn, string name)
    {
        return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(name), ObjectCreationExpression(GenericName(Identifier("Mock")).WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList(typeSyn)))).WithArgumentList(ArgumentList())));
    }
}