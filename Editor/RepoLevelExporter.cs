using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

public class CustomLevelExporter : EditorWindow
{
    private string levelName = "NewCustomLevel";
    private string assetBundleName = "customlevel.bundle";
    private string author = "AuthorName";
    private string description = "Level description";
    private string websiteUrl = "";
    private string gameVersion = "";
    private string networkMode = "";
    private string packageType = "";
    private string installMode = "";
    private string exportPath = "Assets/ExportedLevels";
    private Object selectedLevel;
    private List<string> dependencies = new List<string> { "TestMod1-0.0.0" };

    [MenuItem("Tools/Custom Level Exporter")]
    public static void ShowWindow()
    {
        GetWindow(typeof(CustomLevelExporter));
    }

    void OnGUI()
    {
        GUILayout.Label("Custom Level Exporter", EditorStyles.boldLabel);
        levelName = EditorGUILayout.TextField("Level Name", levelName);
        assetBundleName = EditorGUILayout.TextField("Asset Bundle Name", assetBundleName);
        author = EditorGUILayout.TextField("Author", author);
        description = EditorGUILayout.TextField("Description", description);
        websiteUrl = EditorGUILayout.TextField("Website URL", websiteUrl);
        gameVersion = EditorGUILayout.TextField("Game Version", gameVersion);
        networkMode = EditorGUILayout.TextField("Network Mode", networkMode);
        packageType = EditorGUILayout.TextField("Package Type", packageType);
        installMode = EditorGUILayout.TextField("Install Mode", installMode);
        exportPath = EditorGUILayout.TextField("Export Path", exportPath);
        selectedLevel = EditorGUILayout.ObjectField("Select Level Asset", selectedLevel, typeof(Object), false);

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
                              "    \"name\": \"" + assetBundleName + "\",\n" +
                              "    \"description\": \"" + description + "\",\n" +
                              "    \"version_number\": \"0.0.0\",\n" +
                              "    \"dependencies\": [\"TestMod1-0.0.0\"],\n" +
                              "    \"website_url\": \"" + websiteUrl + "\"\n" +
                              "}";

        File.WriteAllText(Path.Combine(levelFolder, "manifest.json"), manifestJson);
        File.WriteAllText(Path.Combine(levelFolder, "customlevel.txt"), "Custom Level Marker");

        string mmV2ManifestJson = "{" +
            "\"manifestVersion\":1," +
            "\"name\":\"" + assetBundleName + "\"," +
            "\"authorName\":\"" + author + "\"," +
            "\"websiteUrl\":\"" + websiteUrl + "\"," +
            "\"displayName\":\"" + assetBundleName + "\"," +
            "\"description\":\"" + description + "\"," +
            "\"gameVersion\":\"" + gameVersion + "\"," +
            "\"networkMode\":\"" + networkMode + "\"," +
            "\"packageType\":\"" + packageType + "\"," +
            "\"installMode\":\"" + installMode + "\"," +
            "\"installedAtTime\":0," +
            "\"loaders\":[]," +
            "\"dependencies\": [\"TestMod1-0.0.0\", \"" + string.Join("\",\"", dependencies) + "\"]," +
            "\"incompatibilities\":[]," +
            "\"optionalDependencies\":[]," +
            "\"versionNumber\":{\"major\":0,\"minor\":0,\"patch\":0}," +
            "\"enabled\":true," +
            "\"icon\":\"\"}";

        File.WriteAllText(Path.Combine(levelFolder, "mm_v2_manifest.json"), mmV2ManifestJson);

        BuildAssetBundle(levelFolder);
        CompressToZip(levelFolder);
    }

    void BuildAssetBundle(string outputPath)
    {
        string bundlePath = Path.Combine(outputPath, assetBundleName);

        AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
        buildMap[0].assetBundleName = assetBundleName;
        buildMap[0].assetNames = new string[] { AssetDatabase.GetAssetPath(selectedLevel) };

        BuildPipeline.BuildAssetBundles(outputPath, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        Debug.Log("Custom level exported successfully!");
    }

    void CompressToZip(string folderPath)
    {
        string zipPath = Path.Combine(exportPath, levelName + ".zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);

        // Cleanup extra files and leave only necessary ones
        foreach (string file in Directory.GetFiles(folderPath))
        {
            if (!(file.EndsWith(".bundle") ||
                  file.EndsWith("customlevel.txt") ||
                  file.EndsWith("manifest.json") ||
                  file.EndsWith("mm_v2_manifest.json")))
            {
                File.Delete(file);
            }
        }

        ZipFile.CreateFromDirectory(folderPath, zipPath);
        Debug.Log("Custom level compressed into " + zipPath);
    }
}