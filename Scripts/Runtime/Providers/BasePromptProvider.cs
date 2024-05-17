using DoubTech.ThirdParty.AI.Common;
using UnityEngine;

namespace Meta.Voice.NPCs.OpenAIApi.Providers
{
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