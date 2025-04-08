using AppsFlyerSDK;
using Firebase.Extensions;
using Firebase.Messaging;
using Networking.ServerAPI.Api;
using Networking.ServerAPI.Responce;
using OnlineGame.Data;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Boot : MonoBehaviour, IAppsFlyerConversionData
{
    static Boot instance;
    Firebase.FirebaseApp app;
    string firebaseToken;

    public bool IsInit { get; private set; }

    void Awake()
    {
        if(instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this);
    }

    void Start()
    {
        var requestPermissionTime = -1L; // set mode to HasNoPermission
        if(PlayerPrefs.HasKey("RequestPermission"))
            requestPermissionTime = PlayerPrefs.GetString("RequestPermission").ToLong();
        if (requestPermissionTime != 0 && (System.DateTime.Now - System.DateTime.FromBinary(requestPermissionTime)).TotalDays >= 3)
        {
            var scr = FirebaseMessagesRequestScreen.Create();
            scr.boot = this;
        }
        else
        {
            Init();
        }
    }

    public async void Init()
    {
        await Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(FirebaseCheckAction());
        firebaseToken = await FirebaseMessaging.GetTokenAsync();
        Debug.Log("GetTokenAsync: " + firebaseToken);

        AppsFlyer.initSDK("mbuEj35ruc3QCoNtoq5pra", "", this);
        AppsFlyer.startSDK();
        IsInit = true;
    }

    private System.Action<Task<Firebase.DependencyStatus>> FirebaseCheckAction()
    {
        return task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                app = Firebase.FirebaseApp.DefaultInstance;
            }
            else
            {
                Debug.LogError(string.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            }
        };
    }

    public void onConversionDataSuccess(string conversionData)
    {
        AppsFlyer.AFLog("didReceiveConversionData", conversionData);

        var data = JsonUtility.FromJson<ConversionData>(conversionData);
        data.af_id = AppsFlyer.getAppsFlyerId();
        data.bundle_id = Application.identifier;
#if UNITY_ANDROID
        data.os = "Android";
#elif UNITY_IOS
        data.os = "iOS"; 
#endif
        data.store_id = "id1234567"; // ваш store id
        data.locale = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        data.push_token = firebaseToken;
        data.firebase_project_id = app.Options.ProjectId;

        ConfigApi.GetConfig(JsonUtility.ToJson(data), OnGetConfigComplete);
    }

    private void OnGetConfigComplete(ConfigResponce resp, ResponceError error)
    {
        if (!PlayerPrefs.HasKey("AppMode"))
        {
            if (error == null)
            {
                if (!string.IsNullOrEmpty(resp.url))
                {
                    PlayerPrefs.SetString("AppMode", "Web");
                    PlayerPrefs.SetString("Url", "resp.url");
                    WebViewScreen.Create().Url = resp.url;
                    return;
                }
            }
            PlayerPrefs.SetString("AppMode", "Local");
            SceneManager.LoadScene("LocalGame"); 
        }
        else
        {
            switch (PlayerPrefs.GetString("AppMode"))
            {
                case "Local":
                    SceneManager.LoadScene("LocalGame");
                    break;
                case "Web":
                    if (!string.IsNullOrEmpty(resp.url))
                        WebViewScreen.Create().Url = resp.url;
                    else
                    {
                        if (PlayerPrefs.HasKey("Url"))
                            WebViewScreen.Create().Url = PlayerPrefs.GetString("Url");
                        else
                        {
                            Debug.LogWarning("WebView mode can't be loaded because URL is not saved. Load local game");
                            SceneManager.LoadScene("LocalGame");
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }

    public void onConversionDataFail(string error)
    {
        AppsFlyer.AFLog("conversionDataFail", error);
    }

    void Update()
    {
        // check internet always
        if (!BaseWebApi.CheckInternet("142.251.39.110", 500))
        {
            if (BaseScreen.Instance is not NoInternetScreen)
                NoInternetScreen.Create();
        }
        else if (BaseScreen.Instance is NoInternetScreen scr)
            scr.SelfDestroy();
    }

    void OnDestroy()
    {
        instance = null;
    }
    
    public void onAppOpenAttribution(string attributionData)
    {
        AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
        Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
        // add direct deeplink logic here
    }

    public void onAppOpenAttributionFailure(string error)
    {
        AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
    }
}
