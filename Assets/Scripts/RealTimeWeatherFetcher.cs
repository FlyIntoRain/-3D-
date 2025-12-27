using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RealTimeWeatherFetcher : MonoBehaviour
{
    [Header("=== Open-Meteo API 配置 ===")]
    [Tooltip("城市名称，用于地理编码")]
    public string cityName = "济南";

    [Header("=== 更新配置 ===")]
    public bool autoUpdate = true;
    [Range(300, 3600)] public float updateInterval = 600f;

    [Header("=== 离线模式 ===")]
    public bool offlineMode = false;
    public string mockWeather = "晴";
    public float mockTemperature = 22f;

    [Header("调试信息")]
    [SerializeField] private float latitude = 36.66833f;    // 济南纬度
    [SerializeField] private float longitude = 116.99722f;  // 济南经度

    // 数据存储
    private DateTime beijingTime;
    private string currentWeather;
    private float currentTemperature;
    private bool isDataValid = false;
    private bool isInitialized = false;

    // 事件（通知外部系统更新）
    public event Action<DateTime, string, float> OnRealDataUpdated;

    // Open-Meteo天气代码到中文的映射
    private Dictionary<int, string> weatherCodeMap = new Dictionary<int, string>
    {
        {0, "晴"}, {1, "晴间多云"}, {2, "间断多云"}, {3, "阴"},
        {45, "雾"}, {48, "沉积雾"},
        {51, "小雨"}, {53, "中雨"}, {55, "大雨"},
        {56, "冻小雨"}, {57, "冻中雨"},
        {61, "小雨"}, {63, "中雨"}, {65, "大雨"},
        {66, "冻雨"}, {67, "冻大雨"},
        {71, "小雪"}, {73, "中雪"}, {75, "大雪"}, {77, "雪粒"},
        {80, "弱阵雨"}, {81, "中阵雨"}, {82, "强阵雨"},
        {85, "弱阵雪"}, {86, "强阵雪"},
        {95, "雷阵雨"}, {96, "弱雷雹"}, {99, "强雷雹"}
    };

    // 游戏内天气类型映射（保留原有逻辑）
    private Dictionary<string, string> weatherKeywords = new Dictionary<string, string>
    {
        {"晴", "Clear"}, {"晴间多云", "Clear"}, {"间断多云", "Cloudy"}, {"阴", "Cloudy"},
        {"雾", "Foggy"}, {"沉积雾", "Foggy"},
        {"小雨", "Rain"}, {"中雨", "Rain"}, {"大雨", "Rain"}, {"弱阵雨", "Rain"},
        {"中阵雨", "Rain"}, {"强阵雨", "Rain"}, {"雷阵雨", "Rain"},
        {"小雪", "Snow"}, {"中雪", "Snow"}, {"大雪", "Snow"}, {"弱阵雪", "Snow"},
        {"强阵雪", "Snow"}, {"雪粒", "Snow"},
        {"冻小雨", "Rain"}, {"冻中雨", "Rain"}, {"冻雨", "Rain"}, {"冻大雨", "Rain"},
        {"弱雷雹", "Rain"}, {"强雷雹", "Rain"}
    };

    void Start()
    {
        // 初始化经纬度（可根据城市名自动获取，这里使用固定值）
        if (string.IsNullOrEmpty(cityName))
        {
            Debug.LogWarning("⚠️ 城市名未设置，使用默认值济南");
            cityName = "济南";
        }

        // 开始获取数据
        StartCoroutine(InitializeAndFetchData());

        if (autoUpdate)
        {
            StartCoroutine(AutoUpdateCoroutine());
        }
    }

    // 初始化并获取数据
    private IEnumerator InitializeAndFetchData()
    {
        // 如果需要自动获取坐标，可以取消注释以下代码
        // yield return StartCoroutine(GetCityCoordinates(cityName));

        // 直接使用预设的经纬度开始获取天气
        yield return StartCoroutine(GetRealTimeWeatherData());
        isInitialized = true;
    }

    // 自动更新协程
    private IEnumerator AutoUpdateCoroutine()
    {
        // 等待初始化完成
        yield return new WaitUntil(() => isInitialized);

        while (autoUpdate)
        {
            yield return new WaitForSeconds(updateInterval);
            StartCoroutine(GetRealTimeWeatherData());
        }
    }

    // 手动刷新（供外部调用）
    public void ManualRefresh()
    {
        if (!isInitialized)
        {
            StartCoroutine(InitializeAndFetchData());
        }
        else
        {
            StartCoroutine(GetRealTimeWeatherData());
        }
    }

    // 获取城市坐标（可选功能）
    private IEnumerator GetCityCoordinates(string cityName)
    {
        string encodedCityName = UnityWebRequest.EscapeURL(cityName);
        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={encodedCityName}&count=1&language=zh";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result == UnityWebRequest.Result.Success)
#else
        if (!request.isHttpError && !request.isNetworkError)
#endif
        {
            string json = request.downloadHandler.text;
            GeocodingResponse response = JsonUtility.FromJson<GeocodingResponse>(json);

            if (response != null && response.results != null && response.results.Length > 0)
            {
                latitude = response.results[0].latitude;
                longitude = response.results[0].longitude;
                Debug.Log($"📍 获取到 {cityName} 坐标: 纬度={latitude}, 经度={longitude}");
            }
            else
            {
                Debug.LogWarning($"⚠️ 无法获取 {cityName} 的坐标，使用默认值");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ 获取坐标失败: {request.error}，使用默认值");
        }

        request.Dispose();
    }

    // 主数据获取逻辑
    private IEnumerator GetRealTimeWeatherData()
    {
        if (offlineMode)
        {
            // 离线模式：直接返回模拟数据
            beijingTime = DateTime.Now;
            currentWeather = mockWeather;
            currentTemperature = mockTemperature;
            isDataValid = true;
            Debug.Log($"📶 离线模式启用：天气={mockWeather}，温度={mockTemperature}℃");
            OnRealDataUpdated?.Invoke(beijingTime, currentWeather, currentTemperature);
            yield break;
        }

        // 在线模式：调用Open-Meteo API
        string requestUrl = BuildOpenMeteoRequestUrl();
        Debug.Log($"🌐 请求URL: {requestUrl}");

        UnityWebRequest request = UnityWebRequest.Get(requestUrl);
        request.timeout = 10;

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
        bool isSuccess = !request.isHttpError && !request.isNetworkError;
#endif

        if (isSuccess)
        {
            string responseText = request.downloadHandler.text;
            Debug.Log($"📥 API返回数据: {responseText}");

            // 解析Open-Meteo响应
            OpenMeteoResponse response = JsonUtility.FromJson<OpenMeteoResponse>(responseText);

            if (response != null && response.current_weather != null)
            {
                // 提取数据
                currentTemperature = response.current_weather.temperature;
                int weatherCode = response.current_weather.weathercode;

                // 将天气代码转换为中文描述
                if (weatherCodeMap.TryGetValue(weatherCode, out string weatherDesc))
                {
                    currentWeather = weatherDesc;
                }
                else
                {
                    currentWeather = "未知";
                    Debug.LogWarning($"⚠️ 未知天气代码: {weatherCode}");
                }

                // 解析时间（Open-Meteo返回UTC时间，需要转换为北京时间）
                if (DateTime.TryParse(response.current_weather.time, out DateTime parseTime))
                {
                    beijingTime = parseTime;
                }
                else
                {
                    beijingTime = DateTime.Now;
                }

                isDataValid = true;

                Debug.Log($"✅ 数据更新成功！\n" +
                         $"   北京时间: {beijingTime:yyyy-MM-dd HH:mm:ss}\n" +
                         $"   天气: {currentWeather} (代码: {weatherCode})\n" +
                         $"   温度: {currentTemperature}℃");

                // 触发更新事件
                OnRealDataUpdated?.Invoke(beijingTime, currentWeather, currentTemperature);
            }
            else
            {
                Debug.LogError("❌ 解析响应数据失败！");
                UseFallbackData();
            }
        }
        else
        {
            Debug.LogError($"❌ 请求失败: {request.error}");
            UseFallbackData();
        }

        request.Dispose();
    }

    // 构建Open-Meteo请求URL
    private string BuildOpenMeteoRequestUrl()
    {
        // Open-Meteo API 请求格式
        // current_weather=true 获取当前天气
        // hourly参数可以获取更多数据，这里只请求基本数据
        string url = $"https://api.open-meteo.com/v1/forecast?" +
                    $"latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&" +
                    $"longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&" +
                    $"current_weather=true&" +
                    $"timezone=Asia%2FShanghai&" +
                    $"forecast_days=1";

        return url;
    }

    // 备用数据（API失败时使用）
    private void UseFallbackData()
    {
        Debug.LogWarning("⚠️ 使用备用数据");
        beijingTime = DateTime.Now;
        currentWeather = "晴";
        currentTemperature = 20f;
        isDataValid = true;
        OnRealDataUpdated?.Invoke(beijingTime, currentWeather, currentTemperature);
    }

    // 对外接口：获取时间段（白天/黄昏/夜晚）
    public string GetTimeOfDay()
    {
        if (!isDataValid) return "Day"; // 默认值

        int hour = beijingTime.Hour;
        if (hour >= 6 && hour < 17) return "Day";
        if (hour >= 17 && hour < 18) return "Dusk";
        return "Night";
    }

    // 对外接口：映射游戏内天气类型
    public string GetMappedWeatherType()
    {
        if (!isDataValid) return "Clear"; // 默认值

        return weatherKeywords.TryGetValue(currentWeather, out string type) ? type : "Clear";
    }

    // 对外接口：获取原始天气描述
    public string GetWeatherDescription()
    {
        return isDataValid ? currentWeather : "未知";
    }

    // 对外接口：获取温度
    public float GetTemperature()
    {
        return isDataValid ? currentTemperature : 20f;
    }

    // 对外接口：获取时间
    public DateTime GetBeijingTime()
    {
        return isDataValid ? beijingTime : DateTime.Now;
    }

    // 对外接口：数据有效性判断
    public bool IsDataValid() => isDataValid;

    // 对外接口：设置城市（通过城市名）
    public void SetCity(string newCityName)
    {
        if (!string.IsNullOrEmpty(newCityName))
        {
            cityName = newCityName;
            StartCoroutine(GetCityCoordinates(cityName));
            Debug.Log($"🌍 切换城市: {newCityName}");
        }
        else
        {
            Debug.LogError("❌ 城市名不能为空！");
        }
    }

    // 对外接口：直接设置坐标
    public void SetCoordinates(float newLatitude, float newLongitude)
    {
        latitude = newLatitude;
        longitude = newLongitude;
        Debug.Log($"📍 设置坐标: 纬度={latitude}, 经度={longitude}");

        if (isInitialized)
        {
            StartCoroutine(GetRealTimeWeatherData());
        }
    }

    // ========== Open-Meteo API 响应数据结构 ==========

    [Serializable]
    private class OpenMeteoResponse
    {
        public float latitude;
        public float longitude;
        public float generationtime_ms;
        public int utc_offset_seconds;
        public string timezone;
        public string timezone_abbreviation;
        public float elevation;
        public CurrentWeather current_weather;
        public HourlyUnits hourly_units;
        public HourlyData hourly;
    }

    [Serializable]
    private class CurrentWeather
    {
        public float temperature;
        public float windspeed;
        public float winddirection;
        public int weathercode;
        public string time;
    }

    [Serializable]
    private class HourlyUnits
    {
        public string time;
        public string temperature_2m;
    }

    [Serializable]
    private class HourlyData
    {
        public string[] time;
        public float[] temperature_2m;
    }

    // ========== 地理编码API响应数据结构 ==========

    [Serializable]
    private class GeocodingResponse
    {
        public GeocodingResult[] results;
    }

    [Serializable]
    private class GeocodingResult
    {
        public int id;
        public string name;
        public float latitude;
        public float longitude;
        public float elevation;
        public string feature_code;
        public string country_code;
        public int population;
        public string timezone;
        public string country;
        public string admin1;
    }
}