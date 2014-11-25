Imports System.IO
Imports System.Xml

Module DeployApplication

    Sub Main()

        ' Set up variables
        Dim strAppFolder As String = My.Application.Info.DirectoryPath
        Dim strDeployScript As String = Path.Combine(strAppFolder, "Deploy-Application.ps1")
        Dim strToolkitFolder As String = Path.Combine(strAppFolder, "AppDeployToolkit")
        Dim strToolkitXMLFile As String = Path.Combine(strToolkitFolder, "AppDeployToolkitConfig.xml")
        Dim strPowerShellExecutable As String = Path.Combine(Environment.GetEnvironmentVariable("WinDir"), "System32\WindowsPowerShell\v1.0\PowerShell.exe")
        Dim strPowerShellArguments As String = "-ExecutionPolicy Bypass -NoProfile -NoLogo -WindowStyle Hidden"
        Dim strCommandLineArguments As String() = Environment.GetCommandLineArgs
        Dim strCommandLineArgumentsJoined As String = ""
        Dim blnForcex86Mode As Boolean = False
        Dim blnRequireAdmin As Boolean = False

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

            ' Check for x86 Mode
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
                ' Verify the Deploy-Application.ps1 script exists
                If Not My.Computer.FileSystem.FileExists(strDeployScript) Then
                    Throw New Exception("A critical component of the App Deployment Toolkit is missing." & vbNewLine & vbNewLine & "Unable to find the 'Deploy-Application.ps1' file." & vbNewLine & vbNewLine & "Please ensure you have all of the required files available to start the installation.")
                End If

                If (strCommandLineArgumentsJoined.Length) Then
                    strPowerShellArguments = strPowerShellArguments & " -File """ & strDeployScript & """ " & strCommandLineArgumentsJoined
                Else
                    strPowerShellArguments = strPowerShellArguments & " -File """ & strDeployScript & """"
                End If
                sub_DebugMessage("No '-File' parameter specified on command-line. Adding '-File Deploy-Application.ps1'...")
            End If

            ' Remove any unwanted command-line entries
            strPowerShellArguments = strPowerShellArguments.Replace("/32", "")
            sub_DebugMessage(strPowerShellArguments)

            ' Verify the toolkit folder exists
            If Not My.Computer.FileSystem.DirectoryExists(strToolkitFolder) Then
                Throw New Exception("A critical component of the App Deployment Toolkit is missing." & vbNewLine & vbNewLine & "Unable to find the 'AppDeployToolkit' folder." & vbNewLine & vbNewLine & "Please ensure you have all of the required files available to start the installation.")
            End If

            ' Verify the toolkit XML exists
            If Not My.Computer.FileSystem.FileExists(strToolkitXMLFile) Then
                Throw New Exception("A critical component of the App Deployment Toolkit is missing." & vbNewLine & vbNewLine & "Unable to find the 'AppDeployToolkitConfig.xml' file." & vbNewLine & vbNewLine & "Please ensure you have all of the required files available to start the installation.")
            Else
                ' Read the XML and determine whether we need Admin Rights
                Dim xml As New XmlDocument
                xml.Load(strToolkitXMLFile)
                Dim xmlNode As XmlNode
                Dim xmlRoot As XmlElement = xml.DocumentElement
                xmlNode = xmlRoot.SelectSingleNode("/AppDeployToolkit_Config/Toolkit_Options/Toolkit_RequireAdmin")
                blnRequireAdmin = Convert.ToBoolean(xmlNode.InnerText)
                If blnRequireAdmin Then
                    sub_DebugMessage("Administrator rights are required...")
                End If
            End If

            ' Switch to x86 PowerShell if required
            If blnIs64Bit And blnForcex86Mode Then
                strPowerShellExecutable = Path.Combine(Environment.GetEnvironmentVariable("WinDir"), "SysWOW64\WindowsPowerShell\v1.0\PowerShell.exe")
            End If

            ' Define PowerShell process
            Dim processStartInfo As ProcessStartInfo = New ProcessStartInfo
            processStartInfo.FileName = """" & strPowerShellExecutable & """"
            processStartInfo.Arguments = strPowerShellArguments
            processStartInfo.WorkingDirectory = """" & Path.GetDirectoryName(strPowerShellExecutable) & """"
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden
            processStartInfo.UseShellExecute = True
            ' Set the RunAs flag if the XML specifically calls for Admin Rights
            If ((blnRequireAdmin) And (Environment.OSVersion.Version.Major >= 6)) Then
                processStartInfo.Verb = "runas"
            End If

            ' Start the PowerShell process and wait for completion
            Dim processExitCode As Integer = -1
            Dim process As Process = New Process
            Try
                process.StartInfo = processStartInfo
                process.Start()
                process.WaitForExit()
                processExitCode = process.ExitCode
            Catch ex As Exception
                Throw
            Finally
                If Not (process Is Nothing) Then
                    process.Dispose()
                End If
            End Try

            ' Exit
            sub_DebugMessage("Exit Code: " & processExitCode)
            Environment.Exit(processExitCode)

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
