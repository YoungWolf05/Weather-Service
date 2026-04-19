using MediatR;
using Weather.Application.Contracts;

namespace Weather.Application.Alerts.Queries.GetTriggeredAlerts;

public sealed record GetTriggeredAlertsQuery(string Email, int Limit = 50) : IRequest<IReadOnlyList<TriggeredAlertResponse>>;
