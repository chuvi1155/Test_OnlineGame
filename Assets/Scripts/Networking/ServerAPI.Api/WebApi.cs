using Networking.ServerAPI.Responce;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Networking.ServerAPI.Api
{
    public class BaseWebApi : MonoBehaviour
    {
        public const string PlayerPrefsAuthToken = "atoken";
        private static bool _isOnCooldown = false;
        private const int _delayTime = 5;
        private static bool lastInternetState = true;
        public static bool CheckInternet(string ipAddress, int timeoutMs)
        {
            if (_isOnCooldown) return lastInternetState;

            var ping = new Ping(ipAddress);

            var sw = Stopwatch.StartNew();
            while (!ping.isDone || ping.time == -1)
            {
                if (sw.ElapsedMilliseconds > timeoutMs)
                {
                    lastInternetState = false;
                    return false;
                }
            }

            lastInternetState = true;
            Task.Run(() => TimerDelay(_delayTime));
            return lastInternetState;
        }

        private static async Task TimerDelay(int seconds)
        {
            _isOnCooldown = true;
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            _isOnCooldown = false;
        }
    }
    /// <summary>
    /// Базовый класс API запросов
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    public class BaseWebApi<TClass> : BaseWebApi where TClass : Component
    {
        public delegate void OnCustomRequestComplete<in TCustomResp, in TRespError>(TCustomResp obj, TRespError errors);
        public delegate void OnCustomArrayRequestComplete<in TCustomResp, in TRespError>(TCustomResp[] obj, TRespError errors);

        protected WWWForm postform;
        [SerializeField] protected string url;
        protected UnityWebRequestAsyncOperation request;
        protected bool Verbose = true;

        public bool DestroyAfterComplete = true;
        public event System.Action<TClass> onDestroyed;


        public static TClass Create()
        {
            var go = Instantiate(Resources.Load<GameObject>("Network/WaitWebRequest"));
            go.name = $"WaitWebRequest ({typeof(TClass).Name})";            
            return go.AddComponent<TClass>();
        }

        protected virtual void AddAuthData(UnityWebRequest www)
        {
            www.SetRequestHeader("Accept", "application/json");
        }

        protected virtual void Post<TCustomResp, TCustomError>(OnCustomRequestComplete<TCustomResp, TCustomError> onComplete) where TCustomError : IResponceError
        {
            StartCoroutine(_Post(onComplete));
        }

        protected virtual void Get<TCustomResp, TCustomError>(OnCustomRequestComplete<TCustomResp, TCustomError> onComplete) where TCustomError : IResponceError
        {
            StartCoroutine(_Get(onComplete));
        }
        protected async Task<bool> TimerDelay(int seconds, CancellationTokenSource source)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            source.Cancel();
            return false;
        }

        private IEnumerator _Post<TCustomResp, TCustomError>(OnCustomRequestComplete<TCustomResp, TCustomError> onComplete) where TCustomError : IResponceError
        {
            using (UnityWebRequest www = UnityWebRequest.Post(url, postform))
            {
                AddAuthData(www);
                Debug.Log($"POST request: {url}\n{(postform != null ? Encoding.ASCII.GetString(postform.data) : "")}");
                request = www.SendWebRequest();
                yield return request;

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (Verbose)
                        Debug.LogWarning(url + "\n" + www.error + "\n" + www.downloadHandler.text);

                    if (www.responseCode == 503)
                    {
                        //MessageBox.ShowError("Server maintenance", "Service currently unavailable, try again later.");
                        Debug.LogError("Service currently unavailable, try again later.");
                    }
                    TCustomError error = JsonConvert.DeserializeObject<TCustomError>(www.downloadHandler.text);
                    onComplete(default, error);
                }
                else
                {
                    if (Verbose)
                        Debug.Log("response: "+ url + "\n" + www.downloadHandler.text);
                    if (typeof(TCustomResp).IsArray)
                        onComplete((TCustomResp)(object)JsonConvert.DeserializeObject<List<TCustomResp>>(www.downloadHandler.text).ToArray(), default(TCustomError));
                    else onComplete(JsonConvert.DeserializeObject<TCustomResp>(www.downloadHandler.text), default(TCustomError));
                }
            }
            if (DestroyAfterComplete)
                Destroy(gameObject);
        }
        private IEnumerator _Get<TCustomResp, TCustomError>(OnCustomRequestComplete<TCustomResp, TCustomError> onComplete) where TCustomError : IResponceError
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                AddAuthData(www);
                Debug.Log($"GET request: {url}\n{(postform != null ? Encoding.ASCII.GetString(postform.data) : "")}");
                request = www.SendWebRequest();
                yield return request;

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (Verbose)
                        Debug.LogWarning(url + "\n" + www.error + "\n" + www.downloadHandler.text);

                    if (www.responseCode == 503)
                    {
                        //MessageBox.ShowError("Server maintenance", "Service currently unavailable, try again later.");
                        Debug.LogError("Service currently unavailable, try again later.");
                    }
                    TCustomError error = JsonConvert.DeserializeObject<TCustomError>(www.downloadHandler.text);
                    onComplete(default, error);
                }
                else
                {
                    if (Verbose)
                        Debug.Log("response: " + url + "\n" + www.downloadHandler.text);
                    if(typeof(TCustomResp).IsArray)
                        onComplete(JsonConvert.DeserializeObject<TCustomResp>(www.downloadHandler.text), default(TCustomError));
                    else onComplete(JsonConvert.DeserializeObject<TCustomResp>(www.downloadHandler.text), default(TCustomError));
                }
            }
            if (DestroyAfterComplete)
                Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            onDestroyed?.Invoke((TClass)(object)this);
            onDestroyed = null;
        }
    }

    /// <summary>
    /// API без заголовок авторизации
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    /// <typeparam name="TResp"></typeparam>
    /// <typeparam name="TError"></typeparam>
    public class UnsecureWebApi<TClass, TResp, TError> : BaseWebApi<TClass> where TClass : Component
                                                                            where TError : IResponceError
    {
        public delegate void OnRawRequestComplete(string json_resp, TError error);
        public delegate void OnRequestComplete(TResp resp, TError error);
        public delegate void OnRequestArrayComplete(TResp[] resp, TError error);
        public delegate void OnRequestImageComplete(string url, Texture2D resp);

        protected OnRawRequestComplete onRawComplete;
        protected OnRequestComplete onComplete;
        protected OnRequestArrayComplete onCompleteArray;
        protected OnRequestImageComplete onCompleteImage;

        protected virtual void Post()
        {    
            StartCoroutine(_Post());
        }        
        private IEnumerator _Post()
        {
            using (UnityWebRequest www = UnityWebRequest.Post(url, postform))
            {
                AddAuthData(www);
                var requestBody = postform != null ? UnityWebRequest.EscapeURL(Encoding.UTF8.GetString(postform.data)) : "";
                Debug.Log($"POST Request({(www.uploadHandler != null ? www.uploadHandler.contentType : "www.uploadHandler is null")}): " + url + "\n" + requestBody);
                request = www.SendWebRequest();
                yield return request;
                RequestResponse(www);
            }
            if (DestroyAfterComplete)
                Destroy(gameObject);
        }
        protected virtual void Post(string postData)
        {
            StartCoroutine(_Post(postData));
        }
        private IEnumerator _Post(string postData)
        {
            using (UnityWebRequest www = UnityWebRequest.Post(url, postData, "application/json"))
            {
                AddAuthData(www);
                var requestBody = postform != null ? UnityWebRequest.EscapeURL(Encoding.UTF8.GetString(postform.data)) : "";
                Debug.Log($"POST Request({(www.uploadHandler != null ? www.uploadHandler.contentType : "www.uploadHandler is null")}): " + url + "\n" + requestBody);
                request = www.SendWebRequest();
                yield return request;
                RequestResponse(www);
            }
            if (DestroyAfterComplete)
                Destroy(gameObject);
        }

        private void RequestResponse(UnityWebRequest www)
        {
            if (www.result != UnityWebRequest.Result.Success)
            {
                if (Verbose)
                    Debug.LogError("[POST] " + url + "\n" + www.error + "\n" + www.downloadHandler.text);
                TError error = JsonConvert.DeserializeObject<TError>(www.downloadHandler.text);
                error.ResponceCode = www.responseCode.ToString();
                if (www.responseCode == 503)
                {
                    //MessageBox.ShowError("Server maintenance", "Service currently unavailable, try again later.");
                    Debug.LogError("Service currently unavailable, try again later.");
                }
                try
                {
                    onRawComplete?.Invoke("", error);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[onRawComplete] Error: {e.Message}");
                    onRawComplete?.Invoke(default, default);
                }
                try
                {
                    onComplete?.Invoke(default, error);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[onComplete] Error: {e.Message}");
                    onComplete?.Invoke(default, default);
                }
                try
                {
                    onCompleteArray?.Invoke(null, error);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[onCompleteArray] Error: {e.Message}");
                    onCompleteArray?.Invoke(null, default);
                }
            }
            else
            {
                string resp = www.downloadHandler.text;
                if (Verbose)
                {
                    if (!string.IsNullOrEmpty(resp.Replace("{", "").Replace("}", "")))
                        Debug.Log($"Response: " + url + "\n" + resp);
                }

                if (onRawComplete != null)
                {
                    try
                    {
                        onRawComplete?.Invoke(resp, default);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(ex.Message);
                    }
                }

                if (onComplete != null)
                {
                    try
                    {
                        onComplete?.Invoke(JsonConvert.DeserializeObject<TResp>(resp), default);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(ex.Message);
                    }
                }

                if (onCompleteArray != null)
                {
                    try
                    {
                        onCompleteArray?.Invoke(JsonConvert.DeserializeObject<List<TResp>>(resp).ToArray(), default);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(ex.Message);
                    }
                }
            }
        }

        protected virtual void Put()
        {
            StartCoroutine(_Put());
        }
        private IEnumerator _Put()
        {
            var requestBody = postform != null ? UnityWebRequest.EscapeURL(Encoding.UTF8.GetString(postform.data)) : "";
            using (UnityWebRequest www = UnityWebRequest.Put(url, requestBody))
            {
                AddAuthData(www);
                Debug.Log($"PUT Request({(www.uploadHandler != null ? www.uploadHandler.contentType : "www.uploadHandler is null")}): " + url + "\n" + requestBody);
                request = www.SendWebRequest();
                yield return request;
                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (Verbose)
                        Debug.LogWarning("[PUT] " + url + "\n" + www.error + "\n" + www.downloadHandler.text);
                    TError error = JsonConvert.DeserializeObject<TError>(www.downloadHandler.text);
                    error.ResponceCode = www.responseCode.ToString();
                    if (www.responseCode == 503)
                    {
                        //MessageBox.ShowError("Server maintenance", "Service currently unavailable, try again later.");
                        Debug.LogError("Service currently unavailable, try again later.");
                    }
                    try
                    {
                        onRawComplete?.Invoke("", error);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[onRawComplete] Error: {e.Message}");
                        onRawComplete?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = e.Message }));
                    }
                    try
                    {
                        onComplete?.Invoke(default, error);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[onComplete] Error: {e.Message}");
                        onComplete?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = e.Message }));
                    }
                    try
                    {
                        onCompleteArray?.Invoke(null, error);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[onCompleteArray] Error: {e.Message}");
                        onCompleteArray?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = e.Message }));
                    }
                }
                else
                {
                    string resp = www.downloadHandler.text;
                    if (Verbose)
                    {
                        if (!string.IsNullOrEmpty(resp.Replace("{", "").Replace("}", "")))
                            Debug.Log($"Response: " + url + "\n" + resp);
                    }

                    if (onRawComplete != null)
                    {
                        try
                        {
                            onRawComplete?.Invoke(resp, default);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            onRawComplete?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = ex.Message }));
                        }
                    }

                    if (onComplete != null)
                    {
                        try
                        {
                            onComplete?.Invoke(JsonConvert.DeserializeObject<TResp>(resp), default);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            onComplete?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = ex.Message }));
                        }
                    }

                    if (onCompleteArray != null)
                    {
                        try
                        {
                            onCompleteArray?.Invoke(JsonConvert.DeserializeObject<List<TResp>>(resp).ToArray(), default);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            onCompleteArray?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = ex.Message }));
                        }
                    }
                }
            }
            if (DestroyAfterComplete)
                Destroy(gameObject);
        }
        protected virtual void Delete()
        {
            StartCoroutine(_Delete());
        }
        private IEnumerator _Delete()
        {
            using (UnityWebRequest www = UnityWebRequest.Delete(url))
            {
                AddAuthData(www);
                Debug.Log($"DELETE Request({(www.uploadHandler != null ? www.uploadHandler.contentType : "www.uploadHandler is null")}): " + url);
                www.downloadHandler = new DownloadHandlerBuffer();
                request = www.SendWebRequest();
                yield return request;
                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (Verbose)
                        Debug.LogWarning("[DELETE] " + url + "\n" + www.error + "\n" + www.downloadHandler.text);
                    TError error = JsonConvert.DeserializeObject<TError>(www.downloadHandler.text);
                    error.ResponceCode = www.responseCode.ToString();
                    if (www.responseCode == 503)
                    {
                        //MessageBox.ShowError("Server maintenance", "Service currently unavailable, try again later.");
                        Debug.LogError("Service currently unavailable, try again later.");
                    }
                    try
                    {
                        onRawComplete?.Invoke("", error);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[onRawComplete] Error: {e.Message}");
                        onRawComplete?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = e.Message }));
                    }
                    try
                    {
                        onComplete?.Invoke(default, error);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[onComplete] Error: {e.Message}");
                        onComplete?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = e.Message }));
                    }
                    try
                    {
                        onCompleteArray?.Invoke(null, error);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[onCompleteArray] Error: {e.Message}");
                        onCompleteArray?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = e.Message }));
                    }
                }
                else
                {
                    string resp = www.downloadHandler.text;
                    if (Verbose)
                    {
                            Debug.Log($"Delete is done : " + url);
                    }

                    if (onRawComplete != null)
                    {
                        try
                        {
                            onRawComplete?.Invoke(resp, default);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            onRawComplete?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = ex.Message }));
                        }
                    }

                    if (onComplete != null)
                    {
                        try
                        {
                            onComplete?.Invoke(JsonConvert.DeserializeObject<TResp>(resp), default);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            onComplete?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = ex.Message }));
                        }
                    }

                    if (onCompleteArray != null)
                    {
                        try
                        {
                            onCompleteArray?.Invoke(JsonConvert.DeserializeObject<List<TResp>>(resp).ToArray(), default);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            onCompleteArray?.Invoke(default, (TError)(object)(new ResponceError { ResponceCode = ex.Message }));
                        }
                    }
                }
            }
            if (DestroyAfterComplete)
                Destroy(gameObject);
        }
        protected virtual void Get()
        {
            StartCoroutine(_Get());
        }
        private IEnumerator _Get()
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                AddAuthData(www);
                request = www.SendWebRequest();
                yield return request;

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (Verbose)
                        Debug.LogWarning("[GET] " + url + "\n" + www.error + "\n" + www.downloadHandler.text);
                    TError error = JsonConvert.DeserializeObject<TError>(www.downloadHandler.text);
                    try
                    {
                        onRawComplete?.Invoke("", error);
                        onComplete?.Invoke(default, error);
                        onCompleteArray?.Invoke(null, error);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
                else
                {
                    string resp = www.downloadHandler.text;
                    if (Verbose)
                    {
                        if (!string.IsNullOrEmpty(resp.Replace("{", "").Replace("}", "")))
                            Debug.Log("[GET] " + url + "\n" + resp);
                    }
                    try
                    {
                        onRawComplete?.Invoke(resp, default);
                        onComplete?.Invoke(JsonConvert.DeserializeObject<TResp>(resp), default);
                        onCompleteArray?.Invoke(JsonConvert.DeserializeObject<List<TResp>>(resp).ToArray(), default);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
            if (DestroyAfterComplete)
                Destroy(gameObject);
        }

        protected void DownloadTexture(string uri)
        {
            StartCoroutine(_DownloadTexture(uri));
        }
        private IEnumerator _DownloadTexture(string uri)
        {
            Debug.Log("DownloadTexture: " + uri);
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(uri))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning(www.error);
                    onCompleteImage?.Invoke(uri, null);
                }
                else
                {
                    Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    onCompleteImage?.Invoke(uri, myTexture);
                }
            }
            if (DestroyAfterComplete)
                Destroy(gameObject);
        }
    }
    /// <summary>
    /// API с заголовками авторизации, включаемые в запросы
    /// </summary>
    /// <typeparam name="TClass"></typeparam>
    /// <typeparam name="TResp"></typeparam>
    /// <typeparam name="TError"></typeparam>
    public class SecureWebApi<TClass, TResp, TError> : UnsecureWebApi<TClass, TResp, TError> where TClass : Component
                                                                          where TError : IResponceError
    {

        protected override void AddAuthData(UnityWebRequest www)
        {
            if (PlayerPrefs.HasKey(PlayerPrefsAuthToken))
            {
                string token = PlayerPrefs.GetString(PlayerPrefsAuthToken);
                //Debug.Log("############## " + token);
                //www.SetRequestHeader("Authorization", $"Bearer {token}");
                www.SetRequestHeader("Content-Type", "application/json");
            }
            else
                www.SetRequestHeader("Content-Type", "application/json");
        }
    } 
}