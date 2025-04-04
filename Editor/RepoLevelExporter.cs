using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

public class CustomLevelExporter : EditorWindow
{
    private string levelName = "NewCustomLevel";
    private string modName = "CustomLevel";
    private string author = "AuthorName";
    private string description = "Level description";
    private string websiteUrl = "";
    private string version_number = "1.0.0";
    private string exportPath = "Assets/ExportedLevels";
    private Object selectedLevel;
    private Object iconFile;
    private List<string> dependencies = new List<string> { "Alexekokota-RepoLevelLoader-1.0.0" };

    [MenuItem("Tools/Custom Level Exporter")]
    public static void ShowWindow()
    {
        GetWindow(typeof(CustomLevelExporter));
    }

    void OnGUI()
    {
        GUILayout.Label("Custom Level Exporter", EditorStyles.boldLabel);
        levelName = EditorGUILayout.TextField("Level Name", levelName);
        modName = EditorGUILayout.TextField("Mod Name", modName);
        author = EditorGUILayout.TextField("Author", author);
        description = EditorGUILayout.TextField("Description", description);
        websiteUrl = EditorGUILayout.TextField("Website URL", websiteUrl);
        version_number = EditorGUILayout.TextField("Version Number", version_number);
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);
        selectedLevel = EditorGUILayout.ObjectField("Select Level Asset", selectedLevel, typeof(Object), false);
        iconFile = EditorGUILayout.ObjectField("Select Icon File", iconFile, typeof(Object), false);

        GUILayout.Label("Dependencies (comma-separated)", EditorStyles.boldLabel);
        string dependencyInput = string.Join(",", dependencies);
        dependencyInput = EditorGUILayout.TextField("Dependencies", dependencyInput);
        dependencies = new List<string>(dependencyInput.Split(','));

        if (GUILayout.Button("Export Level"))
        {
            ExportLevel();
        }
    }

    void ExportLevel()
    {
        if (selectedLevel == null)
        {
            Debug.LogError("No level asset selected!");
            return;
        }

        string levelFolder = Path.Combine(exportPath, levelName);
        Directory.CreateDirectory(levelFolder);

        string manifestJson = "{\n" +
                              "    \"name\": \"" + modName + "\",\n" +
                              "    \"description\": \"" + description + "\",\n" +
                              "    \"version_number\": \"" + version_number + "\",\n" +
                              "    \"dependencies\": [\"Alexekokota-RepoLevelLoader-1.0.0\"],\n" +
                              "    \"website_url\": \"" + websiteUrl + "\"\n" +
                              "}";

        File.WriteAllText(Path.Combine(levelFolder, "manifest.json"), manifestJson);
        File.WriteAllText(Path.Combine(levelFolder, "customlevel.txt"), "Custom Level Marker");
        File.WriteAllText(Path.Combine(levelFolder, "README.md"), "Made Using Repo Level Exporter");

        string mmV2ManifestJson = "{" +
            "\"manifestVersion\":1," +
            "\"name\":\"" + modName + "\"," +
            "\"authorName\":\"" + author + "\"," +
            "\"websiteUrl\":\"" + websiteUrl + "\"," +
            "\"displayName\":\"" + modName + "\"," +
            "\"description\":\"" + description + "\"," +
            "\"version_number\":\"" + version_number + "\"," +
            "\"installedAtTime\":0," +
            "\"loaders\":[]," +
            "\"dependencies\": [\"Alexekokota-RepoLevelLoader-1.0.0\", \"" + string.Join("\",\"", dependencies) + "\"]," +
            "\"incompatibilities\":[]," +
            "\"optionalDependencies\":[]," +
            "\"versionNumber\":{\"major\":1,\"minor\":0,\"patch\":0}," +
            "\"enabled\":true," +
            "\"icon\":\"icon.png\"}";

        File.WriteAllText(Path.Combine(levelFolder, "mm_v2_manifest.json"), mmV2ManifestJson);

        if (iconFile != null)
        {
            string iconPath = AssetDatabase.GetAssetPath(iconFile);
            File.Copy(iconPath, Path.Combine(levelFolder, "icon.png"), true);
        }

        BuildAssetBundle(levelFolder);
        CompressToZip(levelFolder);
    }

    void BuildAssetBundle(string outputPath)
    {
        string bundleFolder = Path.Combine(outputPath, "AssetBundles");
        Directory.CreateDirectory(bundleFolder);

        HashSet<string> assetsSet = new HashSet<string>();
        string selectedLevelPath = AssetDatabase.GetAssetPath(selectedLevel);

        if (!string.IsNullOrEmpty(selectedLevelPath))
        {
            assetsSet.Add(selectedLevelPath);
            string[] dependencies = AssetDatabase.GetDependencies(selectedLevelPath, true);

            foreach (string asset in dependencies)
            {
                if (!asset.EndsWith(".cs") && !asset.Contains("/Editor/")) // Exclude scripts and editor files
                {
                    assetsSet.Add(asset);
                }
            }
        }

        // Ensure prefabs in "Level/" path are included
        string[] prefabPaths = Directory.GetFiles(Application.dataPath, "*.prefab", SearchOption.AllDirectories);
        foreach (string prefabPath in prefabPaths)
        {
            string assetPath = "Assets" + prefabPath.Replace(Application.dataPath, "").Replace("\\", "/");
            if (assetPath.Contains("Level/") && AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
            {
                assetsSet.Add(assetPath);
            }
        }

        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
        buildMap[0].assetBundleName = modName.ToLower() + ".bundle";
        buildMap[0].assetNames = new string[assetsSet.Count];
        assetsSet.CopyTo(buildMap[0].assetNames);

        BuildPipeline.BuildAssetBundles(bundleFolder, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        string builtBundlePath = Path.Combine(bundleFolder, modName.ToLower() + ".bundle");
        string finalBundlePath = Path.Combine(outputPath, modName.ToLower() + ".bundle");

        if (File.Exists(builtBundlePath))
        {
            File.Move(builtBundlePath, finalBundlePath);
            Debug.Log("Custom level exported successfully! AssetBundle created at: " + finalBundlePath);
        }
        else
        {
            Debug.LogError("AssetBundle creation failed! No bundle found at: " + builtBundlePath);
        }
    }





    void CompressToZip(string folderPath)
    {
        string zipPath = Path.Combine(exportPath, levelName + ".zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);

        foreach (string file in Directory.GetFiles(folderPath))
        {
            if (!(file.EndsWith(".bundle") ||
                  file.EndsWith("customlevel.txt") ||
                  file.EndsWith("manifest.json") ||
                  file.EndsWith("mm_v2_manifest.json") ||
                  file.EndsWith("README.md") ||
                  file.EndsWith("icon.png")))
            {
                File.Delete(file);
            }
        }

        ZipFile.CreateFromDirectory(folderPath, zipPath);
        Debug.Log("Custom level compressed into " + zipPath);
    }
}
