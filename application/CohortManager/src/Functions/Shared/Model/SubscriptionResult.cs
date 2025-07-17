namespace Model;

/// <summary>
/// Represents the result of a subscription operation, including success status and subscription details.
/// </summary>
public class SubscriptionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the subscription operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the subscription ID if the operation was successful.
    /// </summary>
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// Gets or sets an error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful subscription result with the specified subscription ID.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>A successful SubscriptionResult.</returns>
    public static SubscriptionResult CreateSuccess(string subscriptionId)
    {
        return new SubscriptionResult
        {
            Success = true,
            SubscriptionId = subscriptionId
        };
    }

    /// <summary>
    /// Creates a failed subscription result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed SubscriptionResult.</returns>
    public static SubscriptionResult CreateFailure(string errorMessage)
    {
        return new SubscriptionResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}