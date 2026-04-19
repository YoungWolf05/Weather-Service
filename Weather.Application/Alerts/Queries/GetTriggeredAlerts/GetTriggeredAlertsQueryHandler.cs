using MediatR;
using Weather.Application.Abstractions;
using Weather.Application.Contracts;

namespace Weather.Application.Alerts.Queries.GetTriggeredAlerts;

public sealed class GetTriggeredAlertsQueryHandler(
    IAlertRepository alertRepository) : IRequestHandler<GetTriggeredAlertsQuery, IReadOnlyList<TriggeredAlertResponse>>
{
    public Task<IReadOnlyList<TriggeredAlertResponse>> Handle(
        GetTriggeredAlertsQuery request,
        CancellationToken cancellationToken)
        => alertRepository.GetTriggeredAlertsForEmailAsync(
            request.Email.ToLowerInvariant(),
            request.Limit,
            cancellationToken);
}
