namespace DoubTech.ThirdParty.AI.Common.Data
{
    public class Request
    {
        public ApiConfig config;
        public string model;
        public Message[] messages;
        public bool stream;
    }
}