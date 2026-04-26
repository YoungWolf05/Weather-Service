using MediatR;
using Weather.Domain.Contracts.Weather;

namespace Weather.Application.Weather.Queries.GetHistoricalWeather;

public sealed record GetHistoricalWeatherQuery(
    string Location,
    DateTime From,
    DateTime To) : IRequest<HistoricalWeatherResponse>;


