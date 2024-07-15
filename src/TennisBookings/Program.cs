#region Global Usings
global using Microsoft.AspNetCore.Identity;

global using TennisBookings;
global using TennisBookings.Data;
global using TennisBookings.Domain;
global using TennisBookings.Extensions;
global using TennisBookings.Configuration;
global using TennisBookings.Caching;
global using TennisBookings.Shared.Weather;
global using TennisBookings.DependencyInjection;
global using TennisBookings.Services.Bookings;
global using TennisBookings.Services.Greetings;
global using TennisBookings.Services.Unavailability;
global using TennisBookings.Services.Bookings.Rules;
global using TennisBookings.Services.Notifications;
global using TennisBookings.Services.Time;
global using TennisBookings.Services.Staff;
global using TennisBookings.Services.Courts;
global using TennisBookings.Services.Security;
global using Microsoft.EntityFrameworkCore;
#endregion

using Microsoft.Data.Sqlite;
using TennisBookings.BackgroundServices;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

using var connection = new SqliteConnection(builder.Configuration
	.GetConnectionString("SqliteConnection"));

await connection.OpenAsync();

builder.Services.AddOptions<HomePageConfiguration>()
	.Bind(builder.Configuration.GetSection("Features:HomePage"))
	.ValidateOnStart();

builder.Services.TryAddEnumerable(
	ServiceDescriptor.Singleton<IValidateOptions<HomePageConfiguration>,
		HomePageConfigurationValidation>());
builder.Services.TryAddEnumerable(
	ServiceDescriptor.Singleton<IValidateOptions<ExternalServicesConfiguration>,
		ExternalServicesConfigurationValidation>());

builder.Services.Configure<GreetingConfiguration>(builder.Configuration.
	GetSection("Features:Greeting"));

builder.Services.AddOptions<ExternalServicesConfiguration>(
	ExternalServicesConfiguration.WeatherApi)
	.Bind(builder.Configuration.GetSection("ExternalServices:WeatherApi"))
	.ValidateOnStart();
builder.Services.AddOptions<ExternalServicesConfiguration>(
	ExternalServicesConfiguration.ProductsApi)
	.Bind(builder.Configuration.GetSection("ExternalServices:ProductsApi"))
.ValidateOnStart();

builder.Services.Configure<ScoreProcesingConfiguration>(
	builder.Configuration.GetSection("ScoreProcessing"));

builder.Services.AddAWSService<IAmazonS3>();

var useLocalStack = builder.Configuration.GetValue<bool>("AWS:UseLocalStack");

if (builder.Environment.IsDevelopment() && useLocalStack)
{
	builder.Services.AddSingleton<IAmazonS3>(sp =>
	{
		var s3Client = new AmazonS3Client(new AmazonS3Config
		{
			ServiceURL = "http://localhost:4566",
			ForcePathStyle = true,
			AuthenticationRegion = builder.Configuration.GetValue<string>("AWS:Region") ?? "eu-west-2"
		});

		return s3Client;
	});
}

builder.Services
	.AddAppConfiguration(builder.Configuration)
	.AddBookingServices()
	.AddBookingRules()
	.AddCourtUnavailability()
	.AddMembershipServices()
	.AddStaffServices()
	.AddCourtServices()
	.AddWeatherForecasting(builder.Configuration)
	.AddProducts()
	.AddNotifications()
	.AddGreetings()
	.AddCaching()
	.AddTimeServices()
	.AddProfanityValidationService()
	.AddAuditing()
	.AddTennisResultProcessing(builder.Configuration);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(options =>
{
	options.Conventions.AuthorizePage("/Bookings");
	options.Conventions.AuthorizePage("/BookCourt");
	options.Conventions.AuthorizePage("/FindAvailableCourts");
	options.Conventions.Add(new PageRouteTransformerConvention(new SlugifyParameterTransformer()));
});

// Add services to the container.
builder.Services.AddDbContext<TennisBookingsDbContext>(options =>
	options.UseSqlite(connection));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<TennisBookingsUser, TennisBookingsRole>(options => options.SignIn.RequireConfirmedAccount = false)
	.AddEntityFrameworkStores<TennisBookingsDbContext>()
	.AddDefaultUI()
	.AddDefaultTokenProviders();

builder.Services.AddHostedService<InitialiseDatabaseService>();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.AccessDeniedPath = "/identity/account/access-denied";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
