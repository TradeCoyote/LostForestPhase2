using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.Tiles
{
    [Serializable]
    public sealed class TileConstructionAnchors
    {
        [SerializeField] private TileAnchor centerAnchor;
        [SerializeField] private List<TileAnchor> edgeAnchors = new List<TileAnchor>();
        [SerializeField] private List<TileAnchor> cornerAnchors = new List<TileAnchor>();
        [SerializeField] private List<TileAnchor> innerAnchors = new List<TileAnchor>();
        [SerializeField] private List<TileAnchor> propAnchors = new List<TileAnchor>();

        public TileConstructionAnchors(
            TileAnchor centerAnchor,
            IEnumerable<TileAnchor> edgeAnchors,
            IEnumerable<TileAnchor> cornerAnchors,
            IEnumerable<TileAnchor> innerAnchors,
            IEnumerable<TileAnchor> propAnchors)
        {
            this.centerAnchor = centerAnchor;
            this.edgeAnchors = edgeAnchors == null ? new List<TileAnchor>() : new List<TileAnchor>(edgeAnchors);
            this.cornerAnchors = cornerAnchors == null ? new List<TileAnchor>() : new List<TileAnchor>(cornerAnchors);
            this.innerAnchors = innerAnchors == null ? new List<TileAnchor>() : new List<TileAnchor>(innerAnchors);
            this.propAnchors = propAnchors == null ? new List<TileAnchor>() : new List<TileAnchor>(propAnchors);
        }

        public TileAnchor CenterAnchor => centerAnchor;
        public IReadOnlyList<TileAnchor> EdgeAnchors => edgeAnchors;
        public IReadOnlyList<TileAnchor> CornerAnchors => cornerAnchors;
        public IReadOnlyList<TileAnchor> InnerAnchors => innerAnchors;
        public IReadOnlyList<TileAnchor> PropAnchors => propAnchors;

        public IEnumerable<TileAnchor> AllContentAnchors
        {
            get
            {
                if (centerAnchor != null)
                {
                    yield return centerAnchor;
                }

                foreach (TileAnchor anchor in edgeAnchors)
                {
                    yield return anchor;
                }

                foreach (TileAnchor anchor in cornerAnchors)
                {
                    yield return anchor;
                }

                foreach (TileAnchor anchor in innerAnchors)
                {
                    yield return anchor;
                }

                foreach (TileAnchor anchor in propAnchors)
                {
                    yield return anchor;
                }
            }
        }

        public static TileConstructionAnchors CreatePrototypeHexAnchors(float hexOuterRadiusMeters)
        {
            float outerRadius = Mathf.Max(1f, hexOuterRadiusMeters);
            Vector3[] corners = TileHexAnchorMath.GetFlatTopCorners(Vector3.zero, outerRadius);
            Vector3[] edgeMidpoints = TileHexAnchorMath.GetEdgeMidpoints(corners);
            Vector3[] innerPoints = TileHexAnchorMath.GetInnerPoints(Vector3.zero, corners);

            List<TileAnchor> edges = new List<TileAnchor>(6);
            List<TileAnchor> cornerAnchors = new List<TileAnchor>(6);
            List<TileAnchor> inners = new List<TileAnchor>(6);
            List<TileAnchor> props = new List<TileAnchor>(2);

            for (int i = 0; i < 6; i++)
            {
                edges.Add(new TileAnchor($"Content.E{i}", TileAnchorKind.Edge, i, edgeMidpoints[i], true));
                cornerAnchors.Add(new TileAnchor($"Content.V{i}", TileAnchorKind.Corner, i, corners[i], true));
                inners.Add(new TileAnchor($"Content.I{i}", TileAnchorKind.Inner, i, innerPoints[i], true));
            }

            props.Add(new TileAnchor("Content.Prop.Forward", TileAnchorKind.Prop, 0, innerPoints[0] * 0.72f, true));
            props.Add(new TileAnchor("Content.Prop.Right", TileAnchorKind.Prop, 1, innerPoints[1] * 0.55f, true));

            return new TileConstructionAnchors(
                new TileAnchor("Content.Center", TileAnchorKind.Center, -1, Vector3.zero, true),
                edges,
                cornerAnchors,
                inners,
                props);
        }
    }
}
