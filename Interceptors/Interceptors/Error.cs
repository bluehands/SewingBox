using Dunet;

namespace Interceptors;

[Union]
public partial record Error
{
    public partial record NotFound(string Message);

    public partial record Validation(string Message);

    public partial record UnexpectedException(Exception Exception, string Details);
}