using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AOSEditorWindow : EditorWindow
{
    private string processID = ""; // Store the process ID here

    private string walletFilePath = "";
    private string processInput = ""; // Process input field for process name or ID
    private bool isLoggedIn = false;
    private string commandInput = "";
    private List<string> consoleOutputLines = new List<string>();
    private const int maxConsoleLines = 100; // Limit for the number of lines in the console
    private Vector2 scrollPosition; // For the scroll view
    private static AOSEditorWindow windowInstance; // Single window instance
    private InteractiveCMDShell shell;

    [MenuItem("Tools/AOS")]
    public static void ShowWindow()
    {
        // Ensure only one instance of the window is open
        if (windowInstance != null)
        {
            windowInstance.Close(); // Close the existing window
        }
        windowInstance = (AOSEditorWindow)GetWindow(typeof(AOSEditorWindow));
        windowInstance.titleContent = new GUIContent("AOS");
        windowInstance.Show();
    }

    private void OnEnable()
    {
        if (shell != null)
        {
            shell.Stop(); // Stop the existing shell if one exists
        }
        shell = new InteractiveCMDShell(); // Initialize a new shell instance
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

    private void OnGUI()
    {
        GUILayout.Label("AOS Login", EditorStyles.boldLabel);

        if (!isLoggedIn)
        {
            // File Picker for Wallet (optional)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Wallet File (optional):");
            walletFilePath = EditorGUILayout.TextField(walletFilePath);
            if (GUILayout.Button("Browse"))
            {
                walletFilePath = EditorUtility.OpenFilePanel("Select Wallet File", "", "json");
            }
            EditorGUILayout.EndHorizontal();

            // Input field for process (optional)
            GUILayout.Label("Process (optional):");
            processInput = EditorGUILayout.TextField(processInput);

            // Login button
            if (GUILayout.Button("Connect"))
            {
                Login();
            }
        }
        else
        {
            // Display process ID if connected
            if (!string.IsNullOrEmpty(processID))
            {
                GUILayout.Label("Connected to Process ID: " + processID);
            }

            // Logout button (only shown when logged in)
            if (GUILayout.Button("Disconnect"))
            {
                DisconnectShell(); // Disconnect the shell
            }

            // Scroll view for console output that extends as the window resizes
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            foreach (var line in consoleOutputLines)
            {
                // Clean up the output to remove ANSI codes
                EditorGUILayout.LabelField(StripANSICodes(line));
            }

            EditorGUILayout.EndScrollView();

            // Add a little space between the scroll view and the command input
            GUILayout.Space(10);

            // Horizontal layout for command prompt and input field
            GUILayout.BeginHorizontal();

            // Display the command prompt (non-editable)
            GUILayout.Label("aos> ", GUILayout.Width(50)); // The command prompt string

            // Input field for commands
            commandInput = EditorGUILayout.TextField(commandInput);

            GUILayout.EndHorizontal();

            // Button to send the command
            if (GUILayout.Button("Send Command"))
            {
                if (!string.IsNullOrEmpty(commandInput))
                {
                    RunCommand(commandInput);
                    commandInput = ""; // Clear the input field after the command is sent
                }
            }
        }
    }

    private void RunCommand(string command)
    {
        Debug.Log("Running command: " + command);
        shell.RunCommand(command);
        commandInput = ""; // Clear the command input after sending
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
        // Execute the login command
        shell.RunCommand(loginString);
        isLoggedIn = true;
    }

    private void DisconnectShell()
    {
        if (shell != null)
        {
            shell.Stop(); // Simply stop the shell
            shell = new InteractiveCMDShell(); // Create a new shell instance for future use
            isLoggedIn = false; // Mark the user as logged out
            processID = ""; // Clear the process ID
            consoleOutputLines.Clear(); // Clear the console output
        }
    }

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
                    // Check if the line contains the process ID
                    if (line.StartsWith("Your AOS process: "))
                    {
                        processID = line.Replace("Your AOS process:", "").Trim(); // Store the process ID
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

    private string StripANSICodes(string input)
    {
        // This regex matches ANSI escape sequences
        return System.Text.RegularExpressions.Regex.Replace(input, @"\x1B[@-_][0-?]*[ -/]*[@-~]", string.Empty);
    }
}
