using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhateverDevs.Core.Editor.Common;

namespace WhateverDevs.EnvironmentCollidersGenerator.Editor
{
    /// <summary>
    /// Editor window to create a colliders scene from an environment scene.
    /// </summary>
    public class EnvironmentCollidersGeneratorWindow : LoggableEditorWindow<EnvironmentCollidersGeneratorWindow>
    {
        /// <summary>
        /// Reference to the scene where the environment is.
        /// </summary>
        private SceneAsset environmentScene;

        /// <summary>
        /// Reference to the scene where the colliders are.
        /// </summary>
        private SceneAsset collidersScene;

        /// <summary>
        /// Has the consistency check been made?
        /// </summary>
        private bool consistencyCheck;

        /// <summary>
        /// Is the scene consistent?
        /// </summary>
        private bool consistent;

        /// <summary>
        /// Reference to the library.
        /// </summary>
        private EnvironmentCollidersGeneratorLibrary library;

        /// <summary>
        /// Path to the data folder.
        /// </summary>
        private const string DataPath = "Assets/Data/";

        /// <summary>
        /// Path to the scene management folder.
        /// </summary>
        private const string SceneManagementPath = DataPath + "SceneManagement/";

        /// <summary>
        /// Path to the editor folder inside the scene management folder.
        /// </summary>
        private const string EditorFolderPath = SceneManagementPath + "Editor/";

        /// <summary>
        /// Path to the library.
        /// </summary>
        private const string LibraryPath = EditorFolderPath + "EnvironmentCollidersGeneratorLibrary.asset";

        /// <summary>
        /// Renderers that will be ignored during the collider generation.
        /// </summary>
        private List<Renderer> renderersToIgnore;

        /// <summary>
        /// List of meshes to be used on collider generation.
        /// </summary>
        private List<Mesh> meshes = new List<Mesh>();

        /// <summary>
        /// The position of those meshes.
        /// </summary>
        private List<Vector3> positions = new List<Vector3>();

        /// <summary>
        /// The rotations of those meshes.
        /// </summary>
        private List<Quaternion> rotations = new List<Quaternion>();

        /// <summary>
        /// The scales of those meshes.
        /// </summary>
        private List<Vector3> scales = new List<Vector3>();

        /// <summary>
        /// The tags of those meshes.
        /// </summary>
        private List<string> tags = new List<string>();

        /// <summary>
        /// The layers of those meshes.
        /// </summary>
        private List<int> layers = new List<int>();

        /// <summary>
        /// Terrain data to create colliders from.
        /// </summary>
        private List<TerrainData> terrainData = new List<TerrainData>();

        /// <summary>
        /// The positions of those terrains.
        /// </summary>
        private List<Vector3> terrainPositions = new List<Vector3>();

        /// <summary>
        /// The rotations of those terrains.
        /// </summary>
        private List<Quaternion> terrainRotations = new List<Quaternion>();

        /// <summary>
        /// The scales of those terrains.
        /// </summary>
        private List<Vector3> terrainScales = new List<Vector3>();

        /// <summary>
        /// The tags of those terrains.
        /// </summary>
        private List<string> terrainTags = new List<string>();

        /// <summary>
        /// The layers of those terrains.
        /// </summary>
        private List<int> terrainLayers = new List<int>();

        /// <summary>
        /// Display the window.
        /// </summary>
        [MenuItem("WhateverDevs/Scene Management/Environment Colliders Generator")]
        private static void ShowWindow()
        {
            EnvironmentCollidersGeneratorWindow window = GetWindow<EnvironmentCollidersGeneratorWindow>();
            window.titleContent = new GUIContent("Environment Colliders Generator");
            window.Show();
        }

