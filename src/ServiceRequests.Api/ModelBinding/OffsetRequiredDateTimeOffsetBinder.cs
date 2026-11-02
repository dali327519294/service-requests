using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ServiceRequests.Api.ModelBinding;

/// <summary>
/// Accepts a date only with an explicit offset ("Z" or "+03:00"). Without one, parsing would fall back to
/// the server's own time zone, which differs between a developer machine in Israel and a cloud server in UTC.
/// </summary>
internal sealed class OffsetRequiredDateTimeOffsetBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        if (HasOffset(value) && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                "Must be an ISO 8601 date and time with an offset, such as 2026-04-01T00:00:00+03:00.");
        }

        return Task.CompletedTask;
    }

    // Ends with "Z" or with "+hh:mm" / "-hh:mm"
    private static bool HasOffset(string value) =>
        value.EndsWith('Z') || value.EndsWith('z') || (value.Length > 6 && value[^6] is '+' or '-' && value[^3] == ':');
}

/// <summary>Uses <see cref="OffsetRequiredDateTimeOffsetBinder"/> for every DateTimeOffset bound by the API.</summary>
internal sealed class OffsetRequiredDateTimeOffsetBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.UnderlyingOrModelType == typeof(DateTimeOffset) ? new OffsetRequiredDateTimeOffsetBinder() : null;
}
