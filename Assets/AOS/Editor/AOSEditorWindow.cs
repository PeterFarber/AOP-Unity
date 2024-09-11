using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.EditorCoroutines.Editor;
using AO;
using System.Collections;

public class AOSEditorWindow : EditorWindow
{
    private string processID = ""; // Store the process ID here
    private string processName = ""; // Store the process name here
    private string walletFilePath = "";
    private string walletID = ""; // Store the wallet ID
    private string processInput = ""; // Process input field for process name or ID
    private bool isLoggedIn = false;
    private string commandInput = "";
    private string consolePrompt = "aos >"; // Capture the real prompt dynamically
    private List<string> consoleOutputLines = new List<string>();
    private const int maxConsoleLines = 100; // Limit for the number of lines in the console
    private Vector2 scrollPosition; // For the scroll view
    private static AOSEditorWindow windowInstance; // Single window instance
    private InteractiveCMDShell shell;

    private List<Process> processes = new List<Process>();
    private int selectedProcessIndex = 0; // To track the dropdown selection
    private bool isFetchingProcesses = false; // To indicate fetching status
    private string errorMessage = ""; // To display error messages

    private static Texture2D windowIcon; // Texture for the window icon

    private float loginTimeout = 10.0f; // Timeout for waiting for the prompt
    private string loadingMessage = "Connecting... Please wait."; // Message to show while connecting
    private bool connectionError = false; // Flag for connection error

    [MenuItem("AO/AO Manager")]
    public static void ShowWindow()
    {
        // Ensure only one instance of the window is open
        if (windowInstance != null)
        {
            windowInstance.Close(); // Close the existing window
        }
        windowInstance = (AOSEditorWindow)GetWindow(typeof(AOSEditorWindow));
        windowInstance.titleContent = new GUIContent("AO Manager", windowIcon); // Assign the icon

        windowInstance.Show();
    }

    private void OnEnable()
    {
        if (shell != null)
        {
            shell.Stop(); // Stop the existing shell if one exists
        }
        shell = new InteractiveCMDShell(); // Initialize a new shell instance

        windowIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/AOS/Editor/Icons/AO.png", typeof(Texture2D));
    }

    private void OnDisable()
    {
        // Clean up the shell when the window is closed
        if (shell != null)
        {
            shell.Stop();
            shell = null;
        }
        windowInstance = null; // Ensure window instance is cleared
    }

    private void OnDestroy()
    {
        // Ensure the shell is closed when the window is destroyed
        if (shell != null)
        {
            shell.Stop();
            shell = null;
        }
        windowInstance = null;
    }

    private bool isPromptReady = false; // Tracks whether the prompt is detected after login

    private void Update()
    {
        if (isLoggedIn)
        {
            // Fetch any new lines from the shell and add them to the console output
            List<string> newLines = new List<string>();
            shell.GetRecentLines(newLines);

            if (newLines.Count > 0)
            {
                foreach (string line in newLines)
                {
                    // Check if the line contains the process ID or the prompt
                    if (line.StartsWith("Your AOS process: "))
                    {
                        processID = line.Replace("Your AOS process:", "").Trim(); // Store the process ID
                    }

                    // Check if the line contains the prompt (ends with '>')
                    if (line.EndsWith(">"))
                    {
                        isPromptReady = true; // Prompt is now ready, allow command input
                    }

                    consoleOutputLines.Add(line);

                    // If we exceed the max number of console lines, remove the oldest lines
                    if (consoleOutputLines.Count > maxConsoleLines)
                    {
                        consoleOutputLines.RemoveAt(0);
                    }
                }

                Repaint(); // Repaint the window to show new console output
            }
        }
    }

