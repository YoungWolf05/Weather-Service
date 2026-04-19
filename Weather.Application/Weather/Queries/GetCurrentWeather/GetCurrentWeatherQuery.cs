using MediatR;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.GetCurrentWeather;

public sealed record GetCurrentWeatherQuery(string Location) : IRequest<CurrentWeatherResponse>;
