using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class WeatherService
{
    private readonly HttpClient client;
    private readonly string language;
    private readonly string lang;

    public WeatherService()
    {
        client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30) // 增加超时时间
        };
        var cultureInfo = CultureInfo.CurrentCulture;
        language = cultureInfo.Name;
        lang = cultureInfo.TwoLetterISOLanguageName;
    }

    public async Task<(int status, string statusDesc, string city)> GetCityByIpAddressAsync()
    {
        try
        {
            var response = await client.GetStringAsync($"http://ip-api.com/json/?lang={language}");
            var json = JsonDocument.Parse(response);
            var city = json.RootElement.GetProperty("city").GetString();
            return (1, "正确", city);
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"TaskCanceledException: {ex.Message}");
            return (2, "请求超时", null);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HttpRequestException: {ex.Message}");
            return (2, "没有获得城市信息", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            return (2, "没有获得城市信息", null);
        }
    }

    public async Task<WeatherResult> GetWeatherAsync()
    {
        var cityResult = await GetCityByIpAddressAsync();

        if (cityResult.status != 1)
        {
            return new WeatherResult
            {
                Status = cityResult.status,
                StatusDesc = cityResult.statusDesc
            };
        }

        var city = cityResult.city;
        var weatherResult = await GetWeatherInfoAsync(city);

        if (weatherResult.status != 1)
        {
            return new WeatherResult
            {
                Status = weatherResult.status,
                StatusDesc = weatherResult.statusDesc,
                City = city
            };
        }

        return new WeatherResult
        {
            Status = 1,
            StatusDesc = "正确",
            City = city,
            Temperature = weatherResult.weatherData.temperature,
            FeelsLikeTemperature = weatherResult.weatherData.feelsLikeTemperature,
            WeatherDescription = weatherResult.weatherData.weatherDescription,
            WeatherIcon = weatherResult.weatherData.weatherIcon,
            WindSpeed = weatherResult.weatherData.windSpeed,
            WindDirection = weatherResult.weatherData.windDirection,
            Visibility = weatherResult.weatherData.visibility,
            UVIndex = weatherResult.weatherData.uvIndex,
            LocalObsDateTime = weatherResult.weatherData.localObsDateTime
        };
    }

    private async Task<(int status, string statusDesc, (string temperature, string feelsLikeTemperature, string weatherDescription, string weatherIcon, string windSpeed, string windDirection, string visibility, string uvIndex, string localObsDateTime) weatherData)> GetWeatherInfoAsync(string city)
    {
        try
        {
            city = city.Replace(" ", "+");
            //var lang = language.ToLower();
            Console.WriteLine($"https://wttr.in/{city}?format=j1&lang={lang}");
            var response = await client.GetStringAsync($"https://wttr.in/{city}?format=j1&lang={lang}");

            var json = JsonDocument.Parse(response);
            var currentCondition = json.RootElement.GetProperty("current_condition")[0];
            var weatherCode = currentCondition.GetProperty("weatherCode").GetString();
            var tempC = currentCondition.GetProperty("temp_C").GetString();
            var feelsLikeC = currentCondition.GetProperty("FeelsLikeC").GetString();

            string weatherDesc = null;
            if (currentCondition.TryGetProperty($"lang_{lang}", out JsonElement langElement) && langElement.GetArrayLength() > 0)
            {
                weatherDesc = langElement[0].GetProperty("value").GetString();
            }
            else
            {
                weatherDesc = currentCondition.GetProperty("weatherDesc")[0].GetProperty("value").GetString();
            }
            var weatherIcon = WEATHER_SYMBOL.ContainsKey(WWO_CODE[weatherCode]) ? WEATHER_SYMBOL[WWO_CODE[weatherCode]] : WEATHER_SYMBOL["Unknown"];
            var windSpeed = currentCondition.GetProperty("windspeedKmph").GetString() + " km/h";
            var windDir = currentCondition.GetProperty("winddir16Point").GetString();
            var windDirection = WIND_DIRECTION_MAP.ContainsKey(windDir) ? WIND_DIRECTION_MAP[windDir] : windDir;
            var visibility = currentCondition.GetProperty("visibility").GetString() + " km";
            var uvIndex = currentCondition.GetProperty("uvIndex").GetString();
            var localObsDateTime = currentCondition.GetProperty("localObsDateTime").GetString();
            return (1, "正确", (tempC + "°C", feelsLikeC + "°C", weatherDesc, weatherIcon, windSpeed, windDirection, visibility, uvIndex, localObsDateTime));
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"TaskCanceledException: {ex.Message}");
            return (3, "请求超时", (null, null, null, null, null, null, null, null, null));
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HttpRequestException: {ex.Message}");
            return (3, "没有获得天气信息", (null, null, null, null, null, null, null, null, null));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            return (3, "没有获得天气信息", (null, null, null, null, null, null, null, null, null));
        }
    }
    public string GetTemperatureColor(string Temperature)
    {
        if (double.TryParse(Temperature.Replace("°C", ""), out double temp))
        {
            if (temp <= 0)
            {
                return "Blue"; // 寒冷
            }
            else if (temp > 0 && temp <= 15)
            {
                return "LightBlue"; // 凉爽
            }
            else if (temp > 15 && temp <= 25)
            {
                return "Green"; // 舒适
            }
            else if (temp > 25 && temp <= 35)
            {
                return "Orange"; // 温暖
            }
            else
            {
                return "Red"; // 炎热
            }
        }
        return "White"; // 默认颜色
    }

    static readonly Dictionary<string, string> WWO_CODE = new Dictionary<string, string>
    {
        {"113", "Sunny"},
        {"116", "PartlyCloudy"},
        {"119", "Cloudy"},
        {"122", "VeryCloudy"},
        {"143", "Fog"},
        {"176", "LightShowers"},
        {"179", "LightSleetShowers"},
        {"182", "LightSleet"},
        {"185", "LightSleet"},
        {"200", "ThunderyShowers"},
        {"227", "LightSnow"},
        {"230", "HeavySnow"},
        {"248", "Fog"},
        {"260", "Fog"},
        {"263", "LightShowers"},
        {"266", "LightRain"},
        {"281", "LightSleet"},
        {"284", "LightSleet"},
        {"293", "LightRain"},
        {"296", "LightRain"},
        {"299", "HeavyShowers"},
        {"302", "HeavyRain"},
        {"305", "HeavyShowers"},
        {"308", "HeavyRain"},
        {"311", "LightSleet"},
        {"314", "LightSleet"},
        {"317", "LightSleet"},
        {"320", "LightSnow"},
        {"323", "LightSnowShowers"},
        {"326", "LightSnowShowers"},
        {"329", "HeavySnow"},
        {"332", "HeavySnow"},
        {"335", "HeavySnowShowers"},
        {"338", "HeavySnow"},
        {"350", "LightSleet"},
        {"353", "LightShowers"},
        {"356", "HeavyShowers"},
        {"359", "HeavyRain"},
        {"362", "LightSleetShowers"},
        {"365", "LightSleetShowers"},
        {"368", "LightSnowShowers"},
        {"371", "HeavySnowShowers"},
        {"374", "LightSleetShowers"},
        {"377", "LightSleet"},
        {"386", "ThunderyShowers"},
        {"389", "ThunderyHeavyRain"},
        {"392", "ThunderySnowShowers"},
        {"395", "HeavySnowShowers"}
    };

    private static readonly Dictionary<string, string> WEATHER_SYMBOL = new Dictionary<string, string>
    {
        {"Unknown", "✨"},
        {"Cloudy", "☁️"},
        {"Fog", "🌫️"},
        {"HeavyRain", "🌧️"},
        {"HeavyShowers", "🌧️"},
        {"HeavySnow", "❄️"},
        {"HeavySnowShowers", "❄️"},
        {"LightRain", "🌦️"},
        {"LightShowers", "🌦️"},
        {"LightSleet", "🌧️"},
        {"LightSleetShowers", "🌧️"},
        {"LightSnow", "🌨️"},
        {"LightSnowShowers", "🌨️"},
        {"PartlyCloudy", "⛅️"},
        {"Sunny", "🌞"},
        {"ThunderyHeavyRain", "🌩️"},
        {"ThunderyShowers", "⛈️"},
        {"ThunderySnowShowers", "⛈️"},
        {"VeryCloudy", "☁️"}
    };
    static readonly Dictionary<string, string> WIND_DIRECTION_MAP = new Dictionary<string, string>
    {
        {"N", "↓"},
        {"NNE", "↙"},
        {"NE", "↙"},
        {"ENE", "↙"},
        {"E", "←"},
        {"ESE", "↖"},
        {"SE", "↖"},
        {"SSE", "↖"},
        {"S", "↑"},
        {"SSW", "↗"},
        {"SW", "↗"},
        {"WSW", "↗"},
        {"W", "→"},
        {"WNW", "↘"},
        {"NW", "↘"},
        {"NNW", "↘"}
    };
}

public class WeatherResult
{
    public int Status { get; set; }
    public string StatusDesc { get; set; }
    public string City { get; set; }
    public string Temperature { get; set; }
    public string WeatherDescription { get; set; }
    public string WeatherIcon { get; set; }
    public string WindSpeed { get; set; }
    public string WindDirection { get; set; }
    public string Visibility { get; set; }
    public string UVIndex { get; set; }
    public string FeelsLikeTemperature { get; set; }
    public string LocalObsDateTime { get; set; }
}
