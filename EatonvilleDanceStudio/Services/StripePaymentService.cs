using Stripe.Checkout;

namespace EatonvilleDanceStudio.Services;

/// <summary>
/// Provides an implementation of the <see cref="IStripePaymentService"/> interface for managing Stripe payment sessions.
/// This service utilizes the Stripe Checkout API to create and retrieve payment sessions.
/// </summary>
public class StripePaymentService : IStripePaymentService
{
    private readonly SessionService _sessionService = new();

    public async Task<Session> CreateSessionAsync(SessionCreateOptions options, Stripe.RequestOptions requestOptions)
    {
        return await _sessionService.CreateAsync(options, requestOptions);
    }

    public async Task<Session> GetSessionAsync(string sessionId)
    {
        return await _sessionService.GetAsync(sessionId);
    }
}