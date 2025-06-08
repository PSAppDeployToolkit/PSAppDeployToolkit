#-----------------------------------------------------------------------------
#
# MARK: Resolve-ADTErrorRecord
#
#-----------------------------------------------------------------------------

function Resolve-ADTErrorRecord
{
    <#
    .SYNOPSIS
        Enumerates ErrorRecord details.

    .DESCRIPTION
        Enumerates an ErrorRecord, or a collection of ErrorRecord properties. This function can filter and display specific properties of the ErrorRecord, and can exclude certain parts of the error details.

    .PARAMETER ErrorRecord
        The ErrorRecord to resolve. For usage in a catch block, you'd use the automatic variable `$PSItem`. For usage out of a catch block, you can access the global $Error array's first error (on index 0).

    .PARAMETER Property
        The list of properties to display from the ErrorRecord. Use "*" to display all properties.

        Default list of error properties is: Message, FullyQualifiedErrorId, ScriptStackTrace, PositionMessage, InnerException

    .PARAMETER ExcludeErrorRecord
        Exclude ErrorRecord details as represented by $ErrorRecord.

    .PARAMETER ExcludeErrorInvocation
        Exclude ErrorRecord invocation information as represented by $ErrorRecord.InvocationInfo.

    .PARAMETER ExcludeErrorException
        Exclude ErrorRecord exception details as represented by $ErrorRecord.Exception.

    .PARAMETER ExcludeErrorInnerException
        Exclude ErrorRecord inner exception details as represented by $ErrorRecord.Exception.InnerException. Will retrieve all inner exceptions if there is more than one.

    .INPUTS
        System.Management.Automation.ErrorRecord

        Accepts one or more ErrorRecord objects via the pipeline.

    .OUTPUTS
        System.String

        Displays the ErrorRecord details.

    .EXAMPLE
        Resolve-ADTErrorRecord

        Enumerates the details of the last ErrorRecord.

    .EXAMPLE
        Resolve-ADTErrorRecord -Property *

        Enumerates all properties of the last ErrorRecord.

    .EXAMPLE
        Resolve-ADTErrorRecord -Property InnerException

        Enumerates only the InnerException property of the last ErrorRecord.

    .EXAMPLE
        Resolve-ADTErrorRecord -ExcludeErrorInvocation

        Enumerates the details of the last ErrorRecord, excluding the invocation information.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Resolve-ADTErrorRecord
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
        [SupportsWildcards()]
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
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $propsIsWildCard = $($Property).Equals('*')

        # Allows selecting and filtering the properties on the error object if they exist.
        filter Get-ErrorPropertyNames
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
                [ValidateNotNullOrEmpty()]
                [System.Object]$InputObject
            )

            # Store all properties.
            $properties = $InputObject | Get-Member -MemberType *Property | Select-Object -ExpandProperty Name

            # If we've asked for all properties, return early with the above.
            if ($propsIsWildCard)
            {
                return $properties | & { process { if (![System.String]::IsNullOrWhiteSpace(($InputObject.$_ | Out-String).Trim())) { return $_ } } }
            }

            # Return all valid properties in the order used by the caller.
            return $Property | & { process { if (($properties -contains $_) -and ![System.String]::IsNullOrWhiteSpace(($InputObject.$_ | Out-String).Trim())) { return $_ } } }
        }
    }

    process
    {
        # Build out error objects to process in the right order.
        $errorObjects = $(
            $canDoException = !$ExcludeErrorException -and $ErrorRecord.Exception
            if (!$propsIsWildCard -and $canDoException)
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
            if ($propsIsWildCard -and $canDoException)
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
                $logErrorProperties.($logErrorProperties.Keys | Select-Object -Last 1) += "`n"
            }
        }

        # Build out error properties.
        $logErrorMessage = [System.String]::Join("`n", "Error Record:", "-------------", $null, (Out-String -InputObject (Format-List -InputObject ([pscustomobject]$logErrorProperties)) -Width ([System.Int32]::MaxValue)).Trim())

        # Capture Error Inner Exception(s).
        if (!$ExcludeErrorInnerException -and $ErrorRecord.Exception -and $ErrorRecord.Exception.InnerException)
        {
            # Set up initial variables.
            $innerExceptions = [System.Collections.Generic.List[System.String]]::new()
            $errInnerException = $ErrorRecord.Exception.InnerException

            # Get all inner exceptions.
            while ($errInnerException)
            {
                # Add a divider if we've already added a record.
                if ($innerExceptions.Count)
                {
                    $innerExceptions.Add("`n$('~' * 40)`n")
                }

                # Add error record and get next inner exception.
                $innerExceptions.Add(($errInnerException | Select-Object -Property ($errInnerException | Get-ErrorPropertyNames) | Format-List | Out-String -Width ([System.Int32]::MaxValue)).Trim())
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
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
