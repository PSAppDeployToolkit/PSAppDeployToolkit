#-----------------------------------------------------------------------------
#
# MARK: Out-ADTString
#
#-----------------------------------------------------------------------------

function Private:Out-ADTString
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseProcessBlockForPipelineCommand', '', Justification = "Formatting data only renders correctly as a set, so the pipeline is taken from `$input in one go rather than an object at a time.")]
    [CmdletBinding()]
    [OutputType([System.String], [System.String[]])]
    param
    (
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [AllowNull()][AllowEmptyString()][AllowEmptyCollection()]
        [System.Management.Automation.PSObject]$InputObject,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Stream
    )

    end
    {
        # Formatting data only renders correctly as a set, so the whole pipeline is taken at once. A value passed by name never reaches $input.
        $objects = if (!$PSCmdlet.MyInvocation.ExpectingInput)
        {
            $InputObject
        }
        else
        {
            $input
        }

        # Process all objects and return valid string results.
        try
        {
            if ([System.String[]]$lines = $objects | Out-String -Width 16383 -Stream | & { process { $_.TrimEnd() } })
            {
                if ($Stream)
                {
                    return $lines
                }
                if (![System.String]::IsNullOrWhiteSpace(($res = [System.String]::Join([System.Environment]::NewLine, $lines))))
                {
                    return $res
                }
            }
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }
}
