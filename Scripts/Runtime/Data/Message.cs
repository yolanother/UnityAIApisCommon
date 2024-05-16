using System;
using DoubTech.ThirdParty.AI.Common.Data;
using UnityEngine;

namespace DoubTech.ThirdParty.AI.Common
{
    [Serializable]
    public class Message
    {
        [SerializeField] public Roles role;
        [TextArea]
        [SerializeField] public string content;
    }
}
