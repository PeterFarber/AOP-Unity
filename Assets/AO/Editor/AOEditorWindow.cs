using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using AO;
using static AOP.Helpers;
using System.Collections;
using System.IO;
using System;
using SimpleJSON;

public class AOEditorWindow : EditorWindow
{
    private string processID = "";
    private string processName = "";
    private string walletFilePath = "";
    private string walletID = "";
    private string processInput = "";
    private bool isLoggedIn = false;
    private string commandInput = "";
    private string consolePrompt = "aos >";
    private List<string> consoleOutputLines = new List<string>();
    private const int maxConsoleLines = 100;
    private Vector2 scrollPosition;
    private static AOEditorWindow windowInstance;
    private InteractiveCMDShell shell;

    private List<Process> processes = new List<Process>();
    private int selectedProcessIndex = 0;
    private bool isFetchingProcesses = false;
    private string errorMessage = "";

    private static Texture2D windowIcon;

    private bool connectionError = false;
    private bool isPromptReady = false;

    private int selectedTab = 0;
    private string worldName = "";
    private string worldDescription = "";
    private string imageTXID = "";

    // New process fields
    private string newProcessName = "";
    private string newProcessModule = "";

    private int frame = 0;
    private double lastFrameTime = 0;
    private string[] loadingFrames = new string[] { "Loading", "Loading.", "Loading..", "Loading..." };

    [MenuItem("AO/AO Manager")]
    public static void ShowWindow()
    {
        if (windowInstance != null)
        {
            windowInstance.Close();
        }
        windowInstance = (AOEditorWindow)GetWindow(typeof(AOEditorWindow));
        windowIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/AO/Editor/Icons/AO.png", typeof(Texture2D));
        windowInstance.titleContent = new GUIContent("AO Manager", windowIcon);
        windowInstance.Show();
    }

    private void OnEnable()
    {
        DisconnectShell(restartShell: true, clearUIState: true, refreshWallet: true);
    }

    private void OnDisable()
    {
        DisconnectShell(restartShell: false, clearUIState: true, refreshWallet: false);
        windowInstance = null;
    }

    private void OnDestroy()
    {
        DisconnectShell(restartShell: false, clearUIState: true, refreshWallet: false);
        windowInstance = null;
    }

