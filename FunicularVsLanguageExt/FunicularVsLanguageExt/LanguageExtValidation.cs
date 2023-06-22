using LanguageExt;

namespace FunicularVsLanguageExt;

enum Errors
{
    Empty,
    NotCapitalized,
    TooShort
}

[TestClass]
public class LanguageExtValidation
{
    [TestMethod]
    public void TestMethod()
    {
        var subject = "Apple";

        var validationResult = ValidateAll(subject);
        Console.WriteLine(validationResult);

        Validation<Errors, string> IsSet(string str)
        {
            return !string.IsNullOrEmpty(str) ? str : Errors.Empty;
        }

        Validation<Errors, string> IsCapitalized(string str)
        {
            return str.Length >= 1 && char.IsUpper(str[0]) ? str : Errors.NotCapitalized;
        }

        Validation<Errors, string> MustBeAtLeast(string str, uint minLength)
        {
            return str.Length >= minLength ? str : Errors.TooShort;
        }


        Validation<Errors, string> ValidateAll(string str)
        {
            return IsSet(str) | IsCapitalized(str) | MustBeAtLeast(str, 5);
        }
    }
}