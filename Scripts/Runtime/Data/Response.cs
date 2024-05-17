using Newtonsoft.Json;

namespace DoubTech.ThirdParty.AI.Common.Data
{
    public class Response
    {
        public string response = "";
        public string rawResponse = "";
        private object parsedResponse;
        public bool isFullResponse;
        public string error;

        public T GetResponseObject<T>() => (T)parsedResponse;

        public T ParseResponse<T>(string blob)
        {
            rawResponse = blob;
            if (null != parsedResponse) return (T)parsedResponse;
            parsedResponse = JsonConvert.DeserializeObject<T>(rawResponse);
            return (T) parsedResponse;
        }
    }
}