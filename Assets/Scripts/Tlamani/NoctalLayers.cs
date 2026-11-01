namespace Noctal
{
    public static class NoctalLayers
    {
        public const int World = 8, Enemy = 9, Player = 10, Interactable = 11;
        public const int WorldMask = (1 << World) | 1;
        public const int LightMask = WorldMask | (1 << Enemy) | (1 << Interactable);
    }
}
