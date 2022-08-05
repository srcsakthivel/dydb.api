# aws-sample-dydb-api

## Updates

- Add the Nuget Package

```powershell
dotnet add package Newtonsoft.Json
dotnet add package StackExchange.Redis
```

- Update Program.cs
  
```C#
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["RedisConnectionString"]));
```

- Add methods for Get in WeatherForecastController.cs

```C#

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly IDynamoDBContext _dynamoDBContext;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(IDynamoDBContext dynamoDBContext, ILogger<WeatherForecastController> logger)
        {
            _dynamoDBContext = dynamoDBContext;
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get(string zipcode="12345")
        {
            _logger.LogWarning("Call DB to get the data....Start....");
            List<WeatherForecast> items = new List<WeatherForecast>();

            items = await _dynamoDBContext
            .QueryAsync<WeatherForecast>(zipcode)
            .GetRemainingAsync();

            _logger.LogWarning("Call DB to get the data....End....");

            return items;
        }

        [HttpPost(Name = "GetWeatherForecast")]
        public async Task<string> Post(string zipcode)
        {

            var store = AddWeatherForecast(zipcode);
            foreach (var item in store)
            {
                await _dynamoDBContext.SaveAsync(item);
            }

            return "Success";
        }

        private static IEnumerable<WeatherForecast> AddWeatherForecast(string zipcode)
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                ZipCode = zipcode,
                Date = DateTime.Now.AddDays(index).ToString(),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
                        .ToArray();
        }
    }

```
