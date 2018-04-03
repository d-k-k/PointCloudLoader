﻿using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Unity Point Cloud Loader
// (C) 2016 Ryan Theriot, Eric Wu, Jack Lam. Laboratory for Advanced Visualization & Applications, University of Hawaii at Manoa.
// Version: February 17th, 2017

// Edit: April 2, 2018. Testing modification to support batch loading.

public class PointCloudLoaderWindow : EditorWindow {
    //Number of elements per data line from input file
    private static int elementsPerLine = 0;

    //Position of XYZ and RGB elements in data line
    private static int rPOS, gPOS, bPOS, xPOS, yPOS, zPOS;

    //Enumerator for PointCloud color range
    //None = No Color, Normalized = 0-1.0f, RGB = 0-255
    private enum ColorRange {
        NONE = 0,
        NORMALIZED = 1,
        RGB = 255
    }
    private static ColorRange colorRange;

    //Enumber for format standards
    private enum FormatStandard {
        CUSTOM = 0,
        PTS = 1,
        XYZ = 2,
        XYZRGB = 3
    }
    private static FormatStandard formatStandard;

    private string currentFolderPath = "";
    private GameObject batchGameObjectContainer;


    //Data line delimiter  
    public static string dataDelimiter;

    //Maximum vertices a mesh can have in Unity
    static int limitPoints = 65000;

    [MenuItem("Window/PointClouds/LoadCloud")]
    private static void ShowEditor() {
        EditorWindow window = GetWindow(typeof(PointCloudLoaderWindow), true, "Point Cload Loader");
        window.maxSize = new Vector2(385f, 450);
        window.minSize = window.maxSize;
    }

