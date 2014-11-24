Imports System.IO

Module DeployApplication

    Sub Main()

        ' Set up variables
        Dim strAppFolder As String = My.Application.Info.DirectoryPath
        Dim strToolkitFolder As String = Path.Combine(strAppFolder, "AppDeployToolkit")
        Dim strPowerShellExecutable As String = Path.Combine(Environment.GetEnvironmentVariable("WinDir"), "System32\WindowsPowerShell\v1.0\PowerShell.exe")
        Dim strPowerShellArguments As String = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden"
        Dim strCommandLineArguments As String() = Environment.GetCommandLineArgs
        Dim strCommandLineArgumentsJoined As String = ""
        Dim blnForcex86Mode As Boolean = False

        ' Get OS Architecture
        Dim blnIs64Bit As Boolean = False
        If Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").Contains("64") Then blnIs64Bit = True

        Try

            ' Remove first command-line argument as this is always the executable name
            RemoveAt(strCommandLineArguments, 0)

            ' Create the joined command-line
            If strCommandLineArguments.Length > 0 Then
                strCommandLineArgumentsJoined = Join(strCommandLineArguments, " ")
            End If

            ' CHeck for x86 Mode
            If My.Application.CommandLineArgs.Contains("/32") Then
                blnForcex86Mode = True
                sub_DebugMessage("'/32' parameter specified on command-line. Running in Forced x86 Mode...")
            End If

            ' Check for file being specified
            If strCommandLineArgumentsJoined.Contains("-File") Then
                strPowerShellArguments = strPowerShellArguments & " " & strCommandLineArgumentsJoined
                sub_DebugMessage("'-File' parameter specified on command-line. Passing command-line untouched...")
            ElseIf strCommandLineArgumentsJoined.Contains(".ps1") Then
                strPowerShellArguments = strPowerShellArguments & " -File " & strCommandLineArgumentsJoined
                sub_DebugMessage(".ps1 specified on command-line. Adding '-File'...")
            Else
                strPowerShellArguments = strPowerShellArguments & " -File Deploy-Application.ps1 " & strCommandLineArgumentsJoined
                sub_DebugMessage("No '-File' parameter specified on command-line. Adding '-File Deploy-Application.ps1'...")
            End If

            ' Remove any unwanted command-line entries
            strPowerShellArguments = strPowerShellArguments.Replace("/32", "")
            sub_DebugMessage(strPowerShellArguments)

            ' Verify the toolkit folder exists
            If Not My.Computer.FileSystem.DirectoryExists(strToolkitFolder) Then
                Throw New Exception("A critical component of the App Deployment Toolkit is missing." & vbNewLine & vbNewLine & "Unable to find the 'AppDeployToolkit' folder." & vbNewLine & vbNewLine & "Please ensure you have all of the required files available to start the installation.")
            End If

            ' Switch to x86 PowerShell if required
            If blnIs64Bit And blnForcex86Mode Then
                strPowerShellExecutable = Path.Combine(Environment.GetEnvironmentVariable("WinDir"), "SysWOW64\WindowsPowerShell\v1.0\PowerShell.exe")
            End If


            ' Start PowerShell and wait for completion\
            Dim process As Process = New Process
            process.StartInfo.FileName = strPowerShellExecutable
            process.StartInfo.Arguments = strPowerShellArguments
            process.StartInfo.WorkingDirectory = strAppFolder
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            process.Start()
            process.WaitForExit()

            ' Exit
            sub_DebugMessage("Exit Code: " & process.ExitCode)
            Environment.Exit(process.ExitCode)

        Catch ex As Exception
            sub_DebugMessage(ex.Message, True, MsgBoxStyle.Critical)
            Environment.Exit(10)
        End Try

    End Sub

    Public Sub RemoveAt(Of T)(ByRef a() As T, ByVal index As Integer)
        ' Move elements after "index" down 1 position.
        Array.Copy(a, index + 1, a, index, UBound(a) - index)
        ' Shorten by 1 element.
        ReDim Preserve a(UBound(a) - 1)
    End Sub

    Public Sub sub_DebugMessage(Optional ByVal str_DebugMessage As String = Nothing, Optional ByVal bln_DisplayError As Boolean = False, Optional ByVal mbs_Style As MsgBoxStyle = MsgBoxStyle.Information)

        ' Output to the Console
        Console.WriteLine(str_DebugMessage)

        ' If we are to display an error message...
        If bln_DisplayError = True Then
            MsgBox(str_DebugMessage, CType(mbs_Style + MsgBoxStyle.OkOnly + MsgBoxStyle.MsgBoxSetForeground, MsgBoxStyle), My.Resources.App_Title)
        End If

    End Sub

End Module
