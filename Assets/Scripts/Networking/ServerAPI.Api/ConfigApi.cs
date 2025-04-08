using Networking.ServerAPI.Responce;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Networking.ServerAPI.Api
{
    public class ConfigApi : SecureWebApi<ConfigApi, ConfigResponce, ResponceError>
    {
        //Dictionary<string, object> conversionData;
        string json_conversionData;
        public static void GetConfig(Dictionary<string, object> conversionData, OnRawRequestComplete onComplete)
        {
            var api = Create();
            api.url = URLS.ConfigUrl;
            api.postform = new WWWForm();
            foreach (var data in conversionData)
                api.postform.headers.Add(data.Key, data.Value?.ToString());
            //api.conversionData = conversionData;
            api.onRawComplete = onComplete;
            api.Post();
        }
        public static void GetConfig(string json_conversionData, OnRequestComplete onComplete)
        {
            Debug.Log("GetConfig: " + json_conversionData);
            var api = Create();
            api.url = URLS.ConfigUrl;
            api.json_conversionData = json_conversionData;
            api.onComplete = onComplete;
            api.Post();
        }

        public static void GetConfigRaw(string json_conversionData, OnRawRequestComplete onComplete)
        {
            Debug.Log("GetConfigRaw: " + json_conversionData);
            var api = Create();
            api.url = URLS.ConfigUrl;
            api.json_conversionData = json_conversionData;
            api.onRawComplete = onComplete;
            api.Post();
        }

        public static void GetConfigRawPostData(string json_conversionData, OnRawRequestComplete onComplete)
        {
            Debug.Log("GetConfigRaw: " + json_conversionData);
            var api = Create();
            api.url = URLS.ConfigUrl;
            api.onRawComplete = onComplete;
            api.Post(json_conversionData);
        }

        protected override void AddAuthData(UnityWebRequest www)
        {
            //byte[] bodyRaw = Encoding.UTF8.GetBytes(json_conversionData);
            //www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            //base.AddAuthData(www);

            //foreach (var data in conversionData)
            //www.SetRequestHeader(data.Key, data.Value?.ToString());
        }
    }
}
