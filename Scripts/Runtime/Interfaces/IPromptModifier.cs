using System.Collections.Generic;

namespace DoubTech.ThirdParty.AI.Common.Interfaces
{
    public interface IPromptModifier
    {
        void OnModifyPrompt(List<Message> messages);
    }
}