namespace Classes;

public class A
{
    public string StringConstructor { get; }
    public B B { get; set; }

    public int IntField { get; set; }
    public int FieldWithoutSet { get; }
    public int FieldWithPrivateSet { get; private set; }

    public int IntValue;
    public decimal DecimalValue;
    public short ShortValue;
    public string StringValue;

    public string Country { get; }
    public int Age;

    private A()
    {
        StringConstructor = "A()";
    }

    public A(string country)
    {
        StringConstructor = "A(string)";
        Country = country;
    }
}

public class B
{
    public C C { get; set; }
}

public class C
{
    public A A { get; set; }
}