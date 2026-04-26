using MediatR;
using Weather.Domain.Contracts.Weather;

namespace Weather.Application.Weather.Queries.GetCurrentWeather;

public sealed record GetCurrentWeatherQuery(string Location) : IRequest<CurrentWeatherResponse>;


