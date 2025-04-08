using System;
using UnityEngine;
using UnityEngine.UI;

public class BaseScreen : MonoBehaviour
{
    public static BaseScreen Instance => instance;
    protected static BaseScreen instance;
    public static Transform MainCanvas => GameObject.FindGameObjectWithTag("MainCanvas").transform;
    float t = 0;
    float timeWait = 60;

    protected virtual void Start()
    {
        timeWait = INISetting.GetValueWithAdd("TIMEOUT", timeWait);
        if (instance == null)
        {
            //Debug.LogError("Run from method 'Create'");
            instance = this;
        }
    }
    protected virtual void Update()
    {
        if (Input.anyKey) t = 0;
        if (t >= timeWait)
        {
            t = 0;
            GoToStartScreen();
        }
        else t += Time.deltaTime;
    }
    public virtual void GoToStartScreen() { }

    public void SelfDestroy()
    {
        instance = null;
        Destroy(gameObject);
    }

    public static BaseScreen Create(string screenTypeName)
    {
        return Create(screenTypeName, "Screens", MainCanvas);
    }
    public static BaseScreen Create(string screenTypeName, string resourceFolder, Transform parent)
    {
        if (instance != null)
            Destroy(instance.gameObject, 1);
        var scr = Resources.Load<BaseScreen>($"{resourceFolder}/{screenTypeName}");
        instance = Instantiate(scr, parent);
        instance.name = screenTypeName;
        return instance;
    }
}
public abstract class BaseScreen<T> : BaseScreen where T : BaseScreen
{
    public static T Current => (T)instance;
    [SerializeField] protected Button backButton;

    protected override void Start()
    {
        base.Start();
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClick);
    }

    public abstract void OnBackClick();

    public static T Create()
    {
        return Create("Screens", MainCanvas);
    }
    public static T Create(string resourceFolder, Transform parent)
    {
        var name = typeof(T).Name;
        //if (instance != null)
        //    Destroy(instance.gameObject, 1);
        //var scr = Resources.Load<T>($"{resourceFolder}/{name}");
        //instance = Instantiate(scr, parent);
        //instance.name = name;
        //return (T)instance;
        return (T)Create(name, resourceFolder, parent);
    }
}