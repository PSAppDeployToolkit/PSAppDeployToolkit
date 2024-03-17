Option Explicit

Function IsCmdAtEnd(commandLine)
    Dim regEx, cmdPattern
    ' Regular expression pattern to match "cmd" or "cmd.exe" at the end of the string
    cmdPattern = "(cmd\.exe|cmd)$"
    
    ' Create a RegExp object
    Set regEx = New RegExp
    regEx.Pattern = cmdPattern
    regEx.IgnoreCase = True
    regEx.Global = False
    
    ' Test the pattern against the provided command line
    IsCmdAtEnd = regEx.Test(commandLine)
End Function

Function ArgvQuote(Argument, ForceQuote, CmdDetected)
    ' Used this article as a reference for this function:
    ' https://learn.microsoft.com/en-us/archive/blogs/twistylittlepassagesallalike/everyone-quotes-command-line-arguments-the-wrong-way

    ' This routine modifies the given argument to a command line such
    ' that CommandLineToArgvW will return the argument string unchanged.
    ' Arguments in a command line should be separated by spaces; this
    ' function does not add these spaces.

    Dim bQuoteNeeded, sEscapedArgument, i, NumberBackslashes, sMetaChars, j

    ' Define metacharacters
    sMetaChars = "()%!^""<>&|"

    ' Determine if we need to quote the argument
    bQuoteNeeded = ForceQuote Or IsEmpty(Argument) Or InStr(Argument, " ") > 0 Or InStr(Argument, vbTab) > 0 _
                   Or InStr(Argument, Chr(10)) > 0 Or InStr(Argument, Chr(11)) > 0 Or InStr(Argument, """") > 0

    If Not bQuoteNeeded Then
        ' If cmd detected, escape metacharacters
        If CmdDetected Then
            For i = 1 To Len(Argument)
                If InStr(sMetaChars, Mid(Argument, i, 1)) > 0 Then
                    sEscapedArgument = sEscapedArgument & "^" & Mid(Argument, i, 1)
                Else
                    sEscapedArgument = sEscapedArgument & Mid(Argument, i, 1)
                End If
            Next
        Else
            sEscapedArgument = Argument
        End If
    Else
        For i = 1 To Len(Argument)
            ' Count the number of backslashes
            NumberBackslashes = 0
            While Mid(Argument, i, 1) = "\"
                i = i + 1
                NumberBackslashes = NumberBackslashes + 1
            Wend

            If i > Len(Argument) Then
                ' Escape all backslashes, but let the terminating double
                ' quotation mark we add below be interpreted as a metacharacter.
                sEscapedArgument = sEscapedArgument & String(NumberBackslashes * 2, "\")
                Exit For
            ElseIf Mid(Argument, i, 1) = """" Then
                ' Escape all backslashes and the following double quotation mark
                If CmdDetected Then
                    sEscapedArgument = sEscapedArgument & String(NumberBackslashes * 2 + 1, "\") & "^" & """"
                Else
                    sEscapedArgument = sEscapedArgument & String(NumberBackslashes * 2 + 1, "\") & """"
                End If
            Else
                ' Backslashes aren't special here
                If CmdDetected And InStr(sMetaChars, Mid(Argument, i, 1)) > 0 Then
                    ' If cmd detected, escape metacharacters
                    sEscapedArgument = sEscapedArgument & String(NumberBackslashes, "\") & "^" & Mid(Argument, i, 1)
                Else
                    sEscapedArgument = sEscapedArgument & String(NumberBackslashes, "\") & Mid(Argument, i, 1)
                End If
            End If
        Next

        If CmdDetected Then
            sEscapedArgument = "^" & """" & sEscapedArgument & "^" & """"
        Else
            sEscapedArgument = """" & sEscapedArgument & """"
        End If
    End If

    ArgvQuote = sEscapedArgument
End Function

Dim sCmd, oWShell, iReturn, arg, bCmdDetected, oArgumentList

iReturn = 0
If WScript.Arguments.Count > 0 Then
    ' Check if first argument references cmd or cmd.exe
    bCmdDetected = IsCmdAtEnd(WScript.Arguments(0))

    Set oArgumentList = CreateObject("System.Collections.ArrayList")
    For Each arg In WScript.Arguments
        oArgumentList.Add(ArgvQuote(arg, False, bCmdDetected))
    Next

    sCmd = Join(oArgumentList.ToArray, " ")

    Set oWShell = CreateObject("WScript.Shell")
    iReturn = oWShell.Run(sCmd, 0, True)
End If

WScript.Quit iReturn
