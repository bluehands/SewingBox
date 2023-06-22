using FunicularSwitch;

namespace FunicularVsLanguageExt;

[TestClass]
public class FunicularValidation
{
    [TestMethod]
    public void TestMethod()
    {
        var subject = "";

        var validationResult = subject.Validate(ValidationFun, ", ");
        
        Console.WriteLine(validationResult);

        bool IsSet(string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        bool IsCapitalized(string str)
        {
            return str.Length >= 1 && char.IsUpper(str[0]);
        }

        bool MustBeAtLeast(string str, uint minLength)
        {
            return str.Length >= minLength;
        }
        
        IEnumerable<string> ValidationFun(string s)
        {
            if (!IsSet(s))
            {
                yield return "String must not be empty";
            }
            
            if (!IsCapitalized(s))
            {
                yield return "String must start with an uppercase letter";
            }

            if (!MustBeAtLeast(s, 5))
            {
                yield return "String must be at least 5 characters long";
            }
        }
    }
}