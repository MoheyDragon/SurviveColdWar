using UnityEngine;
using UnityEngine.Advertisements;
using System;
public class AdsManager : MonoBehaviour, IUnityAdsInitializationListener
{
    public static AdsManager Singleton;
    public Action OnAdsInitialized;
    [SerializeField] string _androidGameId;
    [SerializeField] string _iOSGameId;
    [SerializeField] bool _testMode = true;
    private string _gameId;

    void Awake()
    {
        InitializeSingleton();
        InitializeAds();
    }
    void InitializeSingleton()
    {
        if (Singleton == null)
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            DestroyImmediate(gameObject);
    }

    public void InitializeAds()
    {
        if (Advertisement.isInitialized) AdReady = true;
#if UNITY_IOS
            _gameId = _iOSGameId;
#elif UNITY_ANDROID
        _gameId = _androidGameId;
#elif UNITY_EDITOR
            _gameId = _androidGameId; //Only for testing the functionality in the Editor
#endif
        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(_gameId, _testMode, this);
        }
    }
    public bool AdReady;

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
        OnAdsInitialized?.Invoke();
        AdReady = true;
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
        InitializeAds();
    }
}