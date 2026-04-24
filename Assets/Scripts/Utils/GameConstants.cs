namespace DestructionRoyale.Utils
{
    public static class GameConstants
    {
        // Layers
        public const string LAYER_PLAYER = "Player";
        public const string LAYER_BUILDING = "Building";
        public const string LAYER_RESOURCE = "Resource";
        public const string LAYER_GROUND = "Ground";
        public const string LAYER_PROJECTILE = "Projectile";

        // Tags
        public const string TAG_HEAD = "Head";
        public const string TAG_BODY = "Body";
        public const string TAG_LIMB = "Limb";
        public const string TAG_WALL = "Wall";
        public const string TAG_RESOURCE = "Resource";

        // Network
        public const int MAX_PLAYERS = 50;
        public const int MIN_PLAYERS = 2;
        public const float TICK_RATE = 64f;

        // Gameplay
        public const float DEFAULT_MATCH_DURATION = 900f;
        public const float DEFAULT_COUNTDOWN = 10f;
        public const float RESPAWN_DELAY = 5f;

        // Building
        public const int MAX_PLACED_STRUCTURES_DEFAULT = 10;
        public const float STRUCTURE_DECAY_DEFAULT = 120f;
        public const float BUILD_COOLDOWN_DEFAULT = 2f;

        // Resources
        public const int MAX_RESOURCE_STACK = 999;
        public const float GATHER_RANGE = 3f;

        // Combat
        public const float HEADSHOT_MULTIPLIER = 2f;
        public const float GRENADE_FUSE_TIME = 3f;
        public const float GRENADE_RADIUS = 5f;
    }
}