        /// <summary>
        /// Display the UI.
        /// </summary>
        private void OnGUI()
        {
            if (!LibraryUi()) return;

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            environmentScene =
                (SceneAsset) EditorGUILayout.ObjectField("Environment scene",
                                                         environmentScene,
                                                         typeof(SceneAsset),
                                                         false);

            collidersScene =
                (SceneAsset) EditorGUILayout.ObjectField("Colliders scene",
                                                         collidersScene,
                                                         typeof(SceneAsset),
                                                         false);

            if (environmentScene == null) return;

            CheckConsistencyUi();

            if (consistencyCheck)
            {
                if (consistent)
                    EditorGUILayout.HelpBox("The environment scene is consistent.", MessageType.Info);
                else
                    EditorGUILayout
                       .HelpBox("There are consistency errors in the environment scene, check the console.",
                                MessageType.Error);
            }

            if (!consistencyCheck || !consistent || collidersScene == null) return;

            CreateCollidersSceneUi();
        }

        /// <summary>
        /// Display UI related to the library.
        /// </summary>
        /// <returns></returns>
        private bool LibraryUi()
        {
            CreateOrLoadLibrary();

            if (library == null)
            {
                EditorGUILayout.HelpBox("Library could not be loaded!", MessageType.Error);
                return false;
            }

            if (!GUILayout.Button("Open library")) return true;

            Selection.activeObject = library;
            EditorGUIUtility.PingObject(library);

            return true;
        }

        /// <summary>
        /// Creates or loads the library.
        /// </summary>
        private void CreateOrLoadLibrary()
        {
            if (library != null) return;

            if (File.Exists(LibraryPath))
                library = AssetDatabase.LoadAssetAtPath<EnvironmentCollidersGeneratorLibrary>(LibraryPath);
            else
            {
                if (!Directory.Exists(DataPath)) Directory.CreateDirectory(DataPath);

                if (!Directory.Exists(SceneManagementPath)) Directory.CreateDirectory(SceneManagementPath);

                if (!Directory.Exists(EditorFolderPath)) Directory.CreateDirectory(EditorFolderPath);

                library = CreateInstance<EnvironmentCollidersGeneratorLibrary>();

                AssetDatabase.CreateAsset(library, LibraryPath);

                AssetDatabase.SaveAssets();

                AssetDatabase.Refresh();

                library = AssetDatabase.LoadAssetAtPath<EnvironmentCollidersGeneratorLibrary>(LibraryPath);
            }
        }

        /// <summary>
        /// Check the consistency of the environment scene.
        /// </summary>
        private void CheckConsistencyUi()
        {
            if (!GUILayout.Button("Check scene consistency")) return;

            try
            {
                consistent = true;

                EditorUtility.DisplayProgressBar("Checking consistency", "Loading environment scene...", 0f);

                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(environmentScene));

                EditorUtility.DisplayProgressBar("Checking consistency", "Finding renderers...", .1f);

                MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();

                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Checking consistency",
                                                     meshRenderers[i].name,
                                                     (float) i / meshRenderers.Length);

                    MeshFilter filter = meshRenderers[i].GetComponent<MeshFilter>();

                    if (filter == null)
                    {
                        Logger.Error("Renderer "
                                   + meshRenderers[i].name
                                   + " is missing a mesh filter.");

                        consistent = false;
                        continue;
                    }

                    Mesh mesh = filter.sharedMesh;

                    if (mesh != null) continue;

                    // TODO: Uncomment if we use probuilder at some point.
                    /*if (filter.GetComponent<ProBuilderMesh>() != null)
                            {
                                Logger.Info("Renderer "
                                        + meshRenderers[i].name
                                        + " is ignored as it uses probuilder.");
                                continue;
                            }*/

                    Logger.Error("Renderer "
                               + meshRenderers[i].name
                               + " is missing a mesh.");

                    consistent = false;
                }

                SkinnedMeshRenderer[] skinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>();

                for (int i = 0; i < skinnedMeshRenderers.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Checking consistency",
                                                     skinnedMeshRenderers[i].name,
                                                     (float) i / skinnedMeshRenderers.Length);

                    if (skinnedMeshRenderers[i].sharedMesh != null) continue;

