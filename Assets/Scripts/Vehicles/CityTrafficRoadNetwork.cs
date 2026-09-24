using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Liga trechos EasyRoads em interseções e monta vários loops regionais pela cidade.
/// </summary>
public static class CityTrafficRoadNetwork
{
    const float IntersectionMergeMeters = 15f;
    const float MinLoopLengthMeters = 50f;
    const float MinPointSpacing = 6f;
    const int MaxLoopsTotal = 16;
    const int MaxLoopsPerSector = 4;

    sealed class Edge
    {
        public int Id;
        public int NodeA;
        public int NodeB;
        public List<Vector3> PointsAtoB;
        public Vector3 Centroid;
        public int Sector;
    }

    public struct LoopPath
    {
        public string Label;
        public List<Vector3> Points;
    }

    public static List<LoopPath> BuildRegionalLoopPaths(IReadOnlyList<Transform> roadSegments, out int skippedSegments)
    {
        skippedSegments = 0;
        var edges = new List<Edge>();
        var nodePositions = new List<Vector3>();

        foreach (var road in roadSegments)
        {
            if (!TryBuildEdge(road, nodePositions, edges, ref skippedSegments))
                skippedSegments++;
        }

        if (edges.Count == 0)
            return new List<LoopPath>();

        var cityCenter = ComputeCityCenter(edges);
        foreach (var e in edges)
        {
            e.Sector = ClassifySector(e.Centroid, cityCenter);
        }

        var used = new bool[edges.Count];
        var loops = new List<LoopPath>();
        var loopCenters = new List<Vector3>();

        for (var sector = 0; sector < 4; sector++)
        {
            var added = BuildLoopsInSector(
                SectorLabel(sector),
                sector,
                edges,
                used,
                nodePositions,
                loopCenters,
                loops,
                MaxLoopsPerSector);

            if (loops.Count >= MaxLoopsTotal)
                return loops;
        }

        while (loops.Count < MaxLoopsTotal)
        {
            var startNode = FindSpreadStartNode(edges, used, nodePositions, loopCenters);
            if (startNode < 0)
                break;

            var path = WalkLoop(startNode, edges, used, nodePositions);
            path = SimplifyPath(path);
            if (!AcceptLoop(path, loops))
                break;

            loops.Add(new LoopPath
            {
                Label = "Extra_" + (loops.Count + 1).ToString("00"),
                Points = path,
            });
            loopCenters.Add(Centroid(path));
        }

        return loops;
    }

    public static List<List<Vector3>> BuildCityLoopPaths(IReadOnlyList<Transform> roadSegments, out int skippedSegments)
    {
        var regional = BuildRegionalLoopPaths(roadSegments, out skippedSegments);
        var legacy = new List<List<Vector3>>(regional.Count);
        foreach (var lp in regional)
            legacy.Add(lp.Points);

        return legacy;
    }

    static bool TryBuildEdge(Transform road, List<Vector3> nodePositions, List<Edge> edges, ref int skipped)
    {
        if (road == null)
            return false;

        if (!CityTrafficRouteGenerator.TryGetWorldBounds(road, out var bounds))
            return false;

        if (!CityTrafficZone.ContainsForNetwork(bounds.center))
            return false;

        var points = CityTrafficRouteGenerator.BuildCenterline(road, bounds);
        if (points == null || points.Count < 2)
            return false;

        var nodeA = FindOrCreateNode(nodePositions, points[0]);
        var nodeB = FindOrCreateNode(nodePositions, points[points.Count - 1]);
        if (nodeA == nodeB)
            return false;

        var centroid = Centroid(points);
        edges.Add(new Edge
        {
            Id = edges.Count,
            NodeA = nodeA,
            NodeB = nodeB,
            PointsAtoB = points,
            Centroid = centroid,
        });

        return true;
    }

