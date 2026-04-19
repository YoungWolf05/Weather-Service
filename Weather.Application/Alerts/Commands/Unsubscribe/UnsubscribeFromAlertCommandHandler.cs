using MediatR;
using Weather.Application.Abstractions;

namespace Weather.Application.Alerts.Commands.Unsubscribe;

public sealed class UnsubscribeFromAlertCommandHandler(
    IAlertRepository alertRepository) : IRequestHandler<UnsubscribeFromAlertCommand>
{
    public Task Handle(UnsubscribeFromAlertCommand request, CancellationToken cancellationToken)
        => alertRepository.DeactivateSubscriptionAsync(request.SubscriptionId, request.Email, cancellationToken);
}
