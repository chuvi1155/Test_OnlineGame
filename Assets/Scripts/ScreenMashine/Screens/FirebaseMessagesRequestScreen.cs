using Firebase.Messaging;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class FirebaseMessagesRequestScreen : BaseScreen<FirebaseMessagesRequestScreen>
{
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


    private async Task<bool> RequestNotificationPermission()
    {
        bool result = false;
        await FirebaseMessaging.RequestPermissionAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log("Notification permission granted.");
                result = true;
            }
            else
            {
                Debug.LogError("Notification permission denied.");
                result = false;
            }
        });

        return result;
    }

}