    //GUI Window Stuff - NO COMMENTS
    private void OnGUI() {

        GUIStyle help = new GUIStyle(GUI.skin.label);
        help.fontSize = 12;
        help.fontStyle = FontStyle.Bold;
        EditorGUILayout.LabelField("How To Use", help);

        EditorGUILayout.HelpBox("1. Set the number of elements that exist on each data line. \n" +
                                "2. Set the delimiter between each element. (Leave blank for white space) \n" +
                                "3. Set the index of the XYZ elements on the data line. (First element = 1) \n" +
                                "4. Select the range of the color data: \n" +
                                "       None: No Color Data \n" +
                                "       Normalized: 0.0 - 1.0 \n" +
                                "       RGB : 0 - 255 \n" +
                                "5. Set the index of the RGB elements on the data line. (First element = 1) \n" +
                                "6. Click \"Load Point Cloud File\"", MessageType.None);

        formatStandard = (FormatStandard)EditorGUILayout.EnumPopup(new GUIContent("Format", ""), formatStandard);

        if (formatStandard == FormatStandard.CUSTOM) {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            elementsPerLine = 6;
            elementsPerLine = EditorGUILayout.IntField(new GUIContent("Elements Per Data Line", "The Number of Elements in the data line"), elementsPerLine);
            dataDelimiter = EditorGUILayout.TextField(new GUIContent("Data Line Delimiter", "Leave blank for white space between elements"), dataDelimiter);
            xPOS = 1;
            yPOS = 2;
            zPOS = 3;
            xPOS = EditorGUILayout.IntField(new GUIContent("X Index", "Index of X in data line"), xPOS);
            yPOS = EditorGUILayout.IntField(new GUIContent("Y Index", "Index of Y in data line"), yPOS);
            zPOS = EditorGUILayout.IntField(new GUIContent("Z Index", "Index of Z in data line"), zPOS);

            colorRange = (ColorRange)EditorGUILayout.EnumPopup(new GUIContent("Color Range", "None(No Color), Normalized (0.0-1.0f), RGB(0-255)"), colorRange);

            if (colorRange == ColorRange.NORMALIZED || colorRange == ColorRange.RGB) {
                rPOS = 4;
                gPOS = 5;
                bPOS = 6;
                rPOS = EditorGUILayout.IntField(new GUIContent("Red Index", "Index of Red color in data line"), rPOS);
                gPOS = EditorGUILayout.IntField(new GUIContent("Green Index", "Index of Green color in data line"), gPOS);
                bPOS = EditorGUILayout.IntField(new GUIContent("Blue Index", "Index of Blue color in data line"), bPOS);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        } else if (formatStandard == FormatStandard.PTS) {
            elementsPerLine = 7;
            dataDelimiter = "";
            xPOS = 1;
            yPOS = 2;
            zPOS = 3;
            colorRange = ColorRange.RGB;
            rPOS = 5;
            gPOS = 6;
            bPOS = 7;
        } else if (formatStandard == FormatStandard.XYZ) {
            elementsPerLine = 3;
            dataDelimiter = "";
            xPOS = 1;
            yPOS = 2;
            zPOS = 3;
            colorRange = ColorRange.NONE;
        } else if (formatStandard == FormatStandard.XYZRGB) {
            elementsPerLine = 7;
            dataDelimiter = "";
            xPOS = 1;
            yPOS = 2;
            zPOS = 3;
            colorRange = ColorRange.NORMALIZED;
            rPOS = 4;
            gPOS = 5;
            bPOS = 6;
        }

        EditorGUILayout.Space();

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 12;
        buttonStyle.fontStyle = FontStyle.Bold;
        if (GUILayout.Button("Load Point Cloud File", buttonStyle, GUILayout.Height(50))) {
            LoadCloud(getPathofCloudToLoad());
        }
        if (GUILayout.Button("Batch file load", buttonStyle, GUILayout.Height(50))) {
            BatchLoader();
        }

    }

    private string getPathofCloudToLoad() {
        //Get path to file with EditorUtility
        string path = EditorUtility.OpenFilePanel("Load Point Cloud File", "", "*");
        return path;
    }

    private void BatchLoader() {
        Debug.Log("Automated loader");
        string folderpath = EditorUtility.OpenFolderPanel("Load Point Cloud Files From Folder", "", "");
        DirectoryInfo info = new DirectoryInfo(folderpath);
        FileInfo[] fileInfo = info.GetFiles();

        batchGameObjectContainer = new GameObject(fileInfo[0].Directory.Name);
        for (int i = 0; i < fileInfo.Length; i++) {
            // Ignore unity .meta files
            if (fileInfo[i].FullName.Contains(".meta")) {
                Debug.Log("Skipping " + fileInfo[i].FullName);
            } else {
                Debug.Log("Starting load of " + fileInfo[i].FullName
                    + " of the " + fileInfo[i].Directory.Name + " directory");
                LoadCloud(fileInfo[i].FullName, fileInfo[i].Directory.Name);
            }
        }
        EditorUtility.DisplayDialog("Point Cloud Loader", "Files saved to PointClouds folder", "Continue", "");
    }


    private void LoadCloud(string path, string batchName = "") {
        bool centerPoints = false;



        //If path doesn't exist of user exits dialog exit function
        if (path.Length == 0) return;

        //Set data delimiter
        char delimiter = ' ';
        try {
            if (dataDelimiter.Length != 0) delimiter = dataDelimiter.ToCharArray()[0];

        } catch (NullReferenceException) { // Possible that user doesn't given delimiter, which would throw NullReferenceException
        }

        //Create string to name future asset creation from file's name
        string filename = null;
        try {
            filename = Path.GetFileName(path).Split('.')[0];
            if (Path.GetFileName(path).Split('.').Length < 2) {
                Debug.LogError("PointCloudLoader: File must have an extension. (.pts, .xyz....etc)");
                return;
            }
        } catch (Exception e) {
            Debug.LogError(e);
            return;
        }

        //Create PointCloud Directories
        currentFolderPath = "Assets/PointClouds/";
        string tempBaseFolder = "Assets/PointClouds";
        // Don't have a trailing "/" on first param of CreateFolder! Or it will silently error :(
        if (!Directory.Exists(currentFolderPath))
            AssetDatabase.CreateFolder("Assets", "PointClouds");
        // For batch files create containing folder
        if (batchName.Length > 0) {
            currentFolderPath = "Assets/PointClouds/" + batchName + "/";
            if (!Directory.Exists(currentFolderPath))
                AssetDatabase.CreateFolder(tempBaseFolder, batchName);
            tempBaseFolder = "Assets/PointClouds/" + batchName;
        }
        if (!Directory.Exists(currentFolderPath + filename)) {
            UnityEditor.AssetDatabase.CreateFolder(tempBaseFolder, filename);
            if (!Directory.Exists(currentFolderPath + filename)) {
                Debug.Log(":( failed to make " + currentFolderPath + filename);
            }
        }

        //Setup Progress Bar 
        float progress = 0.0f;
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayProgressBar("Progress", "Percent Complete: " + (int)(progress * 100) + "%", progress);

        //Setup variables so we can use them to center the PointCloud at origin
        float xMin = float.MaxValue;
        float xMax = float.MinValue;
        float yMin = float.MaxValue;
        float yMax = float.MinValue;
        float zMin = float.MaxValue;
        float zMax = float.MinValue;

        //Streamreader to read data file
        StreamReader sr = new StreamReader(path);
        string line;

        //Could use a while loop but then cant show progress bar progression
        int numberOfLines = File.ReadAllLines(path).Length;
        int numPoints = 0;


        //For loop to count the number of data points (checks again elementsPerLine which is set by user)
        //Calculates the min and max of all axis to center point cloud at origin
        for (int i = 0; i < numberOfLines; i++) {
            line = sr.ReadLine();
            string[] words = line.Split(delimiter);

            //Only read data lines
            if (words.Length == elementsPerLine) {
                numPoints++;

                if (xMin > float.Parse(words[xPOS - 1]))
                    xMin = float.Parse(words[xPOS - 1]);
                if (xMax < float.Parse(words[xPOS - 1]))
                    xMax = float.Parse(words[xPOS - 1]);

                if (yMin > float.Parse(words[yPOS - 1]))
                    yMin = float.Parse(words[yPOS - 1]);
                if (yMax < float.Parse(words[yPOS - 1]))
                    yMax = float.Parse(words[yPOS - 1]);

                if (zMin > float.Parse(words[zPOS - 1]))
                    zMin = float.Parse(words[zPOS - 1]);
                if (zMax < float.Parse(words[zPOS - 1]))
                    zMax = float.Parse(words[zPOS - 1]);

            }

            //Update progress bar -Only updates every 10,000 lines - DisplayProgressBar is not efficient and slows progress
            progress = i * 1.0f / (numberOfLines - 1) * 1.0f;
            if (i % 10000 == 0)
                EditorUtility.DisplayProgressBar("Progress", "Percent Complete: " + (int)((progress * 100) / 3) + "%", progress / 3);

        }

        //Calculate origin of point cloud to shift cloud to unity origin
        float xAvg = 0,  yAvg = 0, zAvg = 0;
        //Calculate origin of point cloud to shift cloud to unity origin
        if (centerPoints) {
            xAvg = (xMin + xMax) / 2;
            yAvg = (yMin + yMax) / 2;
            zAvg = (zMin + zMax) / 2;
        }

        //Setup array for the points and their colors
        Vector3[] points = new Vector3[numPoints];
        Color[] colors = new Color[numPoints];

        //Reset Streamreader
        sr = new StreamReader(path);

        //For loop to create all the new vectors from the data points
        for (int i = 0; i < numPoints; i++) {
            line = sr.ReadLine();
            string[] words = line.Split(delimiter);

            //Only read data lines
            while (words.Length != elementsPerLine) {
                line = sr.ReadLine();
                words = line.Split(' ');
            }

            //Read data line for XYZ and RGB
            float x = float.Parse(words[xPOS - 1]) - xAvg;
            float y = float.Parse(words[yPOS - 1]) - yAvg;
            float z = (float.Parse(words[zPOS - 1]) - zAvg) * -1; //Flips to Unity's Left Handed Coorindate System
            float r = 1.0f;
            float g = 1.0f;
            float b = 1.0f;

            //If color range has been set also get color from data line
            if (colorRange == ColorRange.NORMALIZED || colorRange == ColorRange.RGB) {
                r = float.Parse(words[rPOS - 1]) / (int)colorRange;
                g = float.Parse(words[gPOS - 1]) / (int)colorRange;
                b = float.Parse(words[bPOS - 1]) / (int)colorRange;
            }

            //Save new vector to point array
            //Save new color to color array
            points[i] = new Vector3(x, y, z);
            colors[i] = new Color(r, g, b, 1.0f);

            //Update Progress Bar
            progress = i * 1.0f / (numPoints - 1) * 1.0f;
            if (i % 10000 == 0)
                EditorUtility.DisplayProgressBar("Progress", "Percent Complete: " + (int)(((progress * 100) / 3) + 33) + "%", progress / 3 + .33f);


        }

        //Close Stream reader
        sr.Close();


        // Instantiate Point Groups
        //Unity limits the number of points per mesh to 65,000.  
        //For large point clouds the complete mesh wil be broken down into smaller meshes
        int numMeshes = Mathf.CeilToInt(numPoints * 1.0f / limitPoints * 1.0f);

        //Create the new gameobject
        GameObject cloudGameObject = new GameObject(filename);

        //Create an new material using the point cloud shader
        Material newMat = new Material(Shader.Find("PointCloudShader"));
        newMat.SetFloat("_Size", 1.0f);
        //Save new Material
        AssetDatabase.CreateAsset(newMat, currentFolderPath + filename + "Material" + ".mat");

        //Create the sub meshes of the point cloud
        for (int i = 0; i < numMeshes - 1; i++) {
            CreateMeshGroup(i, limitPoints, filename, cloudGameObject, points, colors, newMat);

            progress = i * 1.0f / (numMeshes - 2) * 1.0f;
            if (i % 2 == 0)
                EditorUtility.DisplayProgressBar("Progress", "Percent Complete: " + (int)(((progress * 100) / 3) + 66) + "%", progress / 3 + .66f);

        }
        //Create one last mesh from the remaining points
        int remainPoints = (numMeshes - 1) * limitPoints;
        CreateMeshGroup(numMeshes - 1, numPoints - remainPoints, filename, cloudGameObject, points, colors, newMat);

        progress = 100.0f;
        EditorUtility.DisplayProgressBar("Progress", "Percent Complete: " + progress + "%", 1.0f);

        //Store PointCloud
        UnityEditor.PrefabUtility.CreatePrefab(currentFolderPath + filename + ".prefab", cloudGameObject);
        if (batchName.Length == 0) {
            EditorUtility.DisplayDialog("Point Cloud Loader", filename + " Saved to PointClouds folder", "Continue", "");
        }
        EditorUtility.ClearProgressBar();

        // Finally add the object to the batch holder if part of batch
        if (batchName.Length > 0) {
            cloudGameObject.transform.parent = batchGameObjectContainer.transform;
        }

        return;
    }

    private void CreateMeshGroup(int meshIndex, int numPoints, string filename, GameObject pointCloud, Vector3[] points, Color[] colors, Material mat) {

        //Create GameObject and set parent
        GameObject pointGroup = new GameObject(filename + meshIndex);
        pointGroup.transform.parent = pointCloud.transform;

        //Add mesh to gameobject
        Mesh mesh = new Mesh();
        pointGroup.AddComponent<MeshFilter>();
        pointGroup.GetComponent<MeshFilter>().mesh = mesh;

        //Add Mesh Renderer and material
        pointGroup.AddComponent<MeshRenderer>();
        pointGroup.GetComponent<Renderer>().sharedMaterial = mat;

        //Create points and color arrays
        int[] indecies = new int[numPoints];
        Vector3[] meshPoints = new Vector3[numPoints];
        Color[] meshColors = new Color[numPoints];

        for (int i = 0; i < numPoints; ++i) {
            indecies[i] = i;
            meshPoints[i] = points[meshIndex * limitPoints + i];
            meshColors[i] = colors[meshIndex * limitPoints + i];
        }

        //Set all points and colors on mesh
        mesh.vertices = meshPoints;
        mesh.colors = meshColors;
        mesh.SetIndices(indecies, MeshTopology.Points, 0);

        //Create bogus uv and normals
        mesh.uv = new Vector2[numPoints];
        mesh.normals = new Vector3[numPoints];

        // Store Mesh
        UnityEditor.AssetDatabase.CreateAsset(mesh, currentFolderPath + filename + @"/" + filename + meshIndex + ".asset");
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        return;
    }

}
