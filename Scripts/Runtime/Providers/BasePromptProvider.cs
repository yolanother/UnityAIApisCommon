using DoubTech.ThirdParty.AI.Common;
using UnityEngine;

namespace Meta.Voice.NPCs.OpenAIApi.Providers
{
    /// <summary>
    /// Provides a base prompt that will be injected at the beginning of a set of messages.
    /// </summary>
    public class BasePromptProvider : MonoBehaviour, IBasePromptProvider
    {
        [SerializeField] BasePrompt basePrompt;
        
        public BasePrompt BasePrompt => basePrompt;
    }
    
    public interface IBasePromptProvider 
    {
        public BasePrompt BasePrompt { get; }
    }
}