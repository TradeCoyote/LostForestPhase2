using System.Collections.Generic;
using LostForest.Phase2.World;

namespace LostForest.Phase2.Tiles
{
    public sealed class TileDefinitionRegistry
    {
        private readonly Dictionary<int, TileDefinition> definitions = new Dictionary<int, TileDefinition>();
        private readonly Dictionary<int, TileDefinition> landmarkDefinitions = new Dictionary<int, TileDefinition>();
        private readonly float hexOuterRadiusMeters;

        public TileDefinitionRegistry(float hexOuterRadiusMeters)
        {
            this.hexOuterRadiusMeters = hexOuterRadiusMeters;
            RegisterPrototypeDefinitions();
        }

        public IReadOnlyDictionary<int, TileDefinition> Definitions => definitions;
        public IReadOnlyDictionary<int, TileDefinition> LandmarkDefinitions => landmarkDefinitions;

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

        public TileDefinition GetDefinition(FieldSlotData slot)
        {
            if (slot == null)
            {
                return null;
            }

            return slot.IsLandmarkTile ? GetLandmarkDefinition(slot.TileId) : GetDefinition(slot.TileId);
        }

        public bool TryGetDefinition(int tileId, out TileDefinition definition)
        {
            return definitions.TryGetValue(tileId, out definition);
        }

        public bool TryGetDefinition(FieldSlotData slot, out TileDefinition definition)
        {
            definition = GetDefinition(slot);
            return definition != null;
        }

        private void RegisterPrototypeDefinitions()
        {
            Register(new TileDefinition(
                FrameSettings.PlayerHomeTileId,
                "Player Home Prototype Tile",
                TileReservedRole.PlayerHomeSpawn,
                TileContentCategory.Home,
                false,
                new[] { "prototype-terrain-placeholder", "home-clearing-placeholder" },
                new[] { "prototype-content-anchor-test", "home-content-placeholder" },
                true,
                ForestFillProfile.CreateHomePrototype(),
                TileConstructionAnchors.CreatePrototypeHexAnchors(hexOuterRadiusMeters)));

            Register(new TileDefinition(
                FrameSettings.PursuerTileId,
                "Pursuer Origin Prototype Tile",
                TileReservedRole.PursuerSpawn,
                TileContentCategory.ThreatOrigin,
                false,
                new[] { "prototype-terrain-placeholder", "threat-origin-placeholder" },
                new[] { "prototype-content-anchor-test", "pursuer-origin-placeholder" },
                true,
                ForestFillProfile.CreateDensePrototype(666),
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
                TileContentCategory.Forest,
                true,
                new[] { "prototype-terrain-placeholder", "field-placeholder" },
                new[] { "prototype-content-anchor-test", "field-content-placeholder" },
                true,
                CreatePrototypeForestFill(tileId),
                TileConstructionAnchors.CreatePrototypeHexAnchors(hexOuterRadiusMeters));
        }

        private TileDefinition GetLandmarkDefinition(int tileId)
        {
            if (landmarkDefinitions.TryGetValue(tileId, out TileDefinition definition))
            {
                return definition;
            }

            definition = CreateLandmarkDefinition(tileId);
            landmarkDefinitions.Add(definition.TileId, definition);
            return definition;
        }

        private TileDefinition CreateLandmarkDefinition(int tileId)
        {
            return new TileDefinition(
                tileId,
                $"Prototype Landmark Tile {tileId:D3}",
                TileReservedRole.None,
                TileContentCategory.Landmark,
                true,
                new[] { "prototype-terrain-placeholder", "field-placeholder", "landmark-tile-placeholder" },
                new[] { "prototype-content-anchor-test", "field-content-placeholder", "landmark-content-placeholder" },
                true,
                ForestFillProfile.CreateLandmarkPrototype(tileId),
                TileConstructionAnchors.CreatePrototypeHexAnchors(hexOuterRadiusMeters));
        }

        private static ForestFillProfile CreatePrototypeForestFill(int tileId)
        {
            switch (tileId % 3)
            {
                case 0:
                    return ForestFillProfile.CreateSparsePrototype(tileId);
                case 1:
                    return ForestFillProfile.CreateNormalPrototype(tileId);
                default:
                    return ForestFillProfile.CreateDensePrototype(tileId);
            }
        }
    }
}
