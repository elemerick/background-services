
using Microsoft.Extensions.Options;
using TennisBookings.External;

namespace TennisBookings.BackgroundServices;

public class WeatherCacheService : BackgroundService
{
	private readonly IWeatherApiClient _weatherApiClient;
	private readonly IDistributedCache<WeatherResult> _cache;
	private readonly ILogger<WeatherCacheService> _logger;

	private readonly int _minutesToCache;
	private readonly int _refreshIntervalInSeconds;

	public WeatherCacheService(IWeatherApiClient weatherApiClient,
		IDistributedCache<WeatherResult> cache,
		IOptionsMonitor<ExternalServicesConfiguration> options,
		ILogger<WeatherCacheService> logger)
	{
		_weatherApiClient = weatherApiClient;
		_cache = cache;
		_logger = logger;
		_minutesToCache = options.Get(ExternalServicesConfiguration.WeatherApi).MinsToCache;
		_refreshIntervalInSeconds = _minutesToCache > 1
			? (_minutesToCache - 1) * 60
			: 30;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var forecast = await _weatherApiClient
				.GetWeatherForecastAsync("Eastbourne", stoppingToken);
			if(forecast is not null)
			{
				var currentWeather = new WeatherResult
				{
					City = "Eastbourne",
					Weather = forecast.Weather,
				};

				var cacheKey = $"current_weather_{DateTime.UtcNow:yyyy_mm_dd}";
				_logger.LogInformation("Updating weather in cache");
				await _cache.SetAsync(cacheKey, currentWeather, _minutesToCache);
			}

			await Task.Delay(TimeSpan.FromSeconds(_refreshIntervalInSeconds), stoppingToken);
		}
	}
}
