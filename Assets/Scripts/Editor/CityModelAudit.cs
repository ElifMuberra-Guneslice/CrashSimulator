using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrashSimulator.Editor
{
    /// <summary>Read-only inventory for an imported city model; never adds colliders or edits source assets.</summary>
    public static class CityModelAudit
    {
        [MenuItem("Crash Simulator/City/Inspect Selected City Model")]
        public static void InspectSelected()
        {
            var selected = Selection.activeObject as GameObject;
            string path = selected != null ? AssetDatabase.GetAssetPath(selected) : string.Empty;
            var source = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null)
            {
                EditorUtility.DisplayDialog("City model audit",
                    "Select the imported model root (FBX/OBJ/glTF prefab) in the Project window first.", "OK");
                return;
            }

            GameObject instance = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (instance == null) instance = Object.Instantiate(source);
                instance.name = "__CityAudit_Temporary";
                instance.hideFlags = HideFlags.HideAndDontSave;

                var report = BuildReport(source, path, instance);
                Debug.Log(report, source);
                EditorUtility.DisplayDialog("City model audit complete",
                    "Inventory written to the Console. This audit did not modify the source asset or scene.", "OK");
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
            }
        }

        private static string BuildReport(GameObject source, string path, GameObject instance)
        {
            var report = new StringBuilder(4096);
            var filters = instance.GetComponentsInChildren<MeshFilter>(true);
            var skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            int triangles = 0;
            int vertices = 0;
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null) continue;
                triangles += TriangleCount(filter.sharedMesh);
                vertices += filter.sharedMesh.vertexCount;
            }
            foreach (var renderer in skinned)
            {
                if (renderer.sharedMesh == null) continue;
                triangles += TriangleCount(renderer.sharedMesh);
                vertices += renderer.sharedMesh.vertexCount;
            }

            report.AppendLine("CITY MODEL AUDIT (read-only)");
            report.AppendLine($"Asset: {path}");
            report.AppendLine($"Root: {source.name}; scale {source.transform.localScale}");
            report.AppendLine($"Renderers: {renderers.Length} (MeshFilters {filters.Length}, skinned {skinned.Length})");
            report.AppendLine($"Estimated instantiated mesh data: {triangles:N0} triangles, {vertices:N0} vertices");
            report.AppendLine($"Existing colliders: {colliders.Length}");
            report.AppendLine($"Renderer bounds (Unity units): {CombinedBounds(renderers)}");
            report.AppendLine("Materials / shaders:");

            var seenMaterials = new System.Collections.Generic.HashSet<Material>();
            foreach (var renderer in renderers)
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null || !seenMaterials.Add(material)) continue;
                string shader = material.shader != null ? material.shader.name : "<missing shader>";
                report.AppendLine($"  {material.name}: {shader}{(shader.StartsWith("Universal Render Pipeline/") ? "" : " [review URP conversion]")}");
            }

            report.AppendLine("Hierarchy (first 160 transforms):");
            int listed = 0;
            AppendHierarchy(instance.transform, 0, report, ref listed);
            if (listed >= 160) report.AppendLine("  … hierarchy output capped at 160 transforms");
            report.AppendLine("Next: identify the actual road meshes before adding static collision; do not apply MeshCollider indiscriminately to decorative objects.");
            return report.ToString();
        }

        private static int TriangleCount(Mesh mesh)
        {
            int count = 0;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                if (mesh.GetTopology(submesh) == MeshTopology.Triangles)
                    count += (int)mesh.GetIndexCount(submesh) / 3;
            return count;
        }

        private static string CombinedBounds(Renderer[] renderers)
        {
            if (renderers.Length == 0) return "<no renderers>";
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return $"center {bounds.center}, size {bounds.size}";
        }

        private static void AppendHierarchy(Transform current, int depth, StringBuilder report, ref int listed)
        {
            if (listed >= 160) return;
            report.Append(' ', depth * 2).Append(current.name)
                .Append(" [local ").Append(current.localPosition).Append("]");
            var filter = current.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                report.Append(" mesh=").Append(filter.sharedMesh.name)
                    .Append(" tris=").Append(TriangleCount(filter.sharedMesh).ToString("N0"));
            var collider = current.GetComponent<Collider>();
            if (collider != null) report.Append(" collider=").Append(collider.GetType().Name);
            report.AppendLine();
            listed++;
            for (int i = 0; i < current.childCount && listed < 160; i++)
                AppendHierarchy(current.GetChild(i), depth + 1, report, ref listed);
        }
    }
}
