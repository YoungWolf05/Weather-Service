using MediatR;
using Weather.Domain.Contracts.Weather;

namespace Weather.Application.Weather.Queries.ExportCurrentWeather;

public sealed record ExportCurrentWeatherQuery(string Location)
    : IRequest<CurrentWeatherExportDto>;


