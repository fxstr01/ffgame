using UnityEngine;

namespace DestructionRoyale.Data
{
    public enum BuildableType
    {
        TacticalWall,
        Barricade,
        ReinforcementPanel,
        MetalBarrier,
        TrapDoor
    }

    [CreateAssetMenu(fileName = "NewBuildable", menuName = "DestructionRoyale/Buildable Data")]
    public class BuildableData : ScriptableObject
    {
        [Header("Identity")]
        public string buildableName;
        public BuildableType buildableType;
        public Sprite buildableIcon;
        public GameObject buildablePrefab;
        public GameObject previewPrefab;

        [Header("Stats")]
        public float maxHealth = 200f;
        public float buildTime = 0.5f;
        public float decayTime = 120f;

        [Header("Cost")]
        public ResourceType requiredResource = ResourceType.Wood;
        public int resourceCost = 30;

        [Header("Placement")]
        public float placementRange = 5f;
        public float cooldownTime = 3f;
        public int maxPlacedCount = 5;
        public bool snapToGround = true;
        public bool canPlaceOnWalls = false;

        [Header("Dimensions")]
        public Vector3 dimensions = new Vector3(2f, 2f, 0.2f);
    }
}
