using UnityEngine;
using System;
using System.Collections;

public class WeatherTimeSystem : MonoBehaviour
{
    // 时间枚举
    public enum TimeOfDay { Day, Dusk, Night }
    // 天气枚举
    public enum WeatherType { Clear, Cloudy, Rain, Snow }

    [Header("=== 直接光照配置（每个组合独立调整）===")]
    // 白天（Day）光照配置
    [Tooltip("白天晴天 - 光照强度")] public float dayClearLightIntensity = 0.5f;
    [Tooltip("白天晴天 - 光照颜色")] public Color dayClearLightColor = new Color(1f, 0.95f, 0.9f);
    [Tooltip("白天阴天 - 光照强度")] public float dayCloudyLightIntensity = 0.4f;
    [Tooltip("白天阴天 - 光照颜色")] public Color dayCloudyLightColor = new Color(0.9f, 0.9f, 0.9f);
    [Tooltip("白天下雨 - 光照强度")] public float dayRainLightIntensity = 0.3f;
    [Tooltip("白天下雨 - 光照颜色")] public Color dayRainLightColor = new Color(0.8f, 0.85f, 0.9f);
    [Tooltip("白天下雪 - 光照强度")] public float daySnowLightIntensity = 0.35f;
    [Tooltip("白天下雪 - 光照颜色")] public Color daySnowLightColor = new Color(0.95f, 0.95f, 1f);

    // 黄昏（Dusk）光照配置
    [Tooltip("黄昏晴天 - 光照强度")] public float duskClearLightIntensity = 0.3f;
    [Tooltip("黄昏晴天 - 光照颜色")] public Color duskClearLightColor = new Color(0.9f, 0.6f, 0.4f);
    [Tooltip("黄昏阴天 - 光照强度")] public float duskCloudyLightIntensity = 0.25f;
    [Tooltip("黄昏阴天 - 光照颜色")] public Color duskCloudyLightColor = new Color(0.8f, 0.55f, 0.4f);
    [Tooltip("黄昏下雨 - 光照强度")] public float duskRainLightIntensity = 0.2f;
    [Tooltip("黄昏下雨 - 光照颜色")] public Color duskRainLightColor = new Color(0.7f, 0.5f, 0.4f);
    [Tooltip("黄昏下雪 - 光照强度")] public float duskSnowLightIntensity = 0.22f;
    [Tooltip("黄昏下雪 - 光照颜色")] public Color duskSnowLightColor = new Color(0.85f, 0.6f, 0.45f);

    // 夜晚（Night）光照配置
    [Tooltip("夜晚晴天 - 光照强度")] public float nightClearLightIntensity = 0.1f;
    [Tooltip("夜晚晴天 - 光照颜色")] public Color nightClearLightColor = new Color(0.3f, 0.4f, 0.8f);
    [Tooltip("夜晚阴天 - 光照强度")] public float nightCloudyLightIntensity = 0.08f;
    [Tooltip("夜晚阴天 - 光照颜色")] public Color nightCloudyLightColor = new Color(0.25f, 0.35f, 0.7f);
    [Tooltip("夜晚下雨 - 光照强度")] public float nightRainLightIntensity = 0.05f;
    [Tooltip("夜晚下雨 - 光照颜色")] public Color nightRainLightColor = new Color(0.2f, 0.3f, 0.6f);
    [Tooltip("夜晚下雪 - 光照强度")] public float nightSnowLightIntensity = 0.09f;
    [Tooltip("夜晚下雪 - 光照颜色")] public Color nightSnowLightColor = new Color(0.4f, 0.5f, 0.9f);

    [Header("Skybox Settings - Time + Weather Combinations")]
    public Material dayClearSkybox;
    public Material dayCloudySkybox;
    public Material dayRainSkybox;
    public Material daySnowSkybox;
    public Material duskClearSkybox;
    public Material duskCloudySkybox;
    public Material duskRainSkybox;
    public Material duskSnowSkybox;
    public Material nightClearSkybox;
    public Material nightCloudySkybox;
    public Material nightRainSkybox;
    public Material nightSnowSkybox;

