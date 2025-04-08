namespace Networking.ServerAPI.Responce
{
    using Newtonsoft.Json.Linq;
    using System;

    public interface IResponceError
    {
        string ResponceCode { get; set; }
        string Code { get; set; }
        JToken Errors { get; set; }
        string GetDescription();
    } 
}