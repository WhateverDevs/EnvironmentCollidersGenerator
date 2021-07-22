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
                
                EditorUtility.DisplayProgressBar("Generating colliders", "Finding lods...", .05f);
                
                LODGroup[] lodGroups = FindObjectsOfType<LODGroup>();
                
                // TODO: Register meshes in lods to use only the lod2 meshes and ignore the rest.

                EditorUtility.DisplayProgressBar("Generating colliders", "Finding renderers...", .1f);

                MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>();
                SkinnedMeshRenderer[] skinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>();

                Logger.Info("Found " + meshRenderers.Length + " meshes on the environment.");
                Logger.Info("Found " + skinnedMeshRenderers.Length + " skinned meshes on the environment.");

                List<Mesh> meshes = new List<Mesh>();
                List<Vector3> positions = new List<Vector3>();
                List<Quaternion> rotations = new List<Quaternion>();
                List<Vector3> scales = new List<Vector3>();

                List<string> tags = new List<string>();
                List<int> layers = new List<int>();

                for (int i = 0; i < meshRenderers.Length; i++)
                {
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
                
                for (int i = 0; i < skinnedMeshRenderers.Length; i++)
                {
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

                for (int i = 0; i < meshes.Count; i++)
                {
                    GameObject newCollider = new GameObject(meshes[i].name)
                                             {
                                                 layer = layers[i],
                                                 tag = tags[i]
                                             };

                    Transform transform = newCollider.GetComponent<Transform>();
                    transform.parent = collidersParent.transform;
                    transform.position = positions[i];
                    transform.rotation = rotations[i];
                    transform.localScale = scales[i];

                    MeshCollider meshCollider = newCollider.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = meshes[i];

                    if (library.TagToPhysicMaterial.ContainsKey(tags[i]))
                        meshCollider.sharedMaterial = library.TagToPhysicMaterial[tags[i]];
                }

                EditorUtility.DisplayProgressBar("Generating colliders", "Saving scene...", .8f);

                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}