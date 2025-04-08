using System.Threading.Tasks;
using UnityEngine;

public class WebViewScreen : BaseScreen<WebViewScreen>
{
    public string Url { get; set; } = "https://www.google.com/";
    public override void OnBackClick()
    {
        SelfDestroy();
    }

    protected async override void Start()
    {
        base.Start();

        WebViewObject webViewObj = gameObject.AddComponent<WebViewObject>();
        webViewObj.Init(
            cb: (msg) =>
            {
                Debug.Log(string.Format("CallFromJS[{0}]", msg));
            },
            err: (msg) =>
            {
                Debug.Log(string.Format("CallOnError[{0}]", msg));
            },
            httpErr: (msg) =>
            {
                Debug.Log(string.Format("CallOnHttpError[{0}]", msg));
            },
            started: (msg) =>
            {
                Debug.Log(string.Format("CallOnStarted[{0}]", msg));
            },
            hooked: (msg) =>
            {
                Debug.Log(string.Format("CallOnHooked[{0}]", msg));
            },
            cookies: (msg) =>
            {
                Debug.Log(string.Format("CallOnCookies[{0}]", msg));
            },
            ld: (msg) =>
            {
                Debug.Log(string.Format("CallOnLoaded[{0}]", msg));
            }
            //transparent: false,
            //zoom: true,
            //ua: "custom user agent string",
            //radius: 0,  // rounded corner radius in pixel
            //// android
            //androidForceDarkMode: 0,  // 0: follow system setting, 1: force dark off, 2: force dark on
            //// ios
            //enableWKWebView: true,
            //wkContentMode: 0,  // 0: recommended, 1: mobile, 2: desktop
            //wkAllowsLinkPreview: true,
            //// editor
            //separated: false
            );

        // cf. https://github.com/gree/unity-webview/issues/1094#issuecomment-2358718029
        while (!webViewObj.IsInitialized())
        {
            await Task.Yield();
        }
        // cf. https://github.com/gree/unity-webview/pull/512
        // Added alertDialogEnabled flag to enable/disable alert/confirm/prompt dialogs. by KojiNakamaru · Pull Request #512 · gree/unity-webview
        //webViewObj.SetAlertDialogEnabled(false);

        // cf. https://github.com/gree/unity-webview/pull/728
        //webViewObj.SetCameraAccess(true);
        //webViewObj.SetMicrophoneAccess(true);

        // cf. https://github.com/gree/unity-webview/pull/550
        // introduced SetURLPattern(..., hookPattern). by KojiNakamaru · Pull Request #550 · gree/unity-webview
        //webViewObj.SetURLPattern("", "^https://.*youtube.com", "^https://.*google.com");

        // cf. https://github.com/gree/unity-webview/pull/570
        // Add BASIC authentication feature (Android and iOS with WKWebView only) by takeh1k0 · Pull Request #570 · gree/unity-webview
        //webViewObj.SetBasicAuthInfo("id", "password");

        //webViewObj.SetScrollbarsVisibility(true);

        webViewObj.SetMargins(0, 0, 0, 0);
        //webViewObj.SetTextZoom(100);  // android only. cf. https://stackoverflow.com/questions/21647641/android-webview-set-font-size-system-default/47017410#47017410
        //webViewObj.SetMixedContentMode(2);  // android only. 0: MIXED_CONTENT_ALWAYS_ALLOW, 1: MIXED_CONTENT_NEVER_ALLOW, 2: MIXED_CONTENT_COMPATIBILITY_MODE
        webViewObj.SetVisibility(true);

#if !UNITY_WEBPLAYER && !UNITY_WEBGL
        if (Url.StartsWith("http") || Url.StartsWith("https"))
        {
            webViewObj.LoadURL(Url.Replace(" ", "%20"));
        }
#else
        if (Url.StartsWith("http") || Url.StartsWith("https")) 
        {
            webViewObject.LoadURL(Url.Replace(" ", "%20"));
        }
#endif
    }
}