    static int BuildLoopsInSector(
        string sectorLabel,
        int sector,
        List<Edge> edges,
        bool[] used,
        List<Vector3> nodePositions,
        List<Vector3> loopCenters,
        List<LoopPath> output,
        int maxInSector)
    {
        var created = 0;
        while (created < maxInSector && output.Count < MaxLoopsTotal)
        {
            var startNode = FindSectorStartNode(sector, edges, used, nodePositions, loopCenters);
            if (startNode < 0)
                break;

            var path = WalkLoop(startNode, edges, used, nodePositions, sector);
            path = SimplifyPath(path);
            if (!AcceptLoop(path, output))
                break;

            created++;
            output.Add(new LoopPath
            {
                Label = sectorLabel + "_" + created.ToString("00"),
                Points = path,
            });
            loopCenters.Add(Centroid(path));
        }

        return created;
    }

    static bool AcceptLoop(List<Vector3> path, List<LoopPath> existing)
    {
        if (path == null || path.Count < 6)
            return false;

        if (PathLengthXZ(path) < MinLoopLengthMeters)
            return false;

        return true;
    }

    static int FindSectorStartNode(
        int sector,
        List<Edge> edges,
        bool[] used,
        List<Vector3> nodePositions,
        List<Vector3> loopCenters)
    {
        var bestNode = -1;
        var bestScore = float.NegativeInfinity;

        for (var node = 0; node < nodePositions.Count; node++)
        {
            if (!NodeHasUnusedEdgeInSector(node, sector, edges, used))
                continue;

            var score = ScoreStartNode(nodePositions[node], loopCenters);
            if (score > bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        return bestNode;
    }

    static int FindSpreadStartNode(
        List<Edge> edges,
        bool[] used,
        List<Vector3> nodePositions,
        List<Vector3> loopCenters)
    {
        var bestNode = -1;
        var bestScore = float.NegativeInfinity;

        for (var node = 0; node < nodePositions.Count; node++)
        {
            if (!NodeHasAnyUnusedEdge(node, edges, used))
                continue;

            var score = ScoreStartNode(nodePositions[node], loopCenters);
            if (score > bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        return bestNode;
    }

    static float ScoreStartNode(Vector3 nodePos, List<Vector3> loopCenters)
    {
        if (loopCenters == null || loopCenters.Count == 0)
            return 0f;

        var minDist = float.MaxValue;
        foreach (var c in loopCenters)
        {
            var d = HorizontalDistance(nodePos, c);
            if (d < minDist)
                minDist = d;
        }

        return minDist;
    }

    static bool NodeHasUnusedEdgeInSector(int node, int sector, List<Edge> edges, bool[] used)
    {
        foreach (var e in edges)
        {
            if (used[e.Id] || e.Sector != sector)
                continue;

            if (e.NodeA == node || e.NodeB == node)
                return true;
        }

        return false;
    }

    static bool NodeHasAnyUnusedEdge(int node, List<Edge> edges, bool[] used)
    {
        foreach (var e in edges)
        {
            if (used[e.Id])
                continue;

            if (e.NodeA == node || e.NodeB == node)
                return true;
        }

        return false;
    }

    static List<Vector3> WalkLoop(int startNode, List<Edge> edges, bool[] used, List<Vector3> nodePositions, int sectorFilter = -1)
    {
        var path = new List<Vector3>();
        var current = startNode;
        var origin = startNode;
        var guard = edges.Count + 12;

        while (guard-- > 0)
        {
            if (!TryPickEdge(current, edges, used, sectorFilter, out var edge, out var nextNode))
                break;

            used[edge.Id] = true;
            AppendEdgePoints(path, edge, current);
            current = nextNode;

            if (current == origin && path.Count >= 8)
                break;
        }

        if (path.Count > 0 && current != origin)
        {
            var close = nodePositions[origin];
            if (HorizontalDistance(path[path.Count - 1], close) > 12f)
                path.Add(close);
        }

        return path;
    }

    static bool TryPickEdge(int node, List<Edge> edges, bool[] used, int sectorFilter, out Edge picked, out int nextNode)
    {
        picked = null;
        nextNode = -1;
        var candidates = new List<Edge>();

        foreach (var e in edges)
        {
            if (used[e.Id])
                continue;

            if (sectorFilter >= 0 && e.Sector != sectorFilter)
                continue;

            if (e.NodeA == node || e.NodeB == node)
                candidates.Add(e);
        }

        if (candidates.Count == 0)
            return false;

        var bestScore = -1;
        var best = new List<Edge>();
        foreach (var e in candidates)
        {
            var next = e.NodeA == node ? e.NodeB : e.NodeA;
            var score = CountUnusedEdgesAt(next, edges, used, sectorFilter);
            if (score > bestScore)
            {
                bestScore = score;
                best.Clear();
                best.Add(e);
            }
            else if (score == bestScore)
            {
                best.Add(e);
            }
        }

        picked = best[Random.Range(0, best.Count)];
        nextNode = picked.NodeA == node ? picked.NodeB : picked.NodeA;
        return true;
    }

    static int CountUnusedEdgesAt(int node, List<Edge> edges, bool[] used, int sectorFilter)
    {
        var count = 0;
        foreach (var e in edges)
        {
            if (used[e.Id])
                continue;

            if (sectorFilter >= 0 && e.Sector != sectorFilter)
                continue;

            if (e.NodeA == node || e.NodeB == node)
                count++;
        }

        return count;
    }

    static Vector3 ComputeCityCenter(List<Edge> edges)
    {
        if (CityTrafficZone.TryGetCompositeCenter(out var anchorCenter))
            return anchorCenter;

        var sum = Vector3.zero;
        foreach (var e in edges)
            sum += e.Centroid;

        return sum / Mathf.Max(1, edges.Count);
    }

    static int ClassifySector(Vector3 point, Vector3 center)
    {
        var dx = point.x - center.x;
        var dz = point.z - center.z;
        if (Mathf.Abs(dx) >= Mathf.Abs(dz))
            return dx >= 0f ? 0 : 1;

        return dz >= 0f ? 2 : 3;
    }

    static string SectorLabel(int sector)
    {
        switch (sector)
        {
            case 0: return "Leste";
            case 1: return "Oeste";
            case 2: return "Norte";
            default: return "Sul";
        }
    }

    static Vector3 Centroid(List<Vector3> points)
    {
        if (points == null || points.Count == 0)
            return Vector3.zero;

        var sum = Vector3.zero;
        foreach (var p in points)
            sum += p;

        return sum / points.Count;
    }

    static void AppendEdgePoints(List<Vector3> path, Edge edge, int fromNode)
    {
        var forward = fromNode == edge.NodeA;
        var pts = edge.PointsAtoB;
        if (pts == null || pts.Count == 0)
            return;

        if (forward)
        {
            for (var i = 0; i < pts.Count; i++)
                AppendPoint(path, pts[i]);
        }
        else
        {
            for (var i = pts.Count - 1; i >= 0; i--)
                AppendPoint(path, pts[i]);
        }
    }

    static void AppendPoint(List<Vector3> path, Vector3 p)
    {
        if (path.Count == 0)
        {
            path.Add(p);
            return;
        }

        if (HorizontalDistance(path[path.Count - 1], p) >= 1.5f)
            path.Add(p);
    }

    static List<Vector3> SimplifyPath(List<Vector3> path)
    {
        if (path == null || path.Count <= 2)
            return path;

        var simplified = new List<Vector3> { path[0] };
        for (var i = 1; i < path.Count; i++)
        {
            if (HorizontalDistance(simplified[simplified.Count - 1], path[i]) >= MinPointSpacing)
                simplified.Add(path[i]);
        }

        if (simplified.Count < 2)
            simplified.Add(path[path.Count - 1]);

        return simplified;
    }

    static float PathLengthXZ(List<Vector3> path)
    {
        if (path == null || path.Count < 2)
            return 0f;

        var len = 0f;
        for (var i = 1; i < path.Count; i++)
            len += HorizontalDistance(path[i - 1], path[i]);

        return len;
    }

    static int FindOrCreateNode(List<Vector3> nodes, Vector3 position)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            if (HorizontalDistance(nodes[i], position) <= IntersectionMergeMeters)
                return i;
        }

        nodes.Add(position);
        return nodes.Count - 1;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var dx = a.x - b.x;
        var dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
