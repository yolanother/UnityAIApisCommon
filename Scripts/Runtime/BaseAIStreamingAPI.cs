using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DoubTech.ThirdParty.AI.Common.Attributes;
using DoubTech.ThirdParty.AI.Common.Data;
using Networking;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace DoubTech.ThirdParty.AI.Common
{
    public abstract class BaseAIStreamingAPI : MonoBehaviour
    {
        [Header("Prompt Config")] [SerializeField]
        private BasePrompt basePrompt;

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

        public bool Stream => stream;
        public string Model => model;
        
        private Response _currentResponse;

        private List<Message> _messageHistory = new List<Message>();
        private Message _partialPrompt;
        private Request _requestData;

        public Message[] MessageHistory
        {
            get
            {
                
                // Combine base prompt messages, messages, and a new message for prompt
                var allMessages = new List<Message>();
                if (basePrompt != null)
                {
                    allMessages.AddRange(basePrompt.messages);
                }

                if (messages != null)
                {
                    allMessages.AddRange(messages);
                }

                allMessages.AddRange(_messageHistory);
                return allMessages.ToArray();
            }
        }

        public void PartialPrompt(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return;
            if (null == _partialPrompt)
            {
                _partialPrompt = new Message()
                {
                    role = Roles.User,
                    content = prompt
                };
                _messageHistory.Add(_partialPrompt);
            }

            _partialPrompt.content = prompt;
            Submit();
        }

        public void Prompt(string prompt)
        {
            if (null != _partialPrompt && prompt == _partialPrompt.content)
            {
                _partialPrompt = null;
                return;
            }

            if (null == _partialPrompt)
            {
                _messageHistory.Add(new Message
                {
                    role = Roles.User,
                    content = prompt
                });
            }
            else
            {
                _partialPrompt = null;
            }

            Submit();
        }

        protected virtual string GetRole(Roles role)
        {
            return role.ToString().ToLower();
        }

        private void Submit()
        {
            _requestData = new Request
            {
                config = apiConfig,
                model = model,
                messages = MessageHistory,
                stream = stream
            };
            var postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(OnPrepareData(_requestData)));

            string url = apiConfig.GetUrl(OnGetRequestPath());
            url = apiConfig.GenerateFullUrl(url);
            var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(postData),
                downloadHandler = new StreamingDownloadHandler(OnDataReceived, stream),
                method = UnityWebRequest.kHttpVerbPOST
            };
            request.SetRequestHeader("Content-Type", "application/json");

            if (apiConfig is IBearerAuth bearerAuth)
            {
                request.SetRequestHeader("Authorization", "Bearer " + bearerAuth.ApiKey);
            }

            if (apiConfig is IQueryParameterAuth queryParameterAuth)
            {
                
            }

            StopAllCoroutines();
            StartCoroutine(SendRequest(request));
        }

        protected abstract object OnPrepareData(Request requestData);

        protected abstract string[] OnGetRequestPath();

        IEnumerator SendRequest(UnityWebRequest request)
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
            _messageHistory.Add(new Message
            {
                role = Roles.Assistant,
                content = _currentResponse.response
            });
            onFullResponseReceived?.Invoke(_currentResponse.response);
            Debug.Log(_currentResponse);
        }

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
                    if(processedResponse.isFullResponse) onFullResponseReceived?.Invoke(processedResponse.response);
                    else onPartialResponseReceived?.Invoke(processedResponse.response);
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