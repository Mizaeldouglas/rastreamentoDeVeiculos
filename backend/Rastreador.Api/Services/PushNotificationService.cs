using Microsoft.EntityFrameworkCore;
using Rastreador.Api.Data;
using WebPush;

namespace Rastreador.Api.Services;

/// <summary>
/// Envia notificações push (Web Push/VAPID) para todas as inscrições de uma empresa.
/// Falhas de entrega nunca devem derrubar o fluxo principal de alertas — apenas logamos.
/// </summary>
public class PushNotificationService
{
    private readonly VapidDetails _vapidDetails;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(IConfiguration configuration, ILogger<PushNotificationService> logger)
    {
        _logger = logger;
        _vapidDetails = new VapidDetails(
            configuration["Vapid:Subject"],
            configuration["Vapid:PublicKey"],
            configuration["Vapid:PrivateKey"]);
    }

    public async Task SendToCompanyAsync(AppDbContext db, int companyId, string title, string body, CancellationToken cancellationToken)
    {
        var subscriptions = await db.PushSubscriptions
            .Where(p => p.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0) return;

        var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body });
        var client = new WebPushClient();

        foreach (var subscription in subscriptions)
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
                await client.SendNotificationAsync(pushSubscription, payload, _vapidDetails, cancellationToken);
            }
            catch (WebPushException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Gone)
            {
                _logger.LogInformation("Subscription expirada, removendo: {Endpoint}", subscription.Endpoint);
                db.PushSubscriptions.Remove(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao enviar push para {Endpoint}", subscription.Endpoint);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
