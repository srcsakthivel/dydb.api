using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace dydb.api.Controllers
{
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
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };



        public WeatherForecastController(IDynamoDBContext dynamoDBContext, ILogger<WeatherForecastController> logger, IConnectionMultiplexer multiplexe)
        {
            _dynamoDBContext = dynamoDBContext;
            _logger = logger;
            _connectionMultiplexer = multiplexe;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get(string zipCode = "12345")
        {
            List<WeatherForecast> items = await GetFromCache(zipCode);

            return items;
        }

        private async Task<List<WeatherForecast>> GetFromCache(string zipCode)
        {
            _logger.LogInformation("Get from Cache....Start....");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            IDatabase db = _connectionMultiplexer.GetDatabase();
            var data = db.StringGetAsync(zipCode).Result;
            var items = JsonConvert.DeserializeObject<List<WeatherForecast>>(value: data.ToString() ?? String.Empty);
            watch.Stop();
            _logger.LogInformation("Get from Cache....End....");
            _logger.LogInformation($"Time Take to call DB to get the data....{watch.ElapsedMilliseconds} ms....");
            return items ?? await GetFromDB(zipCode);
        }

        private async Task<List<WeatherForecast>> GetFromDB(string zipCode)
        {
            List<WeatherForecast> items = new List<WeatherForecast>();

            _logger.LogWarning("Call DB to get the data....Start....");
            var watch = System.Diagnostics.Stopwatch.StartNew();


            items = await _dynamoDBContext
            .QueryAsync<WeatherForecast>(zipCode)
            .GetRemainingAsync();

            watch.Stop();

            _logger.LogWarning("Call DB to get the data....End....");
            _logger.LogInformation($"Time Take to call DB to get the data....{watch.ElapsedMilliseconds} ms....");


            IDatabase db = _connectionMultiplexer.GetDatabase();
            if (items != null && items.Count > 0)
            {
                _logger.LogInformation("Call DB returned values....");
                _logger.LogInformation("Set in Cache....Start....");
                db.StringSet(zipCode, JsonConvert.SerializeObject(items), expiry: TimeSpan.FromMinutes(1));
                _logger.LogInformation("Set in Cache....End....");
            }

            return items ?? new List<WeatherForecast>();
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
            return Enumerable.Range(1, 500).Select(index => new WeatherForecast
            {
                ZipCode = zipcode,
                Date = DateTime.Now.AddDays(index).ToString(),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
                        .ToArray();
        }
    }
}