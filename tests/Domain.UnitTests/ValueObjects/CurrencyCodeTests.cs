using Domain.Shared;

namespace Domain.UnitTests.ValueObjects;

public sealed class CurrencyCodeTests
{
    
    [Fact]
    public void Constructor_ValidUppercaseCode_ShouldStoreValue()
    {
        CurrencyCode code = new("USD");

        code.Value.ShouldBe("USD");
    }

    [Fact]
    public void Constructor_LowercaseCode_ShouldNormaliseToUppercase()
    {
        CurrencyCode code = new("usd");

        code.Value.ShouldBe("USD");
    }

    [Fact]
    public void Constructor_MixedCaseCode_ShouldNormaliseToUppercase()
    {
        CurrencyCode code = new("eUr");

        code.Value.ShouldBe("EUR");
    }

    [Fact]
    public void Constructor_CodeWithLeadingTrailingWhitespace_ShouldTrim()
    {
        CurrencyCode code = new("  pln  ");

        code.Value.ShouldBe("PLN");
    }
    
    [Fact]
    public void Constructor_NullValue_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new CurrencyCode(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_EmptyString_ShouldThrowArgumentException()
    {
        Action act = () => _ = new CurrencyCode("");

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhitespaceOnly_ShouldThrowArgumentException()
    {
        Action act = () => _ = new CurrencyCode("   ");

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("A")] 
    [InlineData("ABCDE")]
    public void Constructor_WrongLength_ShouldThrowArgumentException(string code)
    {
        Action act = () => _ = new CurrencyCode(code);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData("US1")]
    [InlineData("1SD")]
    [InlineData("U-D")]
    [InlineData("U D")]
    public void Constructor_NonLetterCharacters_ShouldThrowArgumentExceptionWithMessage(string code)
    {
        Action act = () => _ = new CurrencyCode(code);

        ArgumentException ex = act.ShouldThrow<ArgumentException>();
        ex.Message.ShouldNotBeEmpty();
    }
    
    [Fact]
    public void Equality_SameCode_ShouldBeEqual()
    {
        CurrencyCode a = new("USD");
        CurrencyCode b = new("USD");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_SameCodeDifferentCase_ShouldBeEqual()
    {
        CurrencyCode a = new("usd");
        CurrencyCode b = new("USD");

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_DifferentCodes_ShouldNotBeEqual()
    {
        CurrencyCode a = new("USD");
        CurrencyCode b = new("EUR");

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }
    
    [Fact]
    public void PLN_Constant_ShouldHaveValuePLN()
    {
        CurrencyCode.PLN.Value.ShouldBe("PLN");
    }

    [Fact]
    public void PLN_Constant_ShouldEqualNewPLNInstance()
    {
        CurrencyCode.PLN.ShouldBe(new CurrencyCode("PLN"));
    }
    
    [Fact]
    public void ToString_ShouldReturnValue()
    {
        CurrencyCode code = new("EUR");

        code.ToString().ShouldBe("EUR");
    }
}


