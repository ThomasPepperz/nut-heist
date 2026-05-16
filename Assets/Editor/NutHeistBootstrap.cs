#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;
using NutHeist.Audio;
using NutHeist.CameraRig;
using NutHeist.Core;
using NutHeist.Environment;
using NutHeist.Pickups;
using NutHeist.Player;
using NutHeist.Progress;
using NutHeist.UI;
using NutHeist.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace NutHeist.EditorTools
{
    /// <summary>One-shot scene + prefab author for local Unity Hub workflows.</summary>
    public static class NutHeistBootstrap
    {
        const string SceneAssetPath = "Assets/Scenes/MainLevel.unity";
        const string NutPrefabPath = "Assets/Prefabs/NutCollectible.prefab";
        const string SquirrelPrefabPath = "Assets/Prefabs/Squirrel.prefab";

        [MenuItem("Nut Heist/Full Project Setup", priority = 0)]
        public static void PerformFullBootstrap() => PerformFullBootstrapInternal(showDialog: true);

        /// <summary>Batchmode entry point: Nut Heist bootstrap without UI dialogs.</summary>
        public static void PerformFullBootstrapBatch() => PerformFullBootstrapInternal(showDialog: false);

        static void PerformFullBootstrapInternal(bool showDialog)
        {
            EnsureFolders();
            EnsureGameplayTags();

            PrepareSceneAsset();
            GameObject sunlight = EnsureDirectionalSun();
            BlockoutYard();
            GameObject squirrel = BuildSquirrel(out Transform pivot);
            BuildNutPickupPrefabAsset();
            BuildSharedServices();
            BuildHudOverlay();
            CinemachineComposer.BootstrapCameras(pivot, squirrel.GetComponent<SquirrelController>());
            EnsureWorldSpawner();
            AuthorTreeSample(Vector3.Scale(new Vector3(-12f, 1f, 12f), new Vector3(3f, 1f, 3f))); // quadrant sample

            PrefabUtility.SaveAsPrefabAsset(squirrel, SquirrelPrefabPath);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            UpsertSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Nut Heist",
                    "Bootstrap complete. Open Graphics settings if magenta materials appear.", "OK");
            }
            else
            {
                Debug.Log("Nut Heist bootstrap complete.");
            }
        }

        static void EnsureFolders()
        {
            CreateFolderRecursive("Assets/Scenes");
            CreateFolderRecursive("Assets/Prefabs");
        }

        static void CreateFolderRecursive(string unixPath)
        {
            unixPath = unixPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(unixPath))
            {
                return;
            }

            string parentPath = Path.GetDirectoryName(unixPath)?.Replace('\\', '/');
            string leafFolder = Path.GetFileName(unixPath);
            if (!string.IsNullOrEmpty(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
            {
                CreateFolderRecursive(parentPath);
            }

            AssetDatabase.CreateFolder(parentPath ?? "Assets", leafFolder ?? unixPath);
        }

        static void EnsureGameplayTags()
        {
            var tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            SerializedObject mgr = new SerializedObject(tagAssets[0]);
            SerializedProperty tagsProp = mgr.FindProperty("tags");
            foreach (string tagWord in new[]
                     {
                         GameplayTags.Player, GameplayTags.Climbable, GameplayTags.Nut, GameplayTags.Hazard,
                         GameplayTags.MovingPlatformTag
                     })
            {
                if (!TagListContains(tagsProp, tagWord))
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagWord;
                }
            }

            mgr.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool TagListContains(SerializedProperty tagsSerialized, string needle)
        {
            for (int i = 0; i < tagsSerialized.arraySize; ++i)
            {
                if (tagsSerialized.GetArrayElementAtIndex(i).stringValue == needle)
                {
                    return true;
                }
            }

            return false;
        }

        static void PrepareSceneAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneAssetPath) != null)
            {
                EditorSceneManager.OpenScene(SceneAssetPath, OpenSceneMode.Single);
                return;
            }

            Scene created = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(created, SceneAssetPath);
            EditorSceneManager.OpenScene(SceneAssetPath, OpenSceneMode.Single);
        }

        static GameObject EnsureDirectionalSun()
        {
            foreach (Light l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude,
                         FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional)
                {
                    return l.gameObject;
                }
            }

            GameObject sun = new GameObject("Sun_Directional");
            Light lite = sun.AddComponent<Light>();
            lite.type = LightType.Directional;
            lite.intensity = 1f;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            Undo.RegisterCreatedObjectUndo(sun, "Sun bootstrap");
            return sun;
        }

        static void BlockoutYard()
        {
            if (GameObject.Find("Terrain_YardPlate"))
            {
                return;
            }

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Terrain_YardPlate";
            plate.transform.localScale = new Vector3(120f, 0.3f, 120f);
            plate.transform.position = new Vector3(0f, -0.2f, 0f);

            Undo.RegisterCreatedObjectUndo(plate, "YardPlate");

            GameObject houseProto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            houseProto.name = "House_Mass_Box";
            houseProto.transform.localScale = new Vector3(24f, 12f, 18f);
            houseProto.transform.position = new Vector3(8f, 6f, 4f);
            Undo.RegisterCreatedObjectUndo(houseProto, "HouseBox");
        }

        static GameObject BuildSquirrel(out Transform pivot)
        {
            GameObject squirrelBody = GameObject.Find("Squirrel_Player") ?? new GameObject("Squirrel_Player");
            squirrelBody.tag = GameplayTags.Player;
            squirrelBody.transform.SetPositionAndRotation(new Vector3(0f, 0.42f, 0f), Quaternion.identity);

            foreach (MonoBehaviour leftover in squirrelBody.GetComponents<MonoBehaviour>())
            {
                if (leftover is SquirrelController || leftover is ClimbingSystem || leftover is SquirrelInput ||
                    leftover is SquirrelAnimator || leftover is CameraFollow)
                {
                    continue;
                }

                Object.DestroyImmediate(leftover);
            }

            squirrelBody.EnsureCharacterControllerSizing();
            Rigidbody looseBody = squirrelBody.GetComponent<Rigidbody>();
            if (looseBody != null)
            {
                Object.DestroyImmediate(looseBody);
            }

            squirrelBody.EnsureComponent<SquirrelInput>();
            squirrelBody.EnsureComponent<ClimbingSystem>();
            squirrelBody.EnsureComponent<SquirrelController>();
            squirrelBody.EnsureComponent<SquirrelAnimator>();

            Transform capsuleChild = squirrelBody.transform.Find("VisualCapsule_Proxy");
            if (!capsuleChild)
            {
                GameObject viz = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                viz.name = "VisualCapsule_Proxy";
                viz.transform.SetParent(squirrelBody.transform, false);
                capsuleChild = viz.transform;
                Object.DestroyImmediate(viz.GetComponent<Collider>());
                capsuleChild.localScale = new Vector3(0.5f, 0.62f, 0.52f);
            }

            PivotChildFactory.AttachPickupSensor(squirrelBody.transform);

            pivot = squirrelBody.transform.Find("Camera_Target");
            if (!pivot)
            {
                GameObject pivotGO = new GameObject("Camera_Target");
                pivot = pivotGO.transform;
                pivot.SetParent(squirrelBody.transform, false);
                pivot.localPosition = new Vector3(0f, ScaleManager.SQUIRREL_HEIGHT * 0.55f,
                    ScaleManager.SQUIRREL_HEIGHT * 0.25f);
            }

            Selection.activeGameObject = squirrelBody;
            Undo.RegisterFullObjectHierarchyUndo(squirrelBody, "Squirrel");
            return squirrelBody;
        }

        static void BuildNutPickupPrefabAsset()
        {
            GameObject nutDraft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nutDraft.name = "NutCollectible_Runtime";
            nutDraft.tag = GameplayTags.Nut;
            SphereCollider nutCollider = nutDraft.GetComponent<SphereCollider>();
            nutCollider.radius = 0.12f;
            nutCollider.isTrigger = true;
            nutDraft.transform.localScale = Vector3.one * 0.2f;
            nutDraft.EnsureComponent<NutPickup>();

            PrefabUtility.SaveAsPrefabAsset(nutDraft, NutPrefabPath);
            Object.DestroyImmediate(nutDraft);
        }

        static void BuildSharedServices()
        {
            if (!UnityEngine.Object.FindFirstObjectByType<NutProgress>())
            {
                new GameObject("NutProgress_ServiceHost").EnsureComponent<NutProgress>();
            }

            if (!UnityEngine.Object.FindFirstObjectByType<VentSystem>())
            {
                new GameObject("VentSystem_ServiceHost").EnsureComponent<VentSystem>();
            }

            if (!UnityEngine.Object.FindFirstObjectByType<SoundManager>())
            {
                GameObject sfx = new GameObject("NutHeist_SoundManager_Service");
                sfx.EnsureComponent<SoundManager>();
            }
        }

        static void BuildHudOverlay()
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (!canvas || canvas.gameObject.name != "NutHeistHudRoot")
            {
                if (!UnityEngine.Object.FindFirstObjectByType<EventSystem>())
                {
                    GameObject evt = new GameObject("EventSystem");
                    evt.EnsureComponent<EventSystem>();
#pragma warning disable 618
                    evt.EnsureComponent<StandaloneInputModule>();
#pragma warning restore 618
                }

                GameObject hudCanvas = new GameObject("NutHeistHudRoot");
                canvas = hudCanvas.EnsureComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                UnityEngine.UI.CanvasScaler canvasScalerAttached = hudCanvas.EnsureComponent<UnityEngine.UI.CanvasScaler>();
                canvasScalerAttached.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScalerAttached.referenceResolution = new Vector2(1920f, 1080f);

                hudCanvas.EnsureComponent<UnityEngine.UI.GraphicRaycaster>();
                HudTextFabric.SpawnPair(canvas.transform, out UnityEngine.UI.Text nuts, out UnityEngine.UI.Text days);
                days.text = "Day 1";
                nuts.text = "0";

                NutHudView binder = hudCanvas.EnsureComponent<NutHudView>();
                SerializedObject so = new SerializedObject(binder);
                so.FindProperty("nutCounterText").objectReferenceValue = nuts;
                so.FindProperty("dayLabelText").objectReferenceValue = days;
                so.ApplyModifiedPropertiesWithoutUndo();

                Undo.RegisterCreatedObjectUndo(hudCanvas, "HUD");
            }
        }

        static void EnsureWorldSpawner()
        {
            WorldSpawner spawner = UnityEngine.Object.FindFirstObjectByType<WorldSpawner>();
            if (!spawner)
            {
                spawner = new GameObject("NutHeistWorldSpawner").EnsureComponent<WorldSpawner>();
            }

            SerializedObject sos = new SerializedObject(spawner);
            GameObject nutPrefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(NutPrefabPath);
            NutPickup nutPickup = nutPrefabRoot ? nutPrefabRoot.GetComponent<NutPickup>() : null;
            sos.FindProperty("nutPrefabPrototype").objectReferenceValue = nutPickup;
            sos.FindProperty("nutsToScatter").intValue = 40;
            sos.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(spawner.gameObject, "WorldSpawner");
        }

        static void AuthorTreeSample(Vector3 position)
        {
            if (GameObject.Find("Oak_BlockoutCylinder"))
            {
                return;
            }

            GameObject tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tree.name = "Oak_BlockoutCylinder";
            tree.transform.position = position + Vector3.up * 12f;
            tree.transform.localScale = new Vector3(4f, ScaleManager.ToUnits(12f), 4f);
            tree.tag = GameplayTags.Climbable;
            tree.EnsureComponent<ClimbableSurface>();
            Undo.RegisterCreatedObjectUndo(tree, "Tree");
        }

        static void UpsertSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> list = EditorBuildSettings.scenes.ToList();
            if (!list.Exists(x => x.path == SceneAssetPath))
            {
                list.Insert(0, new EditorBuildSettingsScene(SceneAssetPath, true));
                EditorBuildSettings.scenes = list.ToArray();
            }
        }

        static class PivotChildFactory
        {
            public static void AttachPickupSensor(Transform squirrelRootTransform)
            {
                Transform detector = squirrelRootTransform.Find("PickupColliderChild");
                if (!detector)
                {
                    GameObject child = new GameObject("PickupColliderChild");
                    detector = child.transform;
                    detector.SetParent(squirrelRootTransform, false);
                    detector.localPosition = Vector3.up * ScaleManager.ToUnits(0.12f);
                }

                SphereCollider sphericalDetector = detector.gameObject.EnsureComponent<SphereCollider>();
                sphericalDetector.radius = Mathf.Max(sphericalDetector.radius, 0.22f);
                sphericalDetector.isTrigger = true;
                detector.gameObject.EnsureComponent<Rigidbody>().isKinematic = true;
                detector.gameObject.EnsureComponent<PickupInteractor>();
            }
        }

        static class HudTextFabric
        {
            public static void SpawnPair(Transform parent, out UnityEngine.UI.Text nuts, out UnityEngine.UI.Text days)
            {
                nuts = HudTextFabric.Builder(parent, "NutHudCount", AnchorCorner.TopLeft, new Vector2(48f, -48f))
                    .GetComponent<UnityEngine.UI.Text>();
                days = HudTextFabric.Builder(parent, "DayHudLabel", AnchorCorner.TopRight, new Vector2(-48f, -48f))
                    .GetComponent<UnityEngine.UI.Text>();
            }

            enum AnchorCorner { TopLeft, TopRight }

            static GameObject Builder(Transform parent, string nm, AnchorCorner corner, Vector2 anchored)
            {
                GameObject txtObj = new GameObject(nm);
                txtObj.transform.SetParent(parent, false);

                UnityEngine.UI.Text label = txtObj.AddComponent<UnityEngine.UI.Text>();
                RectTransform rect = label.rectTransform;

                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 28;
                label.color = Color.white;
                label.alignment = TextAnchor.UpperLeft;
                rect.sizeDelta = new Vector2(320f, 80f);

                switch (corner)
                {
                    case AnchorCorner.TopLeft:
                        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                        rect.pivot = new Vector2(0f, 1f);
                        break;

                    default:
                        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                        rect.pivot = new Vector2(1f, 1f);
                        break;
                }

                rect.anchoredPosition = anchored;
                return txtObj;
            }
        }

        static class CinemachineComposer
        {
            const string CinemachineRootName = "NutHeist_CinemachineRig";

            public static void BootstrapCameras(Transform cameraPivotAnchor, SquirrelController squirrelController)
            {
                Camera mainLens = Camera.main;
                if (mainLens != null && !mainLens.TryGetComponent(out CinemachineBrain _))
                {
                    mainLens.gameObject.AddComponent<CinemachineBrain>();
                    mainLens.transform.position = new Vector3(0f, ScaleManager.ToUnits(1.45f),
                        -ScaleManager.ToUnits(2.05f));

                    if (!mainLens.TryGetComponent(out AudioListener _))
                    {
                        mainLens.gameObject.AddComponent<AudioListener>();
                    }
                }

                GameObject rigGo = GameObject.Find(CinemachineRootName);
                if (!rigGo)
                {
                    rigGo = new GameObject(CinemachineRootName);
                    Undo.RegisterCreatedObjectUndo(rigGo, "Cinemachine rig");
                }

                Transform rig = rigGo.transform;

                GameObject roam = CinemachineComposer.FindOrAttachChild(rig, "VCam_DefaultRoam");
                GameObject climb = CinemachineComposer.FindOrAttachChild(rig, "VCam_Climbing");

                CinemachineVirtualCamera roamCamComp = roam.EnsureComponent<CinemachineVirtualCamera>();
                CinemachineVirtualCamera climbCamComp = climb.EnsureComponent<CinemachineVirtualCamera>();

                roamCamComp.Follow = cameraPivotAnchor;
                roamCamComp.LookAt = cameraPivotAnchor;
                roamCamComp.m_Lens.FieldOfView = 65f;
                roamCamComp.Priority = 20;

                climbCamComp.Follow = cameraPivotAnchor;
                climbCamComp.LookAt = cameraPivotAnchor;
                climbCamComp.m_Lens.FieldOfView = 70f;
                climbCamComp.Priority = 10;

                CamFollowLinker.EnsureLinkage(squirrelController, roamCamComp, climbCamComp, cameraPivotAnchor);

                CinemachineCollider roamCollision = roam.EnsureComponent<CinemachineCollider>();
                CinemachineCollider climbCollision = climb.EnsureComponent<CinemachineCollider>();
                _ = roamCollision;
                _ = climbCollision;
            }

            static GameObject FindOrAttachChild(Transform rig, string childName)
            {
                Transform nested = rig.Find(childName);
                if (nested)
                {
                    return nested.gameObject;
                }

                GameObject match = GameObject.Find(childName);
                if (!match)
                {
                    GameObject child = new GameObject(childName);
                    child.transform.SetParent(rig, false);
                    Undo.RegisterCreatedObjectUndo(child, "NutHeist VCams");
                    return child;
                }

                if (!match.transform.IsChildOf(rig))
                {
                    Undo.SetTransformParent(match.transform, rig, "NutHeist VCams parent");
                    match.transform.localPosition = Vector3.zero;
                    match.transform.localRotation = Quaternion.identity;
                }

                return match;
            }
        }

        static class CamFollowLinker
        {
            public static void EnsureLinkage(SquirrelController squirrelController,
                CinemachineVirtualCamera roam, CinemachineVirtualCamera climb,
                Transform cameraPivotAnchor)
            {
                CameraFollow followBrain = squirrelController.gameObject.EnsureComponent<CameraFollow>();

                SerializedObject linkage = new SerializedObject(followBrain);
                linkage.FindProperty("squirrel").objectReferenceValue = squirrelController;
                linkage.FindProperty("cameraTarget").objectReferenceValue = cameraPivotAnchor;
                linkage.FindProperty("roamCamera").objectReferenceValue = roam;
                linkage.FindProperty("climbCamera").objectReferenceValue = climb;
                linkage.ApplyModifiedPropertiesWithoutUndo();

            }
        }
    }

    static class EditorGameObjectGlue
    {
        public static T EnsureComponent<T>(this GameObject go) where T : Component
        {
            return go.TryGetComponent(out T comp) ? comp : go.AddComponent<T>();
        }

        public static CharacterController EnsureCharacterControllerSizing(this GameObject squirrelRoot)
        {
            CharacterController ccInstance = squirrelRoot.EnsureComponent<CharacterController>();
            ccInstance.skinWidth = 0.01f;
            ccInstance.radius = Mathf.Max(ccInstance.radius, 0.08f);
            ccInstance.height = Mathf.Max(ccInstance.height, ScaleManager.SQUIRREL_HEIGHT);
            ccInstance.center = new Vector3(0f, ccInstance.height * 0.52f + 0.025f, 0f);
            return ccInstance;
        }
    }
}
#endif