    private void Update()
    {
        if (isLoggedIn)
        {
            List<string> newLines = new List<string>();
            shell.GetRecentLines(newLines);

            if (newLines.Count > 0)
            {
                foreach (string line in newLines)
                {
                    if (line.StartsWith("Your AOS process: "))
                    {
                        processID = line.Replace("Your AOS process:", "").Trim();
                    }

                    consoleOutputLines.Add(line);

                    if (consoleOutputLines.Count > maxConsoleLines)
                    {
                        consoleOutputLines.RemoveAt(0);
                    }
                }

                Repaint();
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Use this tool to connect to processes, run commands, and manage your worlds.", MessageType.Info);

        if (!isLoggedIn)
        {
            DrawWalletSection();
        }
        else
        {
            DrawLoggedInUI();
        }
    }

    private void DrawWalletSection()
    {
        GUIStyle errorStyle = new GUIStyle(EditorStyles.label);
        errorStyle.normal.textColor = Color.red;

        if (!string.IsNullOrEmpty(errorMessage))
        {
            EditorGUILayout.LabelField(errorMessage, errorStyle);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Wallet JWK path:");
        walletFilePath = EditorGUILayout.TextField(walletFilePath);
        if (GUILayout.Button("Browse"))
        {
            walletFilePath = EditorUtility.OpenFilePanel("Select Wallet JWK File", "", "json");
            RefreshWallet();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(walletID))
        {
            GUILayout.Label($"Wallet ID: {walletID}");
        }

        GUILayout.Space(10);

        if (isFetchingProcesses)
        {
            GUILayout.Label("Fetching processes, please wait...");
        }
        else if (!string.IsNullOrEmpty(walletID) && string.IsNullOrEmpty(errorMessage))
        {
            DrawProcessSelection();
        }

        if (connectionError)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Display an error message if there was a connection error
            GUILayout.Label("Connection failed. Please try again.", errorStyle, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }

    private void DrawProcessSelection()
    {
        List<string> processOptions = new List<string> { "< New Process >" };
        foreach (var process in processes)
        {
            processOptions.Add($"{process.name} ({ShortenProcessID(process.id)}) | Module: {ShortenProcessID(process.module)}");
        }

        selectedProcessIndex = EditorGUILayout.Popup("Select Process", selectedProcessIndex, processOptions.ToArray());

        if (selectedProcessIndex == 0) // New process selected
        {
            GUILayout.Space(5);

            // New Process Name field on the same line
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("New Process Name:", GUILayout.Width(150)); // Adjust width as needed
            newProcessName = GUILayout.TextField(newProcessName, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Module field on the same line
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("New Process Module:", GUILayout.Width(150)); // Adjust width as needed
            newProcessModule = GUILayout.TextField(newProcessModule, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            Process selectedProcess = processes[selectedProcessIndex - 1]; // Adjust for "New" option
            processInput = selectedProcess.id;
            processName = selectedProcess.name;
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Connect"))
        {
            Login();
        }
    }

    private void DrawLoggedInUI()
    {
        GUILayout.Label($"Wallet ID: {walletID}");
        if (!string.IsNullOrEmpty(processID))
        {
            GUILayout.Label(!string.IsNullOrEmpty(processName) ? $"Process: {processName} (ID: {processID})" : $"Process: (ID: {processID})");
        }

        GUILayout.Space(5);

        if (!isPromptReady)
        {
            // Show loading animation while waiting for the prompt to be ready
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Use text-based or icon-based spinner here
            GUILayout.Label(GetLoadingText(), GUILayout.Width(100));  // Replace this with GetSpinnerIcon() if using icons

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {

            if (GUILayout.Button("Disconnect"))
            {
                DisconnectShell(restartShell: true, clearUIState: true, refreshWallet: true);
            }

            GUILayout.Space(5);

            DrawSeparatorLine();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            selectedTab = GUILayout.Toolbar(selectedTab, new string[] { "Export World", "Console" });
            GUILayout.EndHorizontal();
            switch (selectedTab)
            {
                case 0:
                    DrawExportTab();
                    break;
                case 1:
                    DrawCommandConsole();
                    break;
            }
        }
    }

    private void DrawSeparatorLine()
    {
        GUIStyle whiteLineStyle = new GUIStyle(GUI.skin.box);
        whiteLineStyle.normal.background = EditorGUIUtility.whiteTexture;
        whiteLineStyle.margin = new RectOffset(4, 4, 4, 4);
        GUILayout.Box(GUIContent.none, whiteLineStyle, GUILayout.ExpandWidth(true), GUILayout.Height(1));
    }

    private void DrawCommandConsole()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        foreach (var line in consoleOutputLines)
        {
            EditorGUILayout.LabelField(StripANSICodes(line));
        }

        EditorGUILayout.EndScrollView();

        DrawCommandPrompt();
    }

    private void DrawCommandPrompt()
    {
        GUILayout.BeginHorizontal();

        var promptWidth = GUI.skin.label.CalcSize(new GUIContent(consolePrompt)).x;
        GUILayout.Label($"{consolePrompt} ", GUILayout.Width(promptWidth));

        float availableWidth = position.width - promptWidth - 20;
        float textAreaHeight = GUI.skin.textArea.CalcHeight(new GUIContent(commandInput), availableWidth);
        commandInput = GUILayout.TextArea(commandInput, GUILayout.MinHeight(textAreaHeight), GUILayout.ExpandWidth(true), GUILayout.MaxWidth(availableWidth));

        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (GUILayout.Button("Send", GUILayout.ExpandWidth(true)))
        {
            if (!string.IsNullOrEmpty(commandInput))
            {
                RunCommand(commandInput);
                commandInput = "";
            }
        }
    }

    private void DrawExportTab()
    {
        GUILayout.Space(5);

        GUILayout.Label("World Name:");
        worldName = GUILayout.TextField(worldName);
        GUILayout.Space(5);

        GUILayout.Label("World Description:");
        worldDescription = GUILayout.TextArea(worldDescription, GUILayout.Height(60));
        GUILayout.Space(5);

        GUILayout.Label("Image TXID:");
        imageTXID = GUILayout.TextField(imageTXID);

        GUILayout.Space(5);

        if (GUILayout.Button("Export"))
        {
            ExportWorldState();
        }
    }

    private void ExportWorldState()
    {
        // Step 1: Get the world state JSON
        string sceneData = AOSceneExporter.ExportSceneToJson(worldName, worldDescription, imageTXID);

        // Step 2: Create the Lua script template with the world state JSON
            string luaScriptTemplate = @"
            local json = require('json')
            WorldState = json.decode([[
        __worldstate__
        ]])

            Handlers.add(
                'GetWorldInfo',
                Handlers.utils.hasMatchingTag('Action', 'GetWorldInfo'),
                function(Msg)
                    local worldInfo = require('json').encode({
                        WorldInfo = WorldState
                    })

                    ao.send({
                        Target = Msg.From,
                        Action = 'WorldInfo',
                        Data = worldInfo
                    })
                end
            )";

        // Replace the placeholder __worldstate__ with the actual JSON data
        string luaScript = luaScriptTemplate.Replace("__worldstate__", sceneData);

        // Step 3: Write the Lua script to the Assets folder
        string luaFilePath = Path.Combine(Application.dataPath, "simulation.lua");
        File.WriteAllText(luaFilePath, luaScript);

        RunCommand($".load simulation.lua");
        Debug.Log("Exported world state of process " + processID);
        // Step 4: Notify the user of success
        EditorUtility.DisplayDialog(
            "Export Successful",
            $"World exported to {processID}, check AO Console for details",
            "OK"
        );
    }

    public void RunCommand(string command)
    {
        if (!isLoggedIn)
        {
            Debug.LogError("Not logged in. Cannot run command.");
            return;
        }
        shell.RunCommand(command);
    }

    private void RefreshWallet()
    {
        if (!string.IsNullOrEmpty(walletFilePath))
        {
            walletID = WalletUtils.GetWalletIDFromPath(walletFilePath);

            if (walletID == null)
            {
                errorMessage = "The selected file is not a valid JWK wallet.";
                walletID = "";
            }
            else
            {
                errorMessage = "";
                FetchProcesses(walletID);
            }
        }
    }

    private void FetchProcesses(string walletID)
    {
        isFetchingProcesses = true;
        EditorCoroutineUtility.StartCoroutineOwnerless(GraphQLUtils.FetchProcessesCoroutine(walletID, OnFetchSuccess, OnFetchError));
    }

    private void OnFetchSuccess(string jsonResponse)
    {
        isFetchingProcesses = false;
        var parsedResponse = JsonUtility.FromJson<GraphQLResponse>(jsonResponse);
        processes.Clear();

        foreach (var edge in parsedResponse.data.transactions.edges)
        {
            string module = edge.node.tags.Find(tag => tag.name == "Module")?.value ?? "Unknown";
            string scheduler = edge.node.tags.Find(tag => tag.name == "Scheduler")?.value ?? "Unknown";

            Process process = new Process
            {
                id = edge.node.id,
                name = edge.node.tags.Find(tag => tag.name == "Name")?.value ?? "Unnamed",
                module = module,
                scheduler = scheduler
            };
            processes.Add(process);
        }

        Repaint();
    }

    private void OnFetchError(string error)
    {
        isFetchingProcesses = false;
        errorMessage = "Error fetching processes: " + error;
        Repaint();
    }

    private void Login()
    {
        string loginString = "aos";

        if (selectedProcessIndex == 0) // New process selected
        {
            if (!string.IsNullOrEmpty(newProcessName))
            {
                loginString += $" {newProcessName}";
            }

            if (!string.IsNullOrEmpty(newProcessModule))
            {
                loginString += $" --module {newProcessModule}";
            }

            processName = newProcessName;
        }
        else
        {
            if (!string.IsNullOrEmpty(processInput))
            {
                loginString += " " + processInput;

                TryGetWorldInfo();
            }
        }

        if (!string.IsNullOrEmpty(walletFilePath))
        {
            loginString += " --wallet \"" + walletFilePath + "\"";
        }

        shell.RunCommand(loginString);
        isLoggedIn = true;
        connectionError = false;

        // Clear fields after connecting
        newProcessName = "";
        newProcessModule = "";

        EditorCoroutineUtility.StartCoroutineOwnerless(WaitAndFinalizeLogin());
    }

    private void TryGetWorldInfo()
    {
        Debug.Log("Trying to get world info...");

        List<Tag> tags = new List<Tag>
        {
            new Tag("Action", "GetWorldInfo")
        };

        EditorCoroutineUtility.StartCoroutineOwnerless(DryrunUtils.SendRequestCoroutine(processInput, tags, (success, response) =>
        {
            if (success)
            {
                // Handle success
                if (response.Messages.Count > 0 && response.Messages[0].Data.Contains("WorldInfo"))
                {
                    JSONNode worldInfo = JSONNode.Parse(response.Messages[0].Data);
                    if (worldInfo.HasKey("WorldInfo"))
                    {
                        worldName = worldInfo["WorldInfo"]["name"];
                        worldDescription = worldInfo["WorldInfo"]["description"];
                        imageTXID = worldInfo["WorldInfo"]["imageTXID"];
                    }
                }
                Debug.Log("Request successful: " + response);
            }
            else
            {
                // Handle failure
                Debug.LogError("Request failed: " + response);
            }
        }, ownerId: walletID));
    }

    private IEnumerator WaitAndFinalizeLogin()
    {
        string currentShellLine = shell.GetCurrentLine();
        double startTime = EditorApplication.timeSinceStartup; // Use EditorApplication.timeSinceStartup

        while (!currentShellLine.Contains(">"))
        {
            if (!isLoggedIn)
            {
                yield break;
            }

            currentShellLine = shell.GetCurrentLine();

            // Check if the time difference exceeds the login timeout
            if (EditorApplication.timeSinceStartup - startTime >= 30.0f)
            {
                connectionError = true;
                isLoggedIn = false;
                DisconnectShell(restartShell: true, clearUIState: false, refreshWallet: true);
                Debug.LogError("Connection timed out");
                yield break;
            }

            yield return null;
        }

        consolePrompt = StripANSICodes(currentShellLine.Trim());
        isPromptReady = true;

        Repaint();
    }

    private string GetLoadingText()
    {
        // Update frame every 0.5 seconds
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastFrameTime > 0.5)
        {
            frame = (frame + 1) % loadingFrames.Length;
            lastFrameTime = currentTime;
            Repaint();
        }
        return loadingFrames[frame];
    }

    private void DisconnectShell(bool restartShell = false, bool clearUIState = true, bool refreshWallet = false)
    {
        if (shell != null)
        {
            shell.Stop();
            shell = null;
        }

        if (clearUIState)
        {
            isLoggedIn = false;
            consoleOutputLines.Clear();
            errorMessage = "";
            commandInput = "";
            consolePrompt = "";
            isPromptReady = false;
            connectionError = false;
            selectedTab = 0;
            selectedProcessIndex = 0;
            worldName = "";
            worldDescription = "";
            imageTXID = "";
            processID = "";
            processName = "";
        }

        if (refreshWallet)
        {
            RefreshWallet();
        }

        if (restartShell)
        {
            shell = new InteractiveCMDShell();
        }
    }
}