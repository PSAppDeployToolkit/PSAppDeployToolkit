#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Resolve-ADTErrorRecord
{
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

    .PARAMETER ExcludeErrorRecord
    Exclude error record details as represented by $_.

    .PARAMETER ExcludeErrorInvocation
    Exclude error record invocation information as represented by $_.InvocationInfo.

    .PARAMETER ExcludeErrorException
    Exclude error record exception details as represented by $_.Exception.

    .PARAMETER ExcludeErrorInnerException
    Exclude error record inner exception details as represented by $_.Exception.InnerException. Will retrieve all inner exceptions if there is more than one.

    .INPUTS
    System.Array. Accepts an array of error records.

    .OUTPUTS
    System.String. Displays the error record details.

    .EXAMPLE
    Resolve-ADTErrorRecord

    .EXAMPLE
    Resolve-ADTErrorRecord -Property *

    .EXAMPLE
    Resolve-ADTErrorRecord -Property InnerException

    .EXAMPLE
    Resolve-ADTErrorRecord -GetErrorInvocation:$false

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    [OutputType([System.String])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ErrorRecord]$ErrorRecord,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$Property = ('Message', 'InnerException', 'FullyQualifiedErrorId', 'ScriptStackTrace', 'PositionMessage'),

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeErrorRecord,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeErrorInvocation,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeErrorException,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExcludeErrorInnerException
    )

    begin
    {
        # Initialise function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Allows selecting and filtering the properties on the error object if they exist.
        filter Get-ErrorPropertyNames
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
                [ValidateNotNullOrEmpty()]
                [System.Object]$InputObject
            )

            # Store all properties.
            $properties = $InputObject | & $Script:CommandTable.'Get-Member' -MemberType *Property | & $Script:CommandTable.'Select-Object' -ExpandProperty Name

            # If we've asked for all properties, return early with the above.
            if ($($Property) -eq '*')
            {
                return $properties | & { process { if (![System.String]::IsNullOrWhiteSpace(($InputObject.$_ | & $Script:CommandTable.'Out-String').Trim())) { return $_ } } }
            }

            # Return all valid properties in the order used by the caller.
            return $Property | & { process { if (($properties -contains $_) -and ![System.String]::IsNullOrWhiteSpace(($InputObject.$_ | & $Script:CommandTable.'Out-String').Trim())) { return $_ } } }
        }
    }

    process
    {
        # Build out error objects to process in the right order.
        $errorObjects = $(
            if (($($Property) -ne '*') -and !$ExcludeErrorException -and $ErrorRecord.Exception)
            {
                $ErrorRecord.Exception
            }
            if (!$ExcludeErrorRecord)
            {
                $ErrorRecord
            }
            if (!$ExcludeErrorInvocation -and $ErrorRecord.InvocationInfo)
            {
                $ErrorRecord.InvocationInfo
            }
            if (($($Property) -eq '*') -and !$ExcludeErrorException -and $ErrorRecord.Exception)
            {
                $ErrorRecord.Exception
            }
        )

        # Open property collector and build it out.
        $logErrorProperties = [ordered]@{}
        foreach ($errorObject in $errorObjects)
        {
            # Store initial property count.
            $propCount = $logErrorProperties.Count

            # Add in all properties for the object.
            foreach ($propName in ($errorObject | Get-ErrorPropertyNames))
            {
                $logErrorProperties.Add($propName, ($errorObject.$propName).ToString().Trim())
            }

            # Append a new line to the last value for formatting purposes.
            if (!$propCount.Equals($logErrorProperties.Count))
            {
                $logErrorProperties.($logErrorProperties.Keys | & $Script:CommandTable.'Select-Object' -Last 1) += "`n"
            }
        }

        # Build out error properties.
        $logErrorMessage = [System.String]::Join("`n", "Error Record:", "-------------", $null, (& $Script:CommandTable.'Out-String' -InputObject (& $Script:CommandTable.'Format-List' -InputObject ([pscustomobject]$logErrorProperties))).Trim())

        # Capture Error Inner Exception(s).
        if (!$ExcludeErrorInnerException -and $ErrorRecord.Exception -and $ErrorRecord.Exception.InnerException)
        {
            # Set up initial variables.
            $innerExceptions = [System.Collections.Specialized.StringCollection]::new()
            $errInnerException = $ErrorRecord.Exception.InnerException

            # Get all inner exceptions.
            while ($errInnerException)
            {
                # Add a divider if we've already added a record.
                if ($innerExceptions.Count)
                {
                    $null = $innerExceptions.Add("`n$('~' * 40)`n")
                }

                # Add error record and get next inner exception.
                $null = $innerExceptions.Add(($errInnerException | & $Script:CommandTable.'Select-Object' -Property ($errInnerException | Get-ErrorPropertyNames) | & $Script:CommandTable.'Format-List' | & $Script:CommandTable.'Out-String').Trim())
                $errInnerException = $errInnerException.InnerException
            }

            # Output all inner exceptions to the caller.
            $logErrorMessage += "`n`n`n$([System.String]::Join("`n", "Error Inner Exception(s):", "-------------------------", $null, ($innerExceptions -join "`n")))"
        }

        # Output the error message to the caller.
        return $logErrorMessage
    }

    end
    {
        # Finalise function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
