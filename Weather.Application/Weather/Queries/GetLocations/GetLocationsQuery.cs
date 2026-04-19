using MediatR;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.GetLocations;

public sealed record GetLocationsQuery(string? Type) : IRequest<IReadOnlyList<LocationResponse>>;
