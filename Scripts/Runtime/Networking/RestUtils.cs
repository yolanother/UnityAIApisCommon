using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DoubTech.ThirdParty.AI.Common.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace Networking
{
    public static class RestUtils
    {
        public static string GenerateFullUrl(this IApiConfig config, string baseUrl)
        {
            // Create a url to configure and add parameters to
            UriBuilder uri = new UriBuilder(baseUrl);

            if (config is IQueryParameterAuth auth)
            {
                 if(string.IsNullOrEmpty(auth.ApiKey))
                 {
                     Debug.LogError($"Missing api key on config attached to {(config as MonoBehaviour)?.name ?? config.ToString()}");
                 }
                 
                // Safely add the query parameter to the url
                if (uri.Query.Length > 1)
                {
                    uri.Query += $"&{auth.QueryParameterName}={auth.ApiKey}";
                }
                else
                {
                    uri.Query = $"{auth.QueryParameterName}={auth.ApiKey}";
                }
            }

            return uri.ToString();
        }
        
        public static async Task<string> GetDataAsync(this IApiConfig config, string url)
        {
            var uri = config.GenerateFullUrl(url);
            try
            {
                using (HttpClient _httpClient = new HttpClient())
                {
                    if (config is IBearerAuth bearer)
                    {
                        // Add the Authorization header with the Bearer token
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", bearer.ApiKey);
                    }
                    
                    HttpResponseMessage response = await _httpClient.GetAsync(uri.ToString());
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Failed to fetch: {e.Message}\n{uri}");
                return null;
            }
        }
        public static async Task<string> PostDataAsync(this IApiConfig config, string url, object body)
        {
            // Create a url to configure and add parameters to
            UriBuilder uri = new UriBuilder(url);

            if (config is IQueryParameterAuth auth)
            {
                // Safely add the query parameter to the url
                if (uri.Query.Length > 1)
                {
                    uri.Query += $"&{auth.QueryParameterName}={auth.ApiKey}";
                }
                else
                {
                    uri.Query = $"{auth.QueryParameterName}={auth.ApiKey}";
                }
            }
            
            try
            {
                using (HttpClient _httpClient = new HttpClient())
                {
                    if (config is IBearerAuth bearer)
                    {
                        // Add the Authorization header with the Bearer token
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", bearer.ApiKey);
                    }
                    
                    HttpResponseMessage response = await _httpClient.PostAsync(uri.ToString(), new StringContent(JsonConvert.SerializeObject(body)));
                    
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Failed to fetch: {e.Message}\n{uri}");
                return null;
            }
        }
        
        
        public static async Task<T> Get<T>(this IApiConfig config, params string[] path) where T:class
        {
            string response = await config.GetDataAsync(config.GetUrl(path));
            if (!string.IsNullOrEmpty(response))
            {
                return JsonConvert.DeserializeObject<T>(response);
            }

            return null;
        }
        
        
        public static async Task<T> Post<T>(this IApiConfig config, object data, params string[] path) where T:class
        {
            string response = await config.PostDataAsync(config.GetUrl(path), data);
            if (!string.IsNullOrEmpty(response))
            {
                return JsonConvert.DeserializeObject<T>(response);
            }

            return null;
        }
    }
}