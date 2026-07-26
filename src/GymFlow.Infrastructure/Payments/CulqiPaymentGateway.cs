using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GymFlow.Application.Abstractions.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GymFlow.Infrastructure.Payments;

/// <summary>
/// Adaptador de la pasarela Culqi sobre su API REST v2. Es el único lugar del sistema
/// que conoce el detalle de Culqi; el resto del código habla con <see cref="IPaymentGateway"/>.
/// No lanza por rechazos de negocio (tarjeta declinada, etc.): los devuelve como resultado.
/// </summary>
public sealed class CulqiPaymentGateway(
    HttpClient http,
    IOptions<CulqiSettings> options,
    ILogger<CulqiPaymentGateway> logger) : IPaymentGateway
{
    private readonly CulqiSettings _settings = options.Value;

    public string Name => "culqi";

    public async Task<PaymentGatewayResult> ChargeAsync(PaymentChargeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            logger.LogWarning("Cobro Culqi solicitado sin llave configurada; se rechaza.");
            return PaymentGatewayResult.Fail("Pasarela de pago no configurada.");
        }

        // Culqi cobra en la unidad mínima (céntimos) y como entero.
        var amountInCents = (int)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero);

        var payload = new CulqiChargeRequest(
            amountInCents, request.Currency, request.Email, request.SourceToken, request.Description);

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/charges")
            {
                Content = JsonContent.Create(payload),
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SecretKey);

            using var response = await http.SendAsync(message, ct);

            if (response.IsSuccessStatusCode)
            {
                var ok = await response.Content.ReadFromJsonAsync<CulqiChargeResponse>(ct);
                return string.IsNullOrWhiteSpace(ok?.Id)
                    ? PaymentGatewayResult.Fail("Culqi no devolvió un id de cargo.")
                    : PaymentGatewayResult.Ok(ok!.Id!);
            }

            var error = await response.Content.ReadFromJsonAsync<CulqiErrorResponse>(ct);
            var reason = error?.UserMessage ?? error?.MerchantMessage ?? $"Culqi respondió {(int)response.StatusCode}.";
            logger.LogInformation("Cobro Culqi rechazado: {Reason}", reason);
            return PaymentGatewayResult.Fail(reason);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Error de red al cobrar con Culqi.");
            return PaymentGatewayResult.Fail("No se pudo contactar la pasarela de pago.");
        }
    }

    private sealed record CulqiChargeRequest(
        [property: JsonPropertyName("amount")] int Amount,
        [property: JsonPropertyName("currency_code")] string CurrencyCode,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("source_id")] string SourceId,
        [property: JsonPropertyName("description")] string Description);

    private sealed record CulqiChargeResponse([property: JsonPropertyName("id")] string? Id);

    private sealed record CulqiErrorResponse(
        [property: JsonPropertyName("user_message")] string? UserMessage,
        [property: JsonPropertyName("merchant_message")] string? MerchantMessage);
}