    [Header("Scene Components")]
    public Light mainLight;
    public ParticleSystem rainParticle;
    public ParticleSystem snowParticle;

    [Header("灯笼显示控制")]
    [Tooltip("拖拽包含所有灯笼的总父物体到这里")]
    public GameObject lanternRoot; // 核心接口：总灯笼父物体
    [Tooltip("黄昏是否显示灯笼（默认显示）")]
    public bool showLanternAtDusk = true; // 可选配置：黄昏显示开关

    [Header("Real-time Core Settings（保留时间+天气获取）")]
    public RealTimeWeatherFetcher realTimeFetcher; // 实时天气获取组件
    public bool autoEnableRealTimeMode = true; // 默认启用实时模式
    [Range(300, 3600)] public float realTimeUpdateInterval = 600f; // 实时数据更新间隔（秒）

    [Header("自动模式配置")]
    public float autoSwitchInterval = 30f; // 自动切换间隔（秒）
    private Coroutine autoSwitchCoroutine;
    private bool isAutoMode = false;
    private bool isRealTimeMode = false; // 实时模式标记

    // 自动模式循环组合
    private (TimeOfDay time, WeatherType weather)[] autoSwitchCombinations = new (TimeOfDay, WeatherType)[]
    {
        (TimeOfDay.Day, WeatherType.Clear),
        (TimeOfDay.Dusk, WeatherType.Cloudy),
        (TimeOfDay.Night, WeatherType.Rain),
        (TimeOfDay.Day, WeatherType.Snow),
        (TimeOfDay.Dusk, WeatherType.Rain),
        (TimeOfDay.Night, WeatherType.Snow)
    };

    // 当前状态
    private TimeOfDay currentTime = TimeOfDay.Day;
    private WeatherType currentWeather = WeatherType.Clear;

    void Start()
    {
        ApplyCurrentSettings();
        HideMobileUnusedUI();
        UpdateLanternVisibility(); // 初始化灯笼显示状态

        // 初始化实时模式
        if (realTimeFetcher != null)
        {
            realTimeFetcher.OnRealDataUpdated += OnRealDataUpdated;
            realTimeFetcher.updateInterval = realTimeUpdateInterval;
            if (autoEnableRealTimeMode)
            {
                EnableRealTimeMode(true);
                realTimeFetcher.ManualRefresh(); // 立即获取一次数据
            }
        }
        else
        {
            Debug.LogWarning("未赋值 RealTimeWeatherFetcher，无法获取真实时间和天气！");
        }
    }

    // 隐藏冗余UI（兼容移动端）
    private void HideMobileUnusedUI()
    {
        bool isMobile = Application.isMobilePlatform;
    }

    // 手机端时间设置入口
    public void SetTimeFromMobile(string timeStr)
    {
        if (Enum.TryParse<TimeOfDay>(timeStr, true, out TimeOfDay time))
        {
            if (isRealTimeMode) EnableRealTimeMode(false);
            if (isAutoMode) EnableAutoMode(false);
            SetTime(time);
        }
        else
        {
            Debug.LogError($"手机端无效时间类型：{timeStr}，默认设为白天");
            SetTime(TimeOfDay.Day);
        }
    }

    // 手机端天气设置入口
    public void SetWeatherFromMobile(string weatherStr)
    {
        if (Enum.TryParse<WeatherType>(weatherStr, true, out WeatherType weather))
        {
            if (isRealTimeMode) EnableRealTimeMode(false);
            if (isAutoMode) EnableAutoMode(false);
            SetWeather(weather);
        }
        else
        {
            Debug.LogError($"手机端无效天气类型：{weatherStr}，默认设为晴天");
            SetWeather(WeatherType.Clear);
        }
    }

    // 手机端同时设置时间和天气
    public void SetTimeWeatherFromMobile(string timeStr, string weatherStr)
    {
        SetTimeFromMobile(timeStr);
        SetWeatherFromMobile(weatherStr);
    }

