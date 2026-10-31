using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CrashSimulator.Editor
{
    /// <summary>Read-only scene audit. Entry point also supports Unity -executeMethod.</summary>
    public static class UiPhase4Validation
    {
        public static void BuildMainMenuAndAudit()
        {
            PremiumMenuBuilder.Build();
            RunAudit();
        }
        [Serializable] private sealed class Audit
        {
            public string unityVersion;
            public string timestamp;
            public string graphicsDevice;
            public List<string> missingScripts = new();
            public List<string> brokenReferences = new();
            public List<string> cameras = new();
            public List<string> pipelines = new();
            public int behaviourCount;
            public bool passed;
        }

        [MenuItem("Crash Simulator/Validation/Phase 4 Scene Audit")]
        public static void RunAudit()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before the scene audit.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var audit = new Audit { unityVersion = Application.unityVersion,
                timestamp = DateTime.UtcNow.ToString("O"), graphicsDevice = SystemInfo.graphicsDeviceType.ToString() };
            try
            {
                foreach (var path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/TestTrack.unity" })
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        string objectPath = path + ":" + AnimationUtility.CalculateTransformPath(transform, null);
                        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                        if (missing > 0) audit.missingScripts.Add(objectPath + " (" + missing + ")");
                        foreach (var behaviour in transform.GetComponents<MonoBehaviour>())
                        {
                            if (behaviour == null) continue;
                            audit.behaviourCount++;
                            using var serialized = new SerializedObject(behaviour);
                            var property = serialized.GetIterator();
                            while (property.NextVisible(true))
                                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                    property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                                    audit.brokenReferences.Add(objectPath + "/" + behaviour.GetType().Name + "." + property.propertyPath);
                        }
                        var camera = transform.GetComponent<Camera>();
                        if (camera == null) continue;
                        var data = camera.GetComponent<UniversalAdditionalCameraData>();
                        audit.cameras.Add(objectPath + " targetTexture=" + (camera.targetTexture == null ? "none" : camera.targetTexture.name)
                            + " allowMSAA=" + camera.allowMSAA + " depthMode=" + camera.depthTextureMode
                            + " renderType=" + (data == null ? "Base(default)" : data.renderType.ToString())
                            + " stack=" + (data != null && data.renderType == CameraRenderType.Base ? data.cameraStack.Count : 0));
                    }
                }
                foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                    audit.pipelines.Add(asset.name + " depth=" + asset.supportsCameraDepthTexture
                        + " opaque=" + asset.supportsCameraOpaqueTexture + " msaa=" + asset.msaaSampleCount);
                }
                audit.passed = audit.missingScripts.Count == 0 && audit.brokenReferences.Count == 0;
                Directory.CreateDirectory("Documentation/UI/ValidationEvidence");
                File.WriteAllText("Documentation/UI/ValidationEvidence/SceneAudit.json", JsonUtility.ToJson(audit, true));
                Debug.Log("PHASE4_SCENE_AUDIT " + (audit.passed ? "PASSED" : "FAILED") + " missing="
                    + audit.missingScripts.Count + " broken=" + audit.brokenReferences.Count);
            }
            finally
            {
                // Batchmode can start without any loaded scene to restore.
                if (Array.Exists(setup, entry => entry.isLoaded && entry.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            if (!audit.passed) throw new InvalidOperationException("Scene reference audit failed. See SceneAudit.json.");
        }
    }
}
