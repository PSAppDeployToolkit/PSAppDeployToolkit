#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

filter Write-ADTLogEntry
{
    <#

    .SYNOPSIS
    Write messages to a log file in CMTrace.exe compatible format or Legacy text file format.

    .DESCRIPTION
    Write messages to a log file in CMTrace.exe compatible format or Legacy text file format and optionally display in the console.

    .PARAMETER Message
    The message to write to the log file or output to the console.

    .PARAMETER Severity
    Defines message type. When writing to console or CMTrace.exe log format, it allows highlighting of message type.
    Options: 0 = Success (highlighted in green), 1 = Information (default), 2 = Warning (highlighted in yellow), 3 = Error (highlighted in red)

    .PARAMETER Source
    The source of the message being logged.

    .PARAMETER ScriptSection
    The heading for the portion of the script that is being executed. Default is: $installPhase.

    .PARAMETER PassThru
    Return the message that was passed to the function.

    .PARAMETER DebugMessage
    Specifies that the message is a debug message. Debug messages only get logged if -LogDebugMessage is set to $true.

    .INPUTS
    System.String. The message to write to the log file or output to the console.

    .OUTPUTS
    System.String[]. This function returns the provided output if -PassThru is specified.

    .EXAMPLE
    Write-ADTLogEntry -Message "Installing patch MS15-031" -Source 'Add-Patch'

    .EXAMPLE
    Write-ADTLogEntry -Message "Script is running on Windows 8" -Source 'Test-ValidOS'

    .LINK
    https://psappdeploytoolkit.com

    #>

    param (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [System.String[]]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateRange(0, 3)]
        [System.Nullable[System.Int32]]$Severity,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Source,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ScriptSection,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$DebugMessage
    )

    # The internals of this are within the session's class object.
    $Script:ADT.CurrentSession.WriteLogEntry($Message, $Severity, $Source, $ScriptSection, $DebugMessage)
    if ($PassThru) {return $Message}
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

