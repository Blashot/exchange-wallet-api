using SharedKernel;

namespace Infrastructure.Nbp;

internal static class NbpErrors
{
    public static readonly Error RequestFailed = Error.Failure(
        "Nbp.RequestFailed",
        "The request to the NBP Web API failed due to a network error.");

    public static readonly Error Timeout = Error.Failure(
        "Nbp.Timeout",
        "The request to the NBP Web API timed out.");

    public static readonly Error InvalidJson = Error.Failure(
        "Nbp.InvalidJson",
        "The NBP Web API returned a response that could not be deserialized.");

    public static readonly Error UnexpectedHtmlResponse = Error.Failure(
        "Nbp.UnexpectedHtmlResponse",
        "The NBP Web API returned an HTML page instead of JSON.");

    public static Error UnexpectedStatus(int statusCode) => Error.Failure(
        "Nbp.UnexpectedStatus",
        $"The NBP Web API returned an unexpected HTTP status code: {statusCode}.");
}

