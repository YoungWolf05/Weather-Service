using MediatR;
using Weather.Domain.Abstractions;

namespace Weather.Application.Alerts.Commands.Unsubscribe;

public sealed class UnsubscribeFromAlertCommandHandler(
    IAlertRepository alertRepository) : IRequestHandler<UnsubscribeFromAlertCommand>
{
    public Task Handle(UnsubscribeFromAlertCommand request, CancellationToken cancellationToken)
        => alertRepository.DeleteSubscriptionAsync(request.SubscriptionId, request.Email, cancellationToken);
}


