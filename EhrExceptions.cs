using System.Net;

namespace OpenEhr;

public sealed class EhrApiException : HttpRequestException
{
    public EhrApiException(string operation, HttpStatusCode statusCode)
        : base($"{operation}: HTTP {(int)statusCode}", null, statusCode) { }
}

public sealed class EhrContractException : Exception
{
    public EhrContractException(string message, Exception? inner = null) : base(message, inner) { }
}
