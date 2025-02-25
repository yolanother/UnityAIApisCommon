using System.Collections.Generic;

namespace DoubTech.ThirdParty.AI.Common.Interfaces
{
    /// <summary>
    /// Modifies prompts at runtime updating the state of a message without effecting the original base prompts.
    /// </summary>
    public interface IPromptModifier
    {
        void OnModifyPrompt(List<Message> messages);
    }
}