using System.Threading.Tasks;
using DoubTech.ThirdParty.AI.Common.Data;
using Meta.Voice.NPCs.OpenAIApi.Providers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace DoubTech.ThirdParty.AI.Common
{
    /// <summary>
    /// A wrapper around BaseAIStreamingAPI to make swapping between llm providers quick and easy.
    /// </summary>
    public class LLMRequestRunner : MonoBehaviour
    {
        [Header("API")]
        [SerializeField] private BaseAIStreamingAPI api;
        
        [Header("Prompt Configuration")]
        [SerializeField] private bool preserveMessageHistory;
        [SerializeField] private BasePrompt basePrompt;
        [SerializeField] private Message[] messages;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<string> onResult = new UnityEvent<string>();

        public BasePrompt BasePrompt => basePrompt;

        public void Prompt(string text)
        {
            _ = PromptAsync(text);
        }
        
        public async Task<Response> PromptAsync(string text, bool includeMessageHistory = false) 
        {
            if(basePrompt) api.BasePrompt = basePrompt;
            api.preserveMessageHistory = preserveMessageHistory;
            var response = await api.PromptAsync(messages, text, preserveMessageHistory && includeMessageHistory);
            if(null != response) onResult?.Invoke(response.response);
            return response;
        }
    }
    
#if UNITY_EDITOR
[CustomEditor(typeof(LLMRequestRunner))]
public class LLMRequestRunnerInspector : Editor
{
    private string inputText = string.Empty;
    private string resultText = string.Empty;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LLMRequestRunner myScript = (LLMRequestRunner)target;

        inputText = EditorGUILayout.TextArea(inputText, GUILayout.Height(100));
        if (GUILayout.Button("Submit Prompt") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
        {
            RunPromptAsync(myScript);
        }

        EditorGUILayout.LabelField("Result:");
        EditorGUILayout.TextArea(resultText, GUILayout.Height(100));
    }

    private async void RunPromptAsync(LLMRequestRunner runner)
    {
        var response = await runner.PromptAsync(inputText);
        if(null != response)
        {
            resultText = response.response ?? response.error;
            Repaint();
        }
    }
}
#endif
}
