namespace Networking.ServerAPI.Responce
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Utilities;

    [System.Serializable]
    public class ResponceError : IResponceError
    {
        [JsonProperty("code")]
        public string code;
        [JsonProperty("errors")]
        public JToken errors;
        public string ResponceCode { get; set; }
        public virtual string Code { get => code; set => code = value; }
        public virtual JToken Errors { get => errors; set => errors = value; }

        public string error_description { get { return ErrorDescription(this); } }

        static ResponceError()
        {
            AotHelper.EnsureType<ResponceError>();
            AotHelper.EnsureList<ResponceError>();
        }

        public static string ErrorDescription(IResponceError error)
        {
            string code = error?.Code == null ? error?.ResponceCode : (error.Code.Contains("HTTP/1.1 ") ? error.Code.After("HTTP/1.1 ").Before(" ") : error.Code);
            if (code == null) code = "";
            switch (code)
            {
                case "-1":
                    return "Request timeout error.";
                case "0":
                case "200":
                    return "";
                case "400":
                    return "Bad request.";
                case "401":
                    return "Неверные авторизационные данные";
                case "403":
                    return "Auth login request error.";
                case "404":
                    return "Bad endpoint.";
                case "405":
                    return "Not implement request.";
                case "422":
                    return "Ошибка валидации";
                case "500":
                    return "Server error";
                case "503":
                    return "Server maintenance. Service currently unavailable, try again later.";
                default:
                    break;
            }
            return $"Unknown error({code})\n{GetStringErrorView(error)}";
        }
        static string GetStringErrorView(IResponceError error)
        {
            var token = error.Errors;
            if (token == null)
                return "";
            return token.ToString().Replace(new string[] { "{", "}", "[", "]", "\n", "\r" }, "");
        }
        public override string ToString()
        {
            return GetDescription();
        }

        public virtual string GetDescription()
        {
            return error_description;
        }
    }

}