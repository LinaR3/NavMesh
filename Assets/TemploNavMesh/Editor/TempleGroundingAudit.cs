using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class TempleGroundingAudit
{
    [MenuItem("Tools/NavMesh Tutorial/Audit Ground Contact")]
    public static void Run()
    {
        Physics.SyncTransforms();
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var report = new StringBuilder();
        report.AppendLine($"Temple floor contact audit | scene={scene.name}");
        var surfaces = roots.SelectMany(root => root.GetComponentsInChildren<NavMeshSurface>(true)).ToArray();
        report.AppendLine($"NavMeshSurface count={surfaces.Length}; global triangles={NavMesh.CalculateTriangulation().indices.Length / 3}");
        foreach (var surface in surfaces)
            report.AppendLine($"SURFACE {PathOf(surface.transform)} enabled={surface.enabled} active={surface.gameObject.activeInHierarchy} data={(surface.navMeshData ? surface.navMeshData.name : "NULL")} pos={surface.transform.position:F2}");

        var all = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var candidates = all.Where(t => t.gameObject.activeInHierarchy)
            .Where(IsCandidate).Where(t => !HasMatchingAncestor(t)).ToArray();
        var issues = new List<string>();
        var okCount = 0;
        foreach (var candidate in candidates)
        {
            var renderers = candidate.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            if (renderers.Length == 0) continue;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var selfColliders = candidate.GetComponentsInChildren<Collider>(true);
            var probes = ProbePoints(bounds);
            var gaps = new List<float>();
            var noFloor = 0;
            foreach (var point in probes)
            {
                var origin = new Vector3(point.x, Mathf.Max(bounds.max.y + 0.25f, bounds.min.y + 1f), point.z);
                var hits = Physics.RaycastAll(origin, Vector3.down, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    .Where(hit => hit.collider != null && !selfColliders.Contains(hit.collider) && !IsInside(candidate, hit.collider.transform))
                    .Where(hit => hit.point.y <= bounds.min.y + 0.22f)
                    .OrderByDescending(hit => hit.point.y).ToArray();
                if (hits.Length == 0) { noFloor++; continue; }
                gaps.Add(bounds.min.y - hits[0].point.y);
            }

            var minGap = gaps.Count > 0 ? gaps.Min() : float.NaN;
            var maxGap = gaps.Count > 0 ? gaps.Max() : float.NaN;
            var result = gaps.Count == 0 ? "NO_FLOOR" : maxGap > 0.22f ? "FLOATING" : minGap < -0.35f ? "PENETRATING" : "OK";
            var line = $"{result,-11} {PathOf(candidate)} | pos={candidate.position:F2}, bottom={bounds.min.y:F2}, floorGap={minGap:F2}..{maxGap:F2}m, probes={gaps.Count}/{probes.Count}, size={bounds.size:F2}";
            if (result == "OK") okCount++; else issues.Add(line);
            report.AppendLine(line);
        }

        report.AppendLine($"\nSUMMARY: reviewed={candidates.Length}, okay={okCount}, issues={issues.Count}");
        if (issues.Count > 0)
        {
            report.AppendLine("\nISSUES FIRST:");
            foreach (var issue in issues.Take(60)) report.AppendLine(issue);
        }
        var path = Path.Combine(Path.GetTempPath(), "navmesh-temple-ground-audit.txt");
        File.WriteAllText(path, report.ToString());
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Auditoría de apoyo y NavMesh", $"Revisé {candidates.Length} objetos con render. En contacto: {okCount}. Con alerta: {issues.Count}.\n\nSuperficies NavMesh: {surfaces.Length}. Triángulos activos: {NavMesh.CalculateTriangulation().indices.Length / 3}.\n\nReporte completo: {path}", "OK");
    }

    static bool IsCandidate(Transform t)
    {
        var n = t.name.ToLowerInvariant();
        if (n.StartsWith("golem_") || n == "aventurera_player") return true;
        return n.Contains("wall") || n.Contains("stairs") || n.Contains("stair") || n.Contains("platform") || n.Contains("arch") || n.Contains("railing") || n.Contains("barrier") || n.Contains("pillar") || n.Contains("pilar") || n.Contains("muro") || n.Contains("escalinata") || n.Contains("baldosa") || n.Contains("floor_tile") || n.Contains("piso_") || n.StartsWith("antorcha_") || n.StartsWith("recipiente_");
    }

    static bool HasMatchingAncestor(Transform t)
    {
        for (var parent = t.parent; parent != null; parent = parent.parent)
            if (IsCandidate(parent)) return true;
        return false;
    }

    static bool IsInside(Transform root, Transform child)
    {
        for (var current = child; current != null; current = current.parent)
            if (current == root) return true;
        return false;
    }

    static List<Vector3> ProbePoints(Bounds bounds)
    {
        var dx = Mathf.Min(Mathf.Max(bounds.extents.x * 0.82f, 0.05f), 1.8f);
        var dz = Mathf.Min(Mathf.Max(bounds.extents.z * 0.82f, 0.05f), 1.8f);
        return new List<Vector3>
        {
            bounds.center,
            bounds.center + new Vector3(-dx, 0f, -dz),
            bounds.center + new Vector3(dx, 0f, -dz),
            bounds.center + new Vector3(-dx, 0f, dz),
            bounds.center + new Vector3(dx, 0f, dz)
        };
    }

    static string PathOf(Transform t)
    {
        var parts = new Stack<string>();
        for (var current = t; current != null; current = current.parent) parts.Push(current.name);
        return string.Join("/", parts);
    }
}
