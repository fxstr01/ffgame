using UnityEngine;

namespace DestructionRoyale.Data
{
    public enum ResourceType
    {
        Wood,
        Stone,
        Metal,
        Components
    }

    [CreateAssetMenu(fileName = "NewResource", menuName = "DestructionRoyale/Resource Data")]
    public class ResourceData : ScriptableObject
    {
        public string resourceName;
        public ResourceType resourceType;
        public Sprite resourceIcon;
        public int maxStack = 999;
        public Color resourceColor = Color.white;
    }
}