    // 启用/禁用实时模式（供代码/UnityServer调用）
    public void EnableRealTimeMode(bool enable)
    {
        isRealTimeMode = enable;

        if (enable)
        {
            if (isAutoMode) EnableAutoMode(false);
            Debug.Log("<color=cyan>实时模式已启用，将获取真实时间和天气</color>");
            if (realTimeFetcher != null && !realTimeFetcher.autoUpdate)
            {
                realTimeFetcher.autoUpdate = true;
            }
        }
        else
        {
            Debug.Log("<color=cyan>实时模式已禁用</color>");
            if (realTimeFetcher != null)
            {
                realTimeFetcher.autoUpdate = false;
            }
        }
    }

    // 启用/禁用自动模式
    public void EnableAutoMode(bool enable)
    {
        isAutoMode = enable;

        if (enable)
        {
            if (isRealTimeMode) EnableRealTimeMode(false);
            if (autoSwitchCoroutine == null)
            {
                autoSwitchCoroutine = StartCoroutine(AutoSwitchTimeWeatherCoroutine());
                Debug.Log($"<color=green>自动模式已启用，切换间隔：{autoSwitchInterval}秒</color>");
            }
        }
        else
        {
            if (autoSwitchCoroutine != null)
            {
                StopCoroutine(autoSwitchCoroutine);
                autoSwitchCoroutine = null;
                Debug.Log("<color=yellow>自动模式已禁用</color>");
            }
        }
    }

    // 自动切换时间天气协程
    private IEnumerator AutoSwitchTimeWeatherCoroutine()
    {
        int currentIndex = 0;
        while (isAutoMode)
        {
            var (targetTime, targetWeather) = autoSwitchCombinations[currentIndex];
            SetTime(targetTime);
            SetWeather(targetWeather);
            Debug.Log($"自动切换：时间={targetTime}，天气={targetWeather}");

            yield return new WaitForSeconds(autoSwitchInterval);
            currentIndex = (currentIndex + 1) % autoSwitchCombinations.Length;
        }
    }

    // 设置时间（内部调用）
    private void SetTime(TimeOfDay time)
    {
        if (currentTime == time) return;
        currentTime = time;
        ApplyCurrentSettings();
        UpdateLanternVisibility(); // 时间变化时更新灯笼显示
    }

    // 设置天气（内部调用）
    private void SetWeather(WeatherType weather)
    {
        if (currentWeather == weather) return;
        currentWeather = weather;
        ApplyCurrentSettings();
    }

    // 应用所有配置（光照、天空盒、粒子）
    private void ApplyCurrentSettings()
    {
        // 应用天空盒
        Material skybox = GetSkybox(currentTime, currentWeather);
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
            DynamicGI.UpdateEnvironment();
        }

        // 应用光照
        (float intensity, Color color) = GetCurrentLightParams();
        if (mainLight != null)
        {
            mainLight.intensity = intensity;
            mainLight.color = color;
        }
        else
        {
            Debug.LogWarning("未赋值 MainLight，无法应用光照配置！");
        }

