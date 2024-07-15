using TennisBookings.Processing;
using TennisBookings.ResultsProcessing;

namespace TennisBookings.DependencyInjection;

public static class TennisResultProcessingServiceCollectionExtensions
{
	public static IServiceCollection AddTennisResultProcessing(this IServiceCollection services,
		IConfiguration config)
	{
		services
			.AddResultProcessing()
			.AddTennisPlayerApiClient(options =>
				options.BaseAddress = config.GetSection("ExternalServices:TennisPlayersApi")["Url"])
			.AddStatisticsApiClient(options =>
				options.BaseAddress = config.GetSection("ExternalServices:StatisticsApi")["Url"])
			.AddSingleton<FileProcessingChannel>();

		return services;
	}
}
