using MediatR;
using Weather.Domain.Contracts.Weather;

namespace Weather.Application.Weather.Queries.GetLocations;

public sealed record GetLocationsQuery(string? Type) : IRequest<IReadOnlyList<LocationResponse>>;