        // 应用粒子效果
        ControlParticles(currentWeather);
    }

    // 更新灯笼显示/隐藏状态
    private void UpdateLanternVisibility()
    {
        // 空引用保护
        if (lanternRoot == null)
        {
            Debug.LogWarning("未赋值 Lantern Root（灯笼总父物体），无法控制灯笼显示！");
            return;
        }

        // 显示规则：白天隐藏，黄昏根据配置，黑夜显示
        bool shouldShow = currentTime switch
        {
            TimeOfDay.Day => false,
            TimeOfDay.Dusk => showLanternAtDusk,
            TimeOfDay.Night => true,
            _ => false
        };

        // 更新激活状态
        lanternRoot.SetActive(shouldShow);
        Debug.Log($"<color=orange>灯笼显示状态更新：时间={currentTime}，显示={shouldShow}</color>");
    }

    // 获取当前时间+天气对应的光照参数
    private (float intensity, Color color) GetCurrentLightParams()
    {
        switch (currentTime)
        {
            case TimeOfDay.Day:
                return currentWeather switch
                {
                    WeatherType.Clear => (dayClearLightIntensity, dayClearLightColor),
                    WeatherType.Cloudy => (dayCloudyLightIntensity, dayCloudyLightColor),
                    WeatherType.Rain => (dayRainLightIntensity, dayRainLightColor),
                    WeatherType.Snow => (daySnowLightIntensity, daySnowLightColor),
                    _ => (dayClearLightIntensity, dayClearLightColor)
                };
            case TimeOfDay.Dusk:
                return currentWeather switch
                {
                    WeatherType.Clear => (duskClearLightIntensity, duskClearLightColor),
                    WeatherType.Cloudy => (duskCloudyLightIntensity, duskCloudyLightColor),
                    WeatherType.Rain => (duskRainLightIntensity, duskRainLightColor),
                    WeatherType.Snow => (duskSnowLightIntensity, duskSnowLightColor),
                    _ => (duskClearLightIntensity, duskClearLightColor)
                };
            case TimeOfDay.Night:
                return currentWeather switch
                {
                    WeatherType.Clear => (nightClearLightIntensity, nightClearLightColor),
                    WeatherType.Cloudy => (nightCloudyLightIntensity, nightCloudyLightColor),
                    WeatherType.Rain => (nightRainLightIntensity, nightRainLightColor),
                    WeatherType.Snow => (nightSnowLightIntensity, nightSnowLightColor),
                    _ => (nightClearLightIntensity, nightClearLightColor)
                };
            default:
                return (dayClearLightIntensity, dayClearLightColor);
        }
    }

    // 获取当前时间+天气对应的天空盒
    private Material GetSkybox(TimeOfDay time, WeatherType weather)
    {
        switch (time)
        {
            case TimeOfDay.Day:
                switch (weather)
                {
                    case WeatherType.Clear: return dayClearSkybox;
                    case WeatherType.Cloudy: return dayCloudySkybox;
                    case WeatherType.Rain: return dayRainSkybox;
                    case WeatherType.Snow: return daySnowSkybox;
                }
                break;
            case TimeOfDay.Dusk:
                switch (weather)
                {
                    case WeatherType.Clear: return duskClearSkybox;
                    case WeatherType.Cloudy: return duskCloudySkybox;
                    case WeatherType.Rain: return duskRainSkybox;
                    case WeatherType.Snow: return duskSnowSkybox;
                }
                break;
            case TimeOfDay.Night:
                switch (weather)
                {
                    case WeatherType.Clear: return nightClearSkybox;
                    case WeatherType.Cloudy: return nightCloudySkybox;
                    case WeatherType.Rain: return nightRainSkybox;
                    case WeatherType.Snow: return nightSnowSkybox;
                }
                break;
        }
        return dayClearSkybox;
    }

    // 控制雨雪粒子效果
    private void ControlParticles(WeatherType weather)
    {
        // 停止所有粒子
        if (rainParticle != null)
        {
            if (!rainParticle.gameObject.activeInHierarchy)
                rainParticle.gameObject.SetActive(true);
            rainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (snowParticle != null)
        {
            if (!snowParticle.gameObject.activeInHierarchy)
                snowParticle.gameObject.SetActive(true);
            snowParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // 根据天气播放对应粒子
        switch (weather)
        {
            case WeatherType.Rain:
                rainParticle?.Play();
                Debug.Log($"<color=blue>雨粒子开始播放 - 状态: {rainParticle?.isPlaying ?? false}</color>");
                break;
            case WeatherType.Snow:
                snowParticle?.Play();
                Debug.Log($"<color=white>雪粒子开始播放 - 状态: {snowParticle?.isPlaying ?? false}</color>");
                break;
            default:
                // 非雨雪天气隐藏粒子
                if (rainParticle != null && rainParticle.gameObject.activeInHierarchy)
                    rainParticle.gameObject.SetActive(false);
                if (snowParticle != null && snowParticle.gameObject.activeInHierarchy)
                    snowParticle.gameObject.SetActive(false);
                break;
        }
    }

    // 接收实时天气数据更新回调
    private void OnRealDataUpdated(DateTime beijingTime, string weather, float temperature)
    {
        if (!isRealTimeMode) return;

        Debug.Log($"<color=cyan>实时数据更新：时间={beijingTime:HH:mm}，天气={weather}，温度={temperature}℃</color>");

        // 映射真实时间到游戏内时间段
        string timeOfDayStr = realTimeFetcher.GetTimeOfDay();
        TimeOfDay mappedTime = timeOfDayStr switch
        {
            "Day" => TimeOfDay.Day,
            "Dusk" => TimeOfDay.Dusk,
            "Night" => TimeOfDay.Night,
            _ => TimeOfDay.Day
        };

        // 映射真实天气到游戏内天气类型
        string mappedWeatherStr = realTimeFetcher.GetMappedWeatherType();
        WeatherType mappedWeather = mappedWeatherStr switch
        {
            "Rain" => WeatherType.Rain,
            "Snow" => WeatherType.Snow,
            "Cloudy" => WeatherType.Cloudy,
            _ => WeatherType.Clear
        };

        // 更新状态并应用
        bool timeChanged = currentTime != mappedTime;
        currentTime = mappedTime;
        currentWeather = mappedWeather;
        ApplyCurrentSettings();

        // 时间变化时更新灯笼显示
        if (timeChanged)
        {
            UpdateLanternVisibility();
        }
    }

    // 销毁时解绑事件
    void OnDestroy()
    {
        if (autoSwitchCoroutine != null)
        {
            StopCoroutine(autoSwitchCoroutine);
        }

        if (realTimeFetcher != null)
        {
            realTimeFetcher.OnRealDataUpdated -= OnRealDataUpdated;
        }
    }

    #region 调试方法（右键脚本组件调用）
    [ContextMenu("Set Day Clear（白天晴天）")]
    public void DebugDayClear()
    {
        if (isRealTimeMode) EnableRealTimeMode(false);
        if (isAutoMode) EnableAutoMode(false);
        currentTime = TimeOfDay.Day;
        currentWeather = WeatherType.Clear;
        ApplyCurrentSettings();
        UpdateLanternVisibility();
    }

    [ContextMenu("Set Night Snow（夜晚下雪）")]
    public void DebugNightSnow()
    {
        if (isRealTimeMode) EnableRealTimeMode(false);
        if (isAutoMode) EnableAutoMode(false);
        currentTime = TimeOfDay.Night;
        currentWeather = WeatherType.Snow;
        ApplyCurrentSettings();
        UpdateLanternVisibility();
    }

    [ContextMenu("Enable RealTime Mode（启用实时模式）")]
    public void DebugEnableRealTime()
    {
        EnableRealTimeMode(true);
    }

    [ContextMenu("Enable Auto Mode（启用自动模式）")]
    public void DebugEnableAutoMode()
    {
        EnableAutoMode(true);
    }

    [ContextMenu("强制显示灯笼")]
    public void DebugShowLantern()
    {
        if (lanternRoot != null)
        {
            lanternRoot.SetActive(true);
            Debug.Log("<color=orange>强制显示灯笼</color>");
        }
        else
        {
            Debug.LogWarning("未赋值 Lantern Root，无法强制显示！");
        }
    }

    [ContextMenu("强制隐藏灯笼")]
    public void DebugHideLantern()
    {
        if (lanternRoot != null)
        {
            lanternRoot.SetActive(false);
            Debug.Log("<color=orange>强制隐藏灯笼</color>");
        }
        else
        {
            Debug.LogWarning("未赋值 Lantern Root，无法强制隐藏！");
        }
    }
    #endregion
}