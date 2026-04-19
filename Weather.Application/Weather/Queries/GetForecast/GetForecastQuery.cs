using MediatR;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.GetForecast;

public sealed record GetForecastQuery(string Location) : IRequest<ForecastResponse>;
