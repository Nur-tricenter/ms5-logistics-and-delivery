namespace Nurtricenter.Api;

using Joseco.DDD.Core.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

public sealed class DomainExceptionHandler : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken)
	{
		if (exception is not DomainException domainException)
			return false;

		var error = domainException.Error;
		var statusCode = error.Type switch
		{
			ErrorType.Validation => StatusCodes.Status400BadRequest,
			ErrorType.NotFound => StatusCodes.Status404NotFound,
			ErrorType.Problem => StatusCodes.Status422UnprocessableEntity,
			ErrorType.Conflict => StatusCodes.Status409Conflict,
			_ => StatusCodes.Status500InternalServerError
		};

		var problemDetails = new ProblemDetails
		{
			Status = statusCode,
			Title = error.Code,
			Detail = error.Description,
			Type = $"https://errors.nurtricenter.com/{error.Type.ToString().ToLowerInvariant()}"
		};

		if (error is ValidationError validationError)
		{
			problemDetails.Extensions["errors"] = validationError.Errors
				.Select(e => new { code = e.Code, message = e.Description })
				.ToArray();
		}

		httpContext.Response.StatusCode = statusCode;
		httpContext.Response.ContentType = "application/problem+json";

		await httpContext.Response.WriteAsJsonAsync(
			problemDetails,
			cancellationToken);

		return true;
	}
}
