using System.Collections.Generic;
using LostForest.Phase2.World;

namespace LostForest.Phase2.Tiles
{
    public sealed class TileDefinitionRegistry
    {
        private readonly Dictionary<int, TileDefinition> definitions = new Dictionary<int, TileDefinition>();
        private readonly float hexOuterRadiusMeters;

        public TileDefinitionRegistry(float hexOuterRadiusMeters)
        {
            this.hexOuterRadiusMeters = hexOuterRadiusMeters;
            RegisterPrototypeDefinitions();
        }

        public IReadOnlyDictionary<int, TileDefinition> Definitions => definitions;

        public TileDefinition GetDefinition(int tileId)
        {
            if (definitions.TryGetValue(tileId, out TileDefinition definition))
            {
                return definition;
            }

            definition = CreateFieldDefinition(tileId);
            definitions.Add(definition.TileId, definition);
            return definition;
        }

        public bool TryGetDefinition(int tileId, out TileDefinition definition)
        {
            return definitions.TryGetValue(tileId, out definition);
        }

        private void RegisterPrototypeDefinitions()
        {
            Register(new TileDefinition(
                FrameSettings.PlayerHomeTileId,
                "Player Home Prototype Tile",
                TileReservedRole.PlayerHomeSpawn,
                false,
                new[] { "prototype-terrain-placeholder", "home-clearing-placeholder" },
                new[] { "prototype-content-anchor-test", "home-content-placeholder" },
                true,
                TileConstructionAnchors.CreatePrototypeHexAnchors(hexOuterRadiusMeters)));

            Register(new TileDefinition(
                FrameSettings.PursuerTileId,
                "Pursuer Origin Prototype Tile",
                TileReservedRole.PursuerSpawn,
                false,
                new[] { "prototype-terrain-placeholder", "threat-origin-placeholder" },
                new[] { "prototype-content-anchor-test", "pursuer-origin-placeholder" },
                true,
                TileConstructionAnchors.CreatePrototypeHexAnchors(hexOuterRadiusMeters)));
        }

        private void Register(TileDefinition definition)
        {
            definitions[definition.TileId] = definition;
        }

        private TileDefinition CreateFieldDefinition(int tileId)
        {
            return new TileDefinition(
                tileId,
                $"Prototype Field Tile {tileId:D3}",
                TileReservedRole.None,
                true,
                new[] { "prototype-terrain-placeholder", "field-placeholder" },
                new[] { "prototype-content-anchor-test", "field-content-placeholder" },
                true,
                TileConstructionAnchors.CreatePrototypeHexAnchors(hexOuterRadiusMeters));
        }
    }
}
