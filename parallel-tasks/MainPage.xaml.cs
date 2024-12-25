using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Windows.Input;
using Newtonsoft.Json;

namespace parallel_tasks
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            RefreshCommand = new Command(OnRefresh);
            BindingContext = this;
        }
        public ICommand RefreshCommand { get; }
        private async void OnRefresh(object o)
        {
            await Refresh();
            IsRefreshing = false;
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Refresh();
        }

        private async Task Refresh()
        {
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
            OnPropertyChanged(nameof(LocalTime));
        }

        public ObservableCollection<CurrentWeather> Weather { get; } = new();
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (!Equals(_isRefreshing, value))
                {
                    _isRefreshing = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _isRefreshing = default;
        public string LocalTime => $"Refreshed: {DateTime.Now} (local)";
    }
    public class WeatherData
    {
        public CurrentWeather? current_weather { get; set; }
    }
    public class CurrentWeather
    {
        [JsonProperty("time")]
        public string? Time { get; set; }
        public string? LocalTime => City switch
        {
            "New York" => localeTime("Eastern Standard Time"),
            "Los Angeles" => localeTime("Pacific Standard Time"),
            "Chicago" => localeTime("Central Standard Time"),
            "London" => localeTime("GMT Standard Time"),
            _ => null // unknown
        };
        private string? localeTime(string timeZoneId)
        {
            if (DateTime.TryParse(Time, out var utcTime))
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
                return localTime.ToString("MMMM dd, yyyy, h:mm tt");
            }
            return null; // Return null if parsing fails
        }
        [JsonProperty("temperature")]
        public double Temperature { get; set; }
        public string TemperatureF => $"{(Temperature * 9 / 5) + 32:F2} °F";
        public string? City { get; internal set; }
    }
}
