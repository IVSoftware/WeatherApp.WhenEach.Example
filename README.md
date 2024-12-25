A question was asked, how to fire off async tasks in parallel and display each result as it arrived.

The OP states a requirement to:

>_display the results of each of these calls as soon as I have a response, regardless of whether I already have a response from all of them or not._

One approach for doing this:

~~~
public ObservableCollection<CurrentWeather> Weather { get; } = new();

protected override async void OnAppearing()
{
    base.OnAppearing();
   
    Weather.Clear();
    HttpClient httpClient = new HttpClient();
    var tasks = new Task<CurrentWeather?>[]
    {
        // NOTE: api.open-meteo updates every 15 minutes
        getWeatherForCity("New York", "https://api.open-meteo.com/v1/forecast?latitude=40.7128&longitude=-74.0060&current_weather=true"),
        getWeatherForCity("Los Angeles", "https://api.open-meteo.com/v1/forecast?latitude=34.0522&longitude=-118.2437&current_weather=true"), 
        getWeatherForCity("Chicago", "https://api.open-meteo.com/v1/forecast?latitude=41.8781&longitude=-87.6298&current_weather=true"), 
        getWeatherForCity("London", "https://api.open-meteo.com/v1/forecast?latitude=51.5074&longitude=-0.1278&current_weather=true"),
    }; 
    await foreach (var randoTask in Task.WhenEach(tasks))
    {
        if (await randoTask is { } currentWeather)
        {
            Weather.Add(currentWeather);
        }
    }
    async Task<CurrentWeather?> getWeatherForCity(string city, string url)
    {
        var response = await httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (JsonConvert.DeserializeObject<WeatherData>(content) is { } data &&
                data.current_weather is { } currentWeather)
            {
                currentWeather.City = city;
                return currentWeather;
            }
        }
        return new CurrentWeather { City = city, Time = "Request Failed"};
    }
}
~~~