using Cysharp.Threading.Tasks;
using Firebase.Messaging;
using System;
using System.Collections;
#if UNITY_ANDROID
using UnityEngine.Android; 
#endif
using UnityEngine;
using UnityEngine.UI;
#if UNITY_IOS
using Unity.Notifications.iOS; 
#endif

public class FirebaseMessagesRequestScreen : BaseScreen<FirebaseMessagesRequestScreen>
{
    public delegate void PermissionsAnswers(string permission, bool granted);
    [SerializeField] Button getPermissionBtn;
    public Boot boot { get; set; }

    protected override void Start()
    {
        base.Start();

        getPermissionBtn.onClick.AddListener(OnGetPermissionClick);
    }

    private async void OnGetPermissionClick()
    {
        getPermissionBtn.interactable = false;
        var result = await RequestNotificationPermission();
        if(!result)
        {
            PlayerPrefs.SetString("RequestPermission", System.DateTime.Now.ToBinary().ToString());
        }
        else
            PlayerPrefs.SetString("RequestPermission", "0");

        SelfDestroy();
        boot.Init();
    }

    public override void OnBackClick() 
    {
        PlayerPrefs.SetString("RequestPermission", System.DateTime.Now.ToBinary().ToString());
        SelfDestroy();
        boot.Init();
    }

    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }

    public void RequestPermissions(PermissionsAnswers onComplete)
    {
        StartCoroutine(OnRequestPermissions(onComplete));
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator OnRequestPermissions(PermissionsAnswers onComplete)
    {
        yield return RequestPermission("android.permission.POST_NOTIFICATIONS", onComplete);
    }
    IEnumerator RequestPermission(string permission, PermissionsAnswers onComplete)
    {
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted               += ClosePermissionGranted;
        callbacks.PermissionDenied                += ClosePermissionDenied;
        
        var permissionOccurred = false;
        var granted = false;

        void ClosePermissionGranted(string s)
        {
            permissionOccurred = true;
            granted = true;
        }
        void ClosePermissionDenied(string s)
        {
            permissionOccurred = true;
            granted = false;
        }
        Debug.Log($"Request {permission}");
        Permission.RequestUserPermission(permission, callbacks);
        yield return new WaitUntil(() => permissionOccurred);
        Debug.Log($"Permission '{(granted ? "Granted" : "Denied")}'");
        onComplete?.Invoke(permission, granted);
    }
#elif UNITY_IOS && !UNITY_EDITOR
    IEnumerator OnRequestPermissions(PermissionsAnswers onComplete)
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            }
            ;

            string res = "\n RequestAuthorization:";
            res += "\n finished: " + req.IsFinished;
            res += "\n granted :  " + req.Granted;
            res += "\n error:  " + req.Error;
            res += "\n deviceToken:  " + req.DeviceToken;
            Debug.Log(res);

            onComplete?.Invoke("POST_NOTIFICATIONS", req.Granted);
        }
    }
#else
    IEnumerator OnRequestPermissions(PermissionsAnswers onComplete)
    {
        yield return null;
        onComplete?.Invoke("POST_NOTIFICATIONS", false);
    }
#endif


    private async UniTask<bool> RequestNotificationPermission()
    {
        bool result = false;

        var permission_request_complete = false;
        var permission_request_granted = false;
        RequestPermissions((perm, granted) =>
        {
            permission_request_granted = granted;
            permission_request_complete = perm.Contains("POST_NOTIFICATIONS");
        });

        while (!permission_request_complete)
            await UniTask.Yield();

        if (!permission_request_granted)
            return result;

        await FirebaseMessaging.RequestPermissionAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log("[FirebaseMessaging] Notification permission granted.");
                result = true;
            }
            else
            {
                Debug.LogError("[FirebaseMessaging] Notification permission denied.");
                result = false;
            }
        });

        return result;
    }

}
