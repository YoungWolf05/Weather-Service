using MediatR;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.GetHistoricalWeather;

public sealed record GetHistoricalWeatherQuery(
    string Location,
    DateTime From,
    DateTime To) : IRequest<HistoricalWeatherResponse>;
