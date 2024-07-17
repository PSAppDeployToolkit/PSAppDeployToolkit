Option Explicit

If WScript.Arguments.Count < 1 Then
    WScript.Quit
End If

Dim sArg, sCmd, oWShell, iReturn
sCmd = ""

For Each sArg In WScript.Arguments
    'Replace the [{quote}] placeholder with a double quote
    sArg = Replace(sArg, "[{quote}]", """")
    sCmd = sCmd & sArg & " "
Next
'Trim the trailing space
sCmd = Left(sCmd, Len(sCmd) - 1)

iReturn = 0
Set oWShell = CreateObject("WScript.Shell")
iReturn = oWShell.Run(sCmd, 0, True)
Set oWShell = Nothing
WScript.Quit iReturn
