using UnityEngine;
using UnityEngine.Tilemaps;
using Diploma.Generation.Model;

namespace Diploma.Presentation
{
    [CreateAssetMenu(menuName = "Diploma/Tile Asset Set", fileName = "TileAssetSet")]
    public sealed class TileAssetSet : ScriptableObject
    {
        [Header("Ground / Biomes")]
        public TileBase grass;
        public TileBase dirt;
        public TileBase sand;
        public TileBase stone;
        public TileBase flower;

        [Header("Roads")]
        public TileBase road;

        [Header("Buildings")]
        public TileBase wall;
        public TileBase floor;

        public TileBase Get(TileType type) => type switch
        {
            TileType.Grass => grass,
            TileType.Dirt => dirt,
            TileType.Sand => sand,
            TileType.Stone => stone,
            TileType.Flower => flower,
            TileType.Road => road,
            TileType.Wall => wall,
            TileType.Floor => floor,
            _ => null
        };
    }
}