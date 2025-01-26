using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DoubTech.ThirdParty.AI.Common.Attributes;
using DoubTech.ThirdParty.AI.Common.Data;
using DoubTech.ThirdParty.AI.Common.Interfaces;
using Meta.Voice.NPCs.OpenAIApi.Providers;
using Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace DoubTech.ThirdParty.AI.Common
{
    public abstract class BaseAIStreamingAPI : MonoBehaviour
    {
        [Header("Prompt Config")]
        [SerializeField]
        public bool preserveMessageHistory = true;
        [SerializeField] private BasePrompt basePrompt;
        [SerializeField] private Message[] messages;

        [Header("Server Config")] 
        [Models(nameof(apiConfig))] 
        [SerializeField] private string model;

        [SerializeField] private bool stream;

        [SerializeField] private ApiConfig apiConfig;

        [Header("Events")]
        public UnityEvent<string> onPartialResponseReceived = new UnityEvent<string>();
        public UnityEvent<string> onFullResponseReceived = new UnityEvent<string>();
        public UnityEvent<string> onError = new UnityEvent<string>();
        
        private IPartialResponseHandler[] _partialResponseHandlers;
        private IFullResponseHandler[] _fullResponseHandlers;

        public bool Stream => stream;
        public string Model => model;
        
        public BasePrompt BasePrompt {
            get => basePrompt;
            set => basePrompt = value;
        }
        
        private Response _currentResponse;

        private List<Message> _messageHistory = new List<Message>();
        private Request _requestData;
        
        private List<Message> BaseMessageHistory {
            get 
            {
                // Combine base prompt messages, messages, and a new message for prompt
                var allMessages = new List<Message>();
                if (BasePrompt != null)
                {
                    allMessages.AddRange(BasePrompt.messages);
                }
                
                return allMessages;
            }
        }

        public Message[] MessageHistory
        {
            get
            {
                var allMessages = BaseMessageHistory;

                if (messages != null)
                {
                    allMessages.AddRange(messages);
                }

                allMessages.AddRange(_messageHistory);
                return allMessages.ToArray();
            }
        }
        
        protected virtual void OnEnable() {
            if(!basePrompt)
            {
                var provider = GetComponent<IBasePromptProvider>();
                if (null != provider)
                {
                    basePrompt = provider.BasePrompt;
                }
            }

            _fullResponseHandlers = GetComponentsInChildren<IFullResponseHandler>();
            _partialResponseHandlers = GetComponentsInChildren<IPartialResponseHandler>();
        }

        public void PartialPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return;
            var promptMessages = OnPrepareMessages(prompt, true, false);
            if(null == promptMessages) return;
            Submit(promptMessages, false);
        }
        
        protected virtual List<Message> OnPrepareMessages(string prompt, bool includeMessageHistory, bool saveMessageHistory) {
            return OnPrepareMessages(null, prompt, includeMessageHistory, saveMessageHistory); 
        }
        
        protected virtual List<Message> OnPrepareMessages(IEnumerable<Message> additionalMessages, string prompt, bool includeMessageHistory, bool saveMessageHistory) {
            List<Message> promptMessages = new List<Message>();
            var promptMessage = new Message
             {
                 role = Roles.User,
                 content = prompt
             };

            if(includeMessageHistory) promptMessages.AddRange(MessageHistory);
            else if(BasePrompt) promptMessages.AddRange(BasePrompt.messages);
            
            if(null != additionalMessages) promptMessages.AddRange(additionalMessages);
            promptMessages.Add(promptMessage);
            if(saveMessageHistory) _messageHistory.Add(promptMessage);
            
            return promptMessages;
        }
            

        // Submits a prompt and maintains a history of submissions and responses
        public void Prompt(string prompt)
        {
            Prompt(prompt, true);
        }
        
        public void Prompt(string prompt, bool includeMessageHistory)
        {
            var promptMessages = OnPrepareMessages(prompt, includeMessageHistory, includeMessageHistory);
            if(null == promptMessages) return;
            Submit(promptMessages, includeMessageHistory);
        }

        protected virtual string GetRole(Roles role)
        {
            return role.ToString().ToLower();
        }

        private void Submit(List<Message> promptMessages, bool saveMessageHistory)
        {
            var request = OnPrepareRequest(promptMessages);
            
            StopAllCoroutines();
            StartCoroutine(SendRequest(request, saveMessageHistory));
        }
        
        public class LowercaseUnderscoreEnumConverter : StringEnumConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                string enumText = value.ToString();
                string lowercaseUnderscoreText = enumText.ToLowerInvariant().Replace(" ", "_");
                writer.WriteValue(lowercaseUnderscoreText);
            }
        }
        #region Coroutine

        protected string OnSerializePost(Request request)
        {
            return JsonConvert.SerializeObject(OnPrepareData(request), new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new LowercaseUnderscoreEnumConverter() }
            });
        }

        private byte[] PreparePostData(IEnumerable<Message> messages)
        {
            _requestData = new Request
            {
                config = apiConfig,
                model = model,
                messages = messages.ToArray(),
                stream = stream
            };
            var postString = OnSerializePost(_requestData);
            var postData = Encoding.UTF8.GetBytes(postString);
            return postData;
        }

        private string PrepareRequestUrl()
        {
            string url = apiConfig.GetUrl(OnGetRequestPath());
            url = apiConfig.GenerateFullUrl(url);
            return url;
        }
        
        protected virtual UnityWebRequest OnPrepareRequest(List<Message> promptMessages)
        {
            var postData = PreparePostData(promptMessages);
            var url = PrepareRequestUrl();

            var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(postData),
                downloadHandler = new StreamingDownloadHandler(OnDataReceived, stream),
                method = UnityWebRequest.kHttpVerbPOST
            };
            request.SetRequestHeader("Content-Type", "application/json");

            if (apiConfig is IBearerAuth bearerAuth)
            {
                if(string.IsNullOrEmpty(bearerAuth.ApiKey))
                {
                    Debug.LogError($"Missing api key on config attached to {name}");
                }
                request.SetRequestHeader("Authorization", "Bearer " + bearerAuth.ApiKey);
            }
            
            return request;
        }

        protected abstract object OnPrepareData(Request requestData);

        protected abstract string[] OnGetRequestPath();

        IEnumerator SendRequest(UnityWebRequest request, bool saveMessageHistory)
        {
            _currentResponse = new Response();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                #if UNITY_EDITOR
                Debug.LogError(request.error + "\n" + request.uri);
                #else
                Debug.LogError(request.error);
                #endif
            }
            
            while(!request.isDone)
            {
                yield return null;
            }
            if(preserveMessageHistory && saveMessageHistory) 
            {
                _messageHistory.Add(new Message
                {
                    role = Roles.Assistant,
                    content = _currentResponse.response
                });
            }
            onFullResponseReceived?.Invoke(_currentResponse.response);
            Debug.Log(_currentResponse);
        }
        #endregion
        
        #region Async        
        public async Task<Response> PromptAsync(string prompt, bool includeMessageHistory = true)
        {
            var promptMessages = OnPrepareMessages(prompt, includeMessageHistory, includeMessageHistory);
            var request = await OnPostAsync(promptMessages);
            return await SendRequestAsync(request, includeMessageHistory);
        }

        public async Task<Response> PromptAsync(Message[] messages, string prompt, bool includeMessageHistory = true)
        {
            var promptMessages = OnPrepareMessages(messages, prompt, includeMessageHistory, includeMessageHistory);
            var request = await OnPostAsync(promptMessages);
            return await SendRequestAsync(request, includeMessageHistory);
        }

        /// <summary>
        /// Create an LLM request with no message history/tracking. This is good for asking random functional questions
        /// to the llm. None of the response events will be processed in this result and you will ge the raw response
        /// data back to process yourself.
        /// </summary>
        /// <param name="messages">The set of messages to send as the request prompt</param>
        /// <returns>The response including the response text or any errors</returns>
        public async Task<Response> LlmRequestAsync(params Message[] messages)
        {
            var request = await OnPostAsync(messages);
            return await SendUnhandledRequestAsync(request);
        }

        protected struct RequestData
        {
            public string url;
            public string content;
        }

        protected virtual async Task<RequestData> OnPostAsync(IEnumerable<Message> promptMessages)
        {   
            _requestData = new Request
            {
                config = apiConfig,
                model = model,
                messages = promptMessages.ToArray(),
                stream = stream
            };
            var postData = OnSerializePost(_requestData);
            string url = apiConfig.GetUrl(OnGetRequestPath());
            url = apiConfig.GenerateFullUrl(url);
            
            Debug.Log("Full url: " + url);
        
            return new RequestData {
                url = url,
                content = postData
            };
        }
        
        protected virtual async Task<Response> SendRequestAsync(RequestData request, bool includeMessageHistory)
        {
            using var client = new HttpClient();
            var content = new StringContent(request.content);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            if (apiConfig is IBearerAuth bearerAuth)
            {
                if(string.IsNullOrEmpty(bearerAuth.ApiKey))
                {
                    Debug.LogError($"Missing api key on config attached to {name}");
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerAuth.ApiKey);
            }
            var response = await client.PostAsync(request.url, content);
            _currentResponse = new Response();
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _currentResponse.error = $"Status Code: {response.StatusCode}\nReason: {response.ReasonPhrase}\n\nContent: {errorContent}\n\nRequest Content: {request.content}";
                Debug.LogError(_currentResponse.error);
            }
            else
            {
                var result = await response.Content.ReadAsStringAsync();
                _currentResponse = Stream ? OnHandleStreamedResponse(result, _currentResponse)
                    : OnHandleResponse(result, _currentResponse);
                if(preserveMessageHistory && includeMessageHistory) 
                {
                    _messageHistory.Add(new Message
                    {
                        role = Roles.Assistant,
                        content = _currentResponse.response
                    });
                }
                onFullResponseReceived?.Invoke(_currentResponse.response);
            }

            return _currentResponse;
        }
        
        protected virtual async Task<Response> SendUnhandledRequestAsync(RequestData request)
        {
            using var client = new HttpClient();
            var content = new StringContent(request.content);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            if (apiConfig is IBearerAuth bearerAuth)
            {
                if(string.IsNullOrEmpty(bearerAuth.ApiKey))
                {
                    Debug.LogError($"Missing api key on config attached to {name}");
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerAuth.ApiKey);
            }
            var response = await client.PostAsync(request.url, content);
            var processedResponse = new Response();
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                processedResponse.error = $"Status Code: {response.StatusCode}\nReason: {response.ReasonPhrase}\n\nContent: {errorContent}\n\nRequest Content: {request.content}";
                Debug.LogError(processedResponse.error);
            }
            else
            {
                var result = await response.Content.ReadAsStringAsync();
                processedResponse = OnHandleResponse(result, processedResponse);
            }

            return processedResponse;
        }
        #endregion

        private void OnDataReceived(byte[] data)
        {
            var text = Encoding.UTF8.GetString(data);
            if (!stream)
            {
                HandleFullData(text);
                return;
            }

            // Assuming the API sends newline-delimited JSON blobs
            var jsonBlobs = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var blob in jsonBlobs)
            {
                if (!_requestData.stream) HandleFullData(blob);
                else HandleStreamedData(blob);
            }
        }

        protected abstract Response OnHandleStreamedResponse(string blob, Response currentResponse);
        private void HandleStreamedData(string blob)
        {
            HandleResponseData(OnHandleStreamedResponse(blob, _currentResponse));
        }

        protected abstract Response OnHandleResponse(string blob, Response currentResponse);

        private void HandleFullData(string blob)
        {
            HandleResponseData(OnHandleResponse(blob, _currentResponse));
        }

        private void HandleResponseData(Response processedResponse)
        {
            if (null != processedResponse)
            {
                _currentResponse = processedResponse;
                if (!string.IsNullOrEmpty(processedResponse.error))
                {
                    onError?.Invoke(processedResponse.error);
                }
                else if (!string.IsNullOrEmpty(processedResponse.response))
                {
                    if (processedResponse.isFullResponse)
                    {
                        onFullResponseReceived?.Invoke(processedResponse.response);
                        foreach (var fullResponseHandler in _fullResponseHandlers)
                        {
                            fullResponseHandler.OnFullResponse(processedResponse.response);
                        }
                    }
                    else
                    {
                        onPartialResponseReceived?.Invoke(processedResponse.response);
                        foreach (var partialResponseHandler in _partialResponseHandlers)
                        {
                            partialResponseHandler.OnPartialResponse(processedResponse.response);
                        }
                    }
                }
            }
        }

        private class StreamingDownloadHandler : DownloadHandlerScript
        {
            private Action<byte[]> onDataReceived;
            private MemoryStream _buffer;

            public StreamingDownloadHandler(Action<byte[]> onDataReceivedCallback, bool chunk) : base(new byte[1024])
            {
                onDataReceived = onDataReceivedCallback;
                if (!chunk)
                {
                    _buffer = new MemoryStream();
                }
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (data == null || dataLength == 0)
                {
                    return false;
                }

                if (null != _buffer)
                {
                    _buffer.Write(data, 0, dataLength);
                }
                else
                {
                    // TODO: Handle chunked data that is > one receive.
                    var dataCopy = new byte[dataLength];
                    Buffer.BlockCopy(data, 0, dataCopy, 0, dataLength);
                    onDataReceived?.Invoke(dataCopy);
                }

                return true;
            }

            protected override void CompleteContent()
            {
                base.CompleteContent();
                if (null != _buffer)
                {
                    onDataReceived?.Invoke(_buffer.GetBuffer());
                }
            }
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(BaseAIStreamingAPI), true)]
    public class OpenAIStreamingAPIEditor : Editor
    {
        private string _prompt;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!Application.isPlaying) return;
            GUILayout.Space(16);
            // Create a text area prompt field
            _prompt = EditorGUILayout.TextArea(_prompt);
            var streamingAPI = target as BaseAIStreamingAPI;
            if (GUILayout.Button("Submit Prompt"))
            {
                streamingAPI.Prompt(_prompt);
            }
            
            GUILayout.Space(16);
            EditorGUILayout.LabelField("Conversation History", EditorStyles.boldLabel);
            // Display text areas for all of the messages in the conversation history
            foreach (var message in streamingAPI.MessageHistory)
            {
                GUILayout.Label(message.role.ToString());
                EditorGUILayout.TextArea(message.content);
            }
        }
    }
    #endif
}