using System.Threading.Tasks;

namespace DoubTech.ThirdParty.AI.Common.Data
{
    public interface IApiConfig
    {
        public string[] Models { get; }
        public string GetUrl(params string[] path);
        public Task RefreshModels();
    }

    public interface IBearerAuth
    {
        public string ApiKey { get; }
    }
    
    public interface IQueryParameterAuth
    {
        public string ApiKey { get; }
        public string QueryParameterName { get; }
    }

    public enum RequestAuthType
    {
        Bearer,
        QueryParameter
    }
}