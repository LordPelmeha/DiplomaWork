using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Diploma.Generation.Model;

namespace Diploma.Presentation
{
    public static class TilemapBuilder
    {
        public static void Build(WorldData world, TileAssetSet tiles, Tilemap ground, Tilemap roads, Tilemap walls)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (tiles == null) throw new ArgumentNullException(nameof(tiles));
            if (ground == null) throw new ArgumentNullException(nameof(ground));
            if (roads == null) throw new ArgumentNullException(nameof(roads));
            if (walls == null) throw new ArgumentNullException(nameof(walls));

            ground.ClearAllTiles();
            roads.ClearAllTiles();
            walls.ClearAllTiles();

            for (int y = 0; y < world.Size.y; y++)
                for (int x = 0; x < world.Size.x; x++)
                {
                    var pos = new Vector3Int(x, y, 0);

                    var g = world.Ground.Get(x, y);
                    var r = world.Roads.Get(x, y);
                    var w = world.Walls.Get(x, y);

                    var gt = tiles.Get(g);
                    var rt = tiles.Get(r);
                    var wt = tiles.Get(w);

                    if (gt != null) ground.SetTile(pos, gt);
                    if (rt != null) roads.SetTile(pos, rt);
                    if (wt != null) walls.SetTile(pos, wt);
                }

            ground.RefreshAllTiles();
            roads.RefreshAllTiles();
            walls.RefreshAllTiles();

            var roadsRenderer = roads.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            if (roadsRenderer != null)
            {
                roadsRenderer.enabled = false;
                roadsRenderer.enabled = true;
            }

            var wallRenderer = walls.GetComponent<IsometricWallRenderer>();
            if (wallRenderer != null)
            {
                wallRenderer.Refresh();
            }
        }
    }
}