    private void OnGUI()
    {
        // Info box at the top
        EditorGUILayout.HelpBox("Use this tool to connect to processes, run commands, and manage your worlds.", MessageType.Info);

        // Define a red error style for the error message
        GUIStyle errorStyle = new GUIStyle(EditorStyles.label);
        errorStyle.normal.textColor = Color.red;

        // Show error message if any
        if (!string.IsNullOrEmpty(errorMessage))
        {
            EditorGUILayout.LabelField(errorMessage, errorStyle);
        }

        if (!isLoggedIn)
        {
            // Wallet file picker - always visible
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Wallet JWK path:");
            walletFilePath = EditorGUILayout.TextField(walletFilePath);
            if (GUILayout.Button("Browse"))
            {
                walletFilePath = EditorUtility.OpenFilePanel("Select Wallet JWK File", "", "json");
                if (!string.IsNullOrEmpty(walletFilePath))
                {
                    walletID = GetWalletID(walletFilePath); // Extract the wallet ID from the JSON file

                    if (walletID == "Invalid Wallet JWK File")
                    {
                        // Show error, don't proceed with fetching processes
                        errorMessage = "The selected file is not a valid JWK wallet.";
                        walletID = "";
                    }
                    else
                    {
                        // If valid wallet, clear error and fetch processes
                        errorMessage = ""; // Clear any previous error message
                        FetchProcesses(walletID); // Automatically fetch processes after wallet is selected
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Display the wallet ID once it's extracted
            if (!string.IsNullOrEmpty(walletID))
            {
                GUILayout.Label($"Wallet ID: {walletID}");
            }

            // Show loading message while fetching processes
            if (isFetchingProcesses)
            {
                GUILayout.Label("Fetching processes, please wait...");
            }

            // Only show the process input and connect button after fetching processes or if there was an error fetching processes
            if (!isFetchingProcesses && !string.IsNullOrEmpty(walletID) && string.IsNullOrEmpty(errorMessage))
            {
                if (processes.Count > 0)
                {
                    string[] processOptions = new string[processes.Count];
                    for (int i = 0; i < processes.Count; i++)
                    {
                        processOptions[i] = $"{processes[i].name} ({ShortenProcessID(processes[i].id)})";
                    }

                    selectedProcessIndex = EditorGUILayout.Popup("Select Process", selectedProcessIndex, processOptions);

                    Process selectedProcess = processes[selectedProcessIndex];
                    processInput = selectedProcess.id; // Set processInput to selected process ID
                    processName = selectedProcess.name; // Set processName for connected view
                }
                else
                {
                    // If no processes are found, show the manual input field
                    GUILayout.Label("Process (optional):");
                    processInput = EditorGUILayout.TextField(processInput);
                }

                // Connect button (only enabled after fetching processes is complete or if there was an error)
                if (GUILayout.Button("Connect"))
                {
                    Login();
                }
            }
        }
        // If connected, show the command input and send button
        else
        {
            // Display wallet and process info if connected
            GUILayout.Label($"Wallet ID: {walletID}");
            if (!string.IsNullOrEmpty(processID))
            {
                if (!string.IsNullOrEmpty(processName))
                {
                    GUILayout.Label($"Process: {processName} (ID: {processID})");
                }
                else
                {
                    GUILayout.Label($"Process: (ID: {processID})");
                }
            }

            // Disconnect button
            if (GUILayout.Button("Disconnect"))
            {
                DisconnectShell(); // Disconnect the shell and return to pre-connection state
            }

            GUILayout.Space(10);

            // Scroll view for console output (this shows even while waiting for the prompt)
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            foreach (var line in consoleOutputLines)
            {
                EditorGUILayout.LabelField(StripANSICodes(line)); // Clean ANSI codes
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            // Show "Connecting... Please wait" while waiting for the prompt
            if (!isPromptReady && !connectionError)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); // Add space on the left
                GUILayout.Label(loadingMessage, GUILayout.Width(200)); // Center the message
                GUILayout.FlexibleSpace(); // Add space on the right
                GUILayout.EndHorizontal();
            }
            else if (connectionError)
            {
                // Show error if connection fails
                EditorGUILayout.LabelField("Connection failed. Please try again.", errorStyle);
            }
            else
            {
                // Layout for command prompt and input field
                GUILayout.BeginHorizontal();

                // Display the prompt, adjusting its width based on the content size
                var promptWidth = GUI.skin.label.CalcSize(new GUIContent(consolePrompt)).x;
                GUILayout.Label($"{consolePrompt} ", GUILayout.Width(promptWidth));

                // Get total available width for the input field (adjusted for margins and padding)
                float availableWidth = position.width - promptWidth - 20; // Subtract additional space for margins/padding

                // Calculate the height of the text area based on the content
                float textAreaHeight = GUI.skin.textArea.CalcHeight(new GUIContent(commandInput), availableWidth);

                // Adjust height for multiline text and handle word wrapping
                commandInput = GUILayout.TextArea(commandInput, GUILayout.MinHeight(textAreaHeight), GUILayout.ExpandWidth(true), GUILayout.MaxWidth(availableWidth));

                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                // "Send" button on the next line, extending to full width
                if (GUILayout.Button("Send", GUILayout.ExpandWidth(true)))
                {
                    if (!string.IsNullOrEmpty(commandInput))
                    {
                        RunCommand(commandInput);
                        commandInput = ""; // Clear the input field after the command is sent
                    }
                }
            }
        }
    }
    private void Login()
    {
        string loginString = "aos";

        // Append process input if provided
        if (!string.IsNullOrEmpty(processInput))
        {
            loginString += " " + processInput;
        }

        // Append wallet location if provided
        if (!string.IsNullOrEmpty(walletFilePath))
        {
            loginString += " --wallet \"" + walletFilePath + "\"";
        }

        Debug.Log("Logging in with command: " + loginString);
        shell.RunCommand(loginString);
        isLoggedIn = true;

        // Start coroutine to wait for prompt
        EditorCoroutineUtility.StartCoroutineOwnerless(WaitAndFinalizeLogin());
    }

    private IEnumerator WaitAndFinalizeLogin()
    {
        string currentShellLine = shell.GetCurrentLine();
        float startTime = Time.time;

        // Wait until we detect a prompt (e.g., ">") in the shell output
        while (!currentShellLine.Contains(">"))
        {
            if (!isLoggedIn)
            {
                yield break; // Exit if user disconnected or canceled login
            }

            currentShellLine = shell.GetCurrentLine();
            if (Time.time >= startTime + loginTimeout)
            {
                connectionError = true; // Set connection error
                isLoggedIn = false; // Reset login status
                shell.Stop(); // Stop the shell
                Debug.LogError("Connection timed out");
                yield break;
            }

            yield return null;
        }

        Debug.Log("Prompt detected: " + currentShellLine);

        // Capture and set the dynamic prompt (e.g., "UnityRulez@aos-0.2.1[Inbox:1]>")
        consolePrompt = StripANSICodes(currentShellLine.Trim());

        // Set the prompt ready flag to true, allowing user input
        isPromptReady = true;
        connectionError = false; // Reset any connection errors

        Debug.Log("Logged in successfully.");

        Repaint();
    }

    // Helper function to extract wallet ID from JSON file
    private string GetWalletID(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            return WalletUtils.ExtractWalletID(json);
        }
        catch
        {
            Debug.LogError("Failed to read wallet file.");
            return "Invalid Wallet JWK File";
        }
    }

    private string ShortenProcessID(string id)
    {
        // Take first 3 characters and last 3 characters and add "..." in between
        if (id.Length > 6)
            return $"{id.Substring(0, 3)}...{id.Substring(id.Length - 3)}";
        return id;
    }

    private void FetchProcesses(string walletID)
    {
        isFetchingProcesses = true; // Set loading state
        EditorCoroutineUtility.StartCoroutineOwnerless(NetworkUtils.FetchProcessesCoroutine(walletID, OnFetchSuccess, OnFetchError));
    }

    private void OnFetchSuccess(string jsonResponse)
    {
        isFetchingProcesses = false; // Clear loading state
        var parsedResponse = JsonUtility.FromJson<GraphQLResponse>(jsonResponse);
        processes.Clear();

        foreach (var edge in parsedResponse.data.transactions.edges)
        {
            Process process = new Process
            {
                id = edge.node.id,
                name = edge.node.tags.Find(tag => tag.name == "Name")?.value ?? "Unnamed"
            };
            processes.Add(process);
        }

        Repaint(); // Refresh the UI
    }

    private void OnFetchError(string error)
    {
        isFetchingProcesses = false; // Clear loading state
        errorMessage = "Error fetching processes: " + error;
        Repaint(); // Refresh the UI
    }

    private void RunCommand(string command)
    {
        Debug.Log("Running command: " + command);
        shell.RunCommand(command);
        commandInput = ""; // Clear the command input after sending
    }

    // private void Login()
    // {
    //     string loginString = "aos";

    //     // Append process input if provided
    //     if (!string.IsNullOrEmpty(processInput))
    //     {
    //         loginString += " " + processInput;
    //     }

    //     // Append wallet location if provided
    //     if (!string.IsNullOrEmpty(walletFilePath))
    //     {
    //         loginString += " --wallet \"" + walletFilePath + "\"";
    //     }

    //     Debug.Log("Logging in with command: " + loginString);
    //     shell.RunCommand(loginString);
    //     isLoggedIn = true;
    // }

    private void DisconnectShell()
    {
        if (shell != null)
        {
            shell.Stop();
            shell = new InteractiveCMDShell(); // Restart the shell
            isLoggedIn = false;

            // Retain the wallet and process selection after disconnecting
            consoleOutputLines.Clear();
            errorMessage = ""; // Clear any error messages
            commandInput = "";
            consolePrompt = "";
            isPromptReady = false;
            connectionError = false;
            // Wallet ID, wallet file, and process selection remain the same
        }
    }

    private string StripANSICodes(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, @"\x1B[@-_][0-?]*[ -/]*[@-~]", string.Empty);
    }
}