Function Resolve-Error {
    <#
.SYNOPSIS

Enumerate error record details.

.DESCRIPTION

Enumerate an error record, or a collection of error record, properties. By default, the details for the last error will be enumerated.

.PARAMETER ErrorRecord

The error record to resolve. The default error record is the latest one: $global:Error(0). This parameter will also accept an array of error records.

.PARAMETER Property

The list of properties to display from the error record. Use "*" to display all properties.

Default list of error properties is: Message, FullyQualifiedErrorId, ScriptStackTrace, PositionMessage, InnerException

.PARAMETER GetErrorRecord

Get error record details as represented by $_.

.PARAMETER GetErrorInvocation

Get error record invocation information as represented by $_.InvocationInfo.

.PARAMETER GetErrorException

Get error record exception details as represented by $_.Exception.

.PARAMETER GetErrorInnerException

Get error record inner exception details as represented by $_.Exception.InnerException. Will retrieve all inner exceptions if there is more than one.

.INPUTS

System.Array.

Accepts an array of error records.

.OUTPUTS

System.String

Displays the error record details.

.EXAMPLE

Resolve-Error

.EXAMPLE

Resolve-Error -Property *

.EXAMPLE

Resolve-Error -Property InnerException

.EXAMPLE

Resolve-Error -GetErrorInvocation:$false

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $false, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [Array]$ErrorRecord,
        [Parameter(Mandatory = $false, Position = 1)]
        [ValidateNotNullorEmpty()]
        [String[]]$Property = ('Message', 'InnerException', 'FullyQualifiedErrorId', 'ScriptStackTrace', 'PositionMessage'),
        [Parameter(Mandatory = $false, Position = 2)]
        [Switch]$GetErrorRecord = $true,
        [Parameter(Mandatory = $false, Position = 3)]
        [Switch]$GetErrorInvocation = $true,
        [Parameter(Mandatory = $false, Position = 4)]
        [Switch]$GetErrorException = $true,
        [Parameter(Mandatory = $false, Position = 5)]
        [Switch]$GetErrorInnerException = $true
    )

    Begin {
        ## If function was called without specifying an error record, then choose the latest error that occurred
        If (-not $ErrorRecord) {
            If ($global:Error.Count -eq 0) {
                #Write-Warning -Message "The `$Error collection is empty"
                Return
            }
            Else {
                [Array]$ErrorRecord = $global:Error[0]
            }
        }

        ## Allows selecting and filtering the properties on the error object if they exist
        [ScriptBlock]$SelectProperty = {
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                $InputObject,
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [String[]]$Property
            )

            [String[]]$ObjectProperty = $InputObject | Get-Member -MemberType '*Property' | Select-Object -ExpandProperty 'Name'
            ForEach ($Prop in $Property) {
                If ($Prop -eq '*') {
                    [String[]]$PropertySelection = $ObjectProperty
                    Break
                }
                ElseIf ($ObjectProperty -contains $Prop) {
                    [String[]]$PropertySelection += $Prop
                }
            }
            Write-Output -InputObject ($PropertySelection)
        }

        #  Initialize variables to avoid error if 'Set-StrictMode' is set
        $LogErrorRecordMsg = $null
        $LogErrorInvocationMsg = $null
        $LogErrorExceptionMsg = $null
        $LogErrorMessageTmp = $null
        $LogInnerMessage = $null
    }
    Process {
        If (-not $ErrorRecord) {
            Return
        }
        ForEach ($ErrRecord in $ErrorRecord) {
            ## Capture Error Record
            If ($GetErrorRecord) {
                [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord -Property $Property
                $LogErrorRecordMsg = $ErrRecord | Select-Object -Property $SelectedProperties
            }

            ## Error Invocation Information
            If ($GetErrorInvocation) {
                If ($ErrRecord.InvocationInfo) {
                    [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.InvocationInfo -Property $Property
                    $LogErrorInvocationMsg = $ErrRecord.InvocationInfo | Select-Object -Property $SelectedProperties
                }
            }

            ## Capture Error Exception
            If ($GetErrorException) {
                If ($ErrRecord.Exception) {
                    [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.Exception -Property $Property
                    $LogErrorExceptionMsg = $ErrRecord.Exception | Select-Object -Property $SelectedProperties
                }
            }

            ## Display properties in the correct order
            If ($Property -eq '*') {
                #  If all properties were chosen for display, then arrange them in the order the error object displays them by default.
                If ($LogErrorRecordMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorRecordMsg
                }
                If ($LogErrorInvocationMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorInvocationMsg
                }
                If ($LogErrorExceptionMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorExceptionMsg
                }
            }
            Else {
                #  Display selected properties in our custom order
                If ($LogErrorExceptionMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorExceptionMsg
                }
                If ($LogErrorRecordMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorRecordMsg
                }
                If ($LogErrorInvocationMsg) {
                    [Array]$LogErrorMessageTmp += $LogErrorInvocationMsg
                }
            }

            If ($LogErrorMessageTmp) {
                $LogErrorMessage = 'Error Record:'
                $LogErrorMessage += "`n-------------"
                $LogErrorMsg = $LogErrorMessageTmp | Format-List | Out-String
                $LogErrorMessage += $LogErrorMsg
            }

            ## Capture Error Inner Exception(s)
            If ($GetErrorInnerException) {
                If ($ErrRecord.Exception -and $ErrRecord.Exception.InnerException) {
                    $LogInnerMessage = 'Error Inner Exception(s):'
                    $LogInnerMessage += "`n-------------------------"

                    $ErrorInnerException = $ErrRecord.Exception.InnerException
                    $Count = 0

                    While ($ErrorInnerException) {
                        [String]$InnerExceptionSeperator = '~' * 40

                        [String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrorInnerException -Property $Property
                        $LogErrorInnerExceptionMsg = $ErrorInnerException | Select-Object -Property $SelectedProperties | Format-List | Out-String

                        If ($Count -gt 0) {
                            $LogInnerMessage += $InnerExceptionSeperator
                        }
                        $LogInnerMessage += $LogErrorInnerExceptionMsg

                        $Count++
                        $ErrorInnerException = $ErrorInnerException.InnerException
                    }
                }
            }

            If ($LogErrorMessage) {
                $Output = $LogErrorMessage
            }
            If ($LogInnerMessage) {
                $Output += $LogInnerMessage
            }

            Write-Output -InputObject $Output

            If (Test-Path -LiteralPath 'variable:Output') {
                Clear-Variable -Name 'Output'
            }
            If (Test-Path -LiteralPath 'variable:LogErrorMessage') {
                Clear-Variable -Name 'LogErrorMessage'
            }
            If (Test-Path -LiteralPath 'variable:LogInnerMessage') {
                Clear-Variable -Name 'LogInnerMessage'
            }
            If (Test-Path -LiteralPath 'variable:LogErrorMessageTmp') {
                Clear-Variable -Name 'LogErrorMessageTmp'
            }
        }
    }
    End {
    }
}


#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Show-ADTHelpConsole
{
    # Run this via a new PowerShell window so it doesn't stall the main thread.
    Start-Process -FilePath (Get-ADTPowerShellProcessPath) -NoNewWindow -ArgumentList "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -EncodedCommand $([System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes([System.String]::Join("`n", {
        # Ensure job runs in strict mode since its in a new scope.
        $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
        $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
        Set-PSDebug -Strict
        Set-StrictMode -Version Latest

        # Import the module and store its passthru data so we can access it later.
        $module = Import-Module -Name '<<PSScriptRoot>>' -DisableNameChecking -PassThru

        # Build out the form's listbox.
        $helpListBox = [System.Windows.Forms.ListBox]::new()
        $helpListBox.ClientSize = [System.Drawing.Size]::new(261, 675)
        $helpListBox.Font = [System.Drawing.SystemFonts]::MessageBoxFont
        $helpListBox.Location = [System.Drawing.Point]::new(3,0)
        $helpListBox.add_SelectedIndexChanged({$helpTextBox.Text = [System.String]::Join("`n", ((Get-Help -Name $helpListBox.SelectedItem -Full | Out-String -Stream -Width ([System.Int32]::MaxValue)) -replace '^\s+$').TrimEnd()).Trim()})
        [System.Void]$helpListBox.Items.AddRange($module.ExportedCommands.Keys)

        # Build out the form's textbox.
        $helpTextBox = [System.Windows.Forms.RichTextBox]::new()
        $helpTextBox.ClientSize = [System.Drawing.Size]::new(1250, 675)
        $helpTextBox.Font = [System.Drawing.Font]::new('Consolas', 9)
        $helpTextBox.Location = [System.Drawing.Point]::new(271,0)
        $helpTextBox.ReadOnly = $true
        $helpTextBox.WordWrap = $false

        # Build out the form. The suspend/resume is crucial for HiDPI support!
        $helpForm = [System.Windows.Forms.Form]::new()
        $helpForm.SuspendLayout()
        $helpForm.Text = "$($module.Name) Help Console"
        $helpForm.Font = [System.Drawing.SystemFonts]::MessageBoxFont
        $helpForm.AutoScaleDimensions = [System.Drawing.SizeF]::new(7,15)
        $helpForm.AutoScaleMode = [System.Windows.Forms.AutoScaleMode]::Font
        $helpForm.AutoSize = $true
        $helpForm.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Fixed3D
        $helpForm.MaximizeBox = $false
        $helpForm.Controls.Add($helpListBox)
        $helpForm.Controls.Add($helpTextBox)
        $helpForm.ResumeLayout()

        # Show the form. Using Application.Run automatically manages disposal for us.
        [System.Windows.Forms.Application]::Run($helpForm)
    }.ToString().Trim().Replace('<<PSScriptRoot>>', $Script:PSScriptRoot).Split("`n").Trim()))))"
}
