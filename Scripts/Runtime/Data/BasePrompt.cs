using UnityEngine;

namespace DoubTech.ThirdParty.AI.Common
{
    [CreateAssetMenu(fileName = "Base Prompt", menuName = "DoubTech/Third Party/AI/Base Prompt", order = 0)]
    public class BasePrompt : ScriptableObject
    {
        [SerializeField] public Message[] messages;
    }
}
