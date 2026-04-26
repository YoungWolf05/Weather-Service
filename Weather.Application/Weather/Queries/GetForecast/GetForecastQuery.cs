using MediatR;
using Weather.Domain.Contracts.Weather;

namespace Weather.Application.Weather.Queries.GetForecast;

public sealed record GetForecastQuery(string Location) : IRequest<ForecastResponse>;