                    Logger.Error("Renderer " + skinnedMeshRenderers[i].name + " is missing a mesh!");
                    consistent = false;
                }

                consistencyCheck = true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Create the colliders scene.
        /// </summary>
        private void CreateCollidersSceneUi()
        {
            if (!GUILayout.Button("Create new colliders")) return;

            try
            {
                EditorUtility.DisplayProgressBar("Generating colliders", "Loading environment scene...", 0f);

                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(environmentScene));

                renderersToIgnore = new List<Renderer>();

                EditorUtility.DisplayProgressBar("Generating colliders", "Finding lods...", .05f);

                LODGroup[] lodGroups = FindObjectsOfType<LODGroup>();

                foreach (LODGroup lodGroup in lodGroups)
                {
                    LOD[] lods = lodGroup.GetLODs();

                    // We want to ignore all lods but the last.
                    for (int i = 0; i < lods.Length - 1; ++i) renderersToIgnore.AddRange(lods[i].renderers);
                }

                Logger.Info("Ignoring " + renderersToIgnore.Count + " renderers as they are in detail lods.");

                EditorUtility.DisplayProgressBar("Generating colliders", "Finding renderers...", .1f);

                MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();
                SkinnedMeshRenderer[] skinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>();
                Terrain[] terrains = FindObjectsOfType<Terrain>();

                Logger.Info("Found " + meshRenderers.Length + " meshes on the environment.");
                Logger.Info("Found " + skinnedMeshRenderers.Length + " skinned meshes on the environment.");
                Logger.Info("Found " + terrains.Length + " terrains on the environment.");

                meshes = new List<Mesh>();
                positions = new List<Vector3>();
                rotations = new List<Quaternion>();
                scales = new List<Vector3>();

                tags = new List<string>();
                layers = new List<int>();

                terrainData = new List<TerrainData>();
                terrainPositions = new List<Vector3>();
                terrainRotations = new List<Quaternion>();
                terrainScales = new List<Vector3>();

                terrainTags = new List<string>();
                terrainLayers = new List<int>();

                RegisterMeshRenderers(meshRenderers);

                RegisterSkinnedMeshRenderers(skinnedMeshRenderers);

                RegisterTerrains(terrains);
                
                EditorSceneManager.SaveOpenScenes();

                EditorUtility.DisplayProgressBar("Generating colliders", "Loading colliders scene...", .2f);

                Scene scene =
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(collidersScene), OpenSceneMode.Additive);

                // ReSharper disable once AccessToStaticMemberViaDerivedType
                EditorSceneManager.SetActiveScene(scene);

                EditorUtility.DisplayProgressBar("Generating colliders", "Removing old colliders...", .4f);

                GameObject collidersParent = GameObject.Find("Colliders");

                if (collidersParent != null) DestroyImmediate(collidersParent);

                collidersParent = new GameObject("Colliders");

                EditorUtility.DisplayProgressBar("Generating colliders", "Generating new colliders...", .6f);

                GenerateMeshColliders(collidersParent);
                GenerateTerrainColliders(collidersParent);

                EditorUtility.DisplayProgressBar("Generating colliders", "Saving scene...", .8f);

                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Register the mesh renderers to generate colliders from.
        /// </summary>
        /// <param name="meshRenderers"></param>
        private void RegisterMeshRenderers(IReadOnlyList<MeshRenderer> meshRenderers)
        {
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                if (renderersToIgnore.Contains(meshRenderers[i])) continue;

                // TODO: Uncomment if we use probuilder at some point.
                /*if (meshRenderers[i].GetComponent<ProBuilderMesh>() != null)
                {
                    Logger.Info("Renderer "
                            + meshRenderers[i].name
                            + " is ignored as it uses probuilder.");
                    continue;
                }*/

                Logger.Info("Registering mesh "
                          + meshRenderers[i].name
                          + " for collider creation.");

                meshes.Add(meshRenderers[i].GetComponent<MeshFilter>().sharedMesh);
                Transform transform = meshRenderers[i].transform;
                positions.Add(transform.position);
                rotations.Add(transform.rotation);
                scales.Add(transform.lossyScale);
                tags.Add(transform.tag);
                layers.Add(transform.gameObject.layer);
            }
        }

        /// <summary>
        /// Register the skinned mesh renderers to create colliders from.
        /// </summary>
        /// <param name="skinnedMeshRenderers"></param>
        private void RegisterSkinnedMeshRenderers(IReadOnlyList<SkinnedMeshRenderer> skinnedMeshRenderers)
        {
            for (int i = 0; i < skinnedMeshRenderers.Count; i++)
            {
                if (renderersToIgnore.Contains(skinnedMeshRenderers[i])) continue;

                Logger.Info("Registering skinned mesh "
                          + skinnedMeshRenderers[i].name
                          + " for collider creation.");

                meshes.Add(skinnedMeshRenderers[i].sharedMesh);
                Transform transform = skinnedMeshRenderers[i].transform;
                positions.Add(transform.position);
                rotations.Add(transform.rotation);
                scales.Add(transform.lossyScale);
                tags.Add(transform.tag);
                layers.Add(transform.gameObject.layer);
            }
        }

        /// <summary>
        /// Register the terrains to generate colliders from.
        /// </summary>
        /// <param name="terrains"></param>
        private void RegisterTerrains(IReadOnlyList<Terrain> terrains)
        {
            for (int i = 0; i < terrains.Count; i++)
            {
                TerrainCollider collider = terrains[i].GetComponent<TerrainCollider>();

                if (collider == null)
                {
                    Logger.Error("Ignoring terrain "
                               + terrains[i].name
                               + " as it doesn't have a terrain collider.");

                    continue;
                }

                if (collider.enabled)
                {
                    Logger.Warn("Terrain collider on the environment scene should be disabled. Disabling it now.");
                    collider.enabled = false;
                }

                Logger.Info("Registering terrain "
                          + terrains[i].name
                          + " for collider creation.");

                terrainData.Add(collider.terrainData);
                Transform transform = terrains[i].transform;
                terrainPositions.Add(transform.position);
                terrainRotations.Add(transform.rotation);
                terrainScales.Add(transform.lossyScale);
                terrainTags.Add(transform.tag);
                terrainLayers.Add(transform.gameObject.layer);
            }
        }

        /// <summary>
        /// Generate the mesh colliders.
        /// </summary>
        /// <param name="collidersParent"></param>
        private void GenerateMeshColliders(GameObject collidersParent)
        {
            for (int i = 0; i < meshes.Count; i++)
            {
                GameObject newCollider = new GameObject(meshes[i].name)
                                         {
                                             layer = layers[i],
                                             tag = tags[i]
                                         };

                Transform transform = newCollider.transform;
                transform.parent = collidersParent.transform;
                transform.position = positions[i];
                transform.rotation = rotations[i];
                transform.localScale = scales[i];

                MeshCollider meshCollider = newCollider.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshes[i];

                if (library.TagToPhysicMaterial.ContainsKey(tags[i]))
                    meshCollider.sharedMaterial = library.TagToPhysicMaterial[tags[i]];
            }
        }

        /// <summary>
        /// Generate the terrain colliders.
        /// </summary>
        /// <param name="collidersParent"></param>
        private void GenerateTerrainColliders(GameObject collidersParent)
        {
            for (int i = 0; i < terrainData.Count; ++i)
            {
                GameObject newGameObject = new GameObject(terrainData[i].name)
                                           {
                                               layer = terrainLayers[i],
                                               tag = terrainTags[i]
                                           };

                Transform transform = newGameObject.transform;
                transform.parent = collidersParent.transform;
                transform.position = terrainPositions[i];
                transform.rotation = terrainRotations[i];
                transform.localScale = terrainScales[i];

                TerrainCollider newCollider = newGameObject.AddComponent<TerrainCollider>();

                newCollider.terrainData = terrainData[i];

                if (library.TagToPhysicMaterial.ContainsKey(terrainTags[i]))
                    newCollider.sharedMaterial = library.TagToPhysicMaterial[terrainTags[i]];
            }
        }
    }
}