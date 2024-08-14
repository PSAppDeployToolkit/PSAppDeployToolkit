function Resolve-ADTError
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
    Resolve-ADTError

    .EXAMPLE
    Resolve-ADTError -Property *

    .EXAMPLE
    Resolve-ADTError -Property InnerException

    .EXAMPLE
    Resolve-ADTError -GetErrorInvocation:$false

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [AllowEmptyCollection()]
        [System.Object[]]$ErrorRecord,

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

    begin {
        # If function was called without specifying an error record, then choose the latest error that occurred.
        if (!$ErrorRecord)
        {
            if ($Global:Error.Count -eq 0)
            {
                return
            }
            $ErrorRecord = $Global:Error[0]
        }

        # Allows selecting and filtering the properties on the error object if they exist.
        filter Get-ErrorPropertyNames
        {
            [CmdletBinding()]
            param (
                [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
                [ValidateNotNullOrEmpty()]
                [System.Object]$InputObject
            )

            # Store all properties.
            $properties = $InputObject | Get-Member -MemberType *Property | Select-Object -ExpandProperty Name

            # If we've asked for all properties, return early with the above.
            if ($($Property) -eq '*')
            {
                return $properties | Where-Object {![System.String]::IsNullOrWhiteSpace(($InputObject.$_ | Out-String).Trim())}
            }

            # Return all valid properties in the order used by the caller.
            return $Property | Where-Object {($properties -contains $_) -and ![System.String]::IsNullOrWhiteSpace(($InputObject.$_ | Out-String).Trim())}
        }
    }

    process {
        foreach ($errRecord in $ErrorRecord)
        {
            # Build out error properties.
            $logErrorMessage = [System.String]::Join("`n", "Error Record:", "-------------", $null, (Out-String -InputObject (Format-List -InputObject $(
                # Capture Error Exception here if the caller has selected all property values.
                if (($($Property) -ne '*') -and !$ExcludeErrorException -and $errRecord.Exception)
                {
                    $errRecord.Exception | Select-Object -Property ($errRecord.Exception | Get-ErrorPropertyNames)
                }

                # Capture Error Record.
                if (!$ExcludeErrorRecord)
                {
                    $errRecord | Select-Object -Property ($errRecord | Get-ErrorPropertyNames)
                }

                # Error Invocation Information.
                if (!$ExcludeErrorInvocation -and $errRecord.InvocationInfo)
                {
                    $errRecord.InvocationInfo | Select-Object -Property ($errRecord.InvocationInfo | Get-ErrorPropertyNames)
                }

                # Capture Error Exception here to display using our custom order.
                if (($($Property) -eq '*') -and !$ExcludeErrorException -and $errRecord.Exception)
                {
                    $errRecord.Exception | Select-Object -Property ($errRecord.Exception | Get-ErrorPropertyNames)
                }
            ))).Trim())

            # Capture Error Inner Exception(s).
            if (!$ExcludeErrorInnerException -and $errRecord.Exception -and $errRecord.Exception.InnerException)
            {
                # Set up initial variables.
                $innerExceptions = [System.Collections.Generic.List[System.String]]::new()
                $errInnerException = $errRecord.Exception.InnerException

                # Get all inner exceptions.
                while ($errInnerException)
                {
                    # Add a divider if we've already added a record.
                    if ($innerExceptions.Count)
                    {
                        $innerExceptions.Add("`n$('~' * 40)`n")
                    }

                    # Add error record and get next inner exception.
                    $innerExceptions.Add(($errInnerException | Select-Object -Property ($errInnerException | Get-ErrorPropertyNames) | Format-List | Out-String).Trim())
                    $errInnerException = $errInnerException.InnerException
                }

                # Output all inner exceptions to the caller.
                $logErrorMessage += "`n`n`n$([System.String]::Join("`n", "Error Inner Exception(s):", "-------------------------", $null, [System.String]::Join("`n", $innerExceptions)))"
            }

            # Output the error message to the caller.
            $logErrorMessage
        }
    }
}
