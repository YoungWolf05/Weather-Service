using MediatR;
using Weather.Application.Contracts;

namespace Weather.Application.Weather.Queries.ExportCurrentWeather;

public sealed record ExportCurrentWeatherQuery(string Location)
    : IRequest<CurrentWeatherExportDto>;
