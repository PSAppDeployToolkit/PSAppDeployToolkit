#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function New-ADTErrorRecord
{
    <#

    .NOTES
    This function can be called without an active ADT session.

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = "This function does not change system state.")]
    [CmdletBinding(SupportsShouldProcess = $false)]
    [OutputType([System.Management.Automation.ErrorRecord])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Exception]$Exception,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ErrorCategory]$Category,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ErrorId = 'NotSpecified',

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [System.Object]$TargetObject,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$TargetName,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$TargetType,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Activity,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Reason,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$RecommendedAction
    )

    # Instantiate new ErrorRecord object.
    $errRecord = [System.Management.Automation.ErrorRecord]::new($Exception, $ErrorId, $Category, $TargetObject)

    # Add in all optional values, if specified.
    if ($Activity)
    {
        $errRecord.CategoryInfo.Activity = $Activity
    }
    if ($TargetName)
    {
        $errRecord.CategoryInfo.TargetName = $TargetName
    }
    if ($TargetType)
    {
        $errRecord.CategoryInfo.TargetType = $TargetType
    }
    if ($Reason)
    {
        $errRecord.CategoryInfo.Reason = $Reason
    }
    if ($RecommendedAction)
    {
        $errRecord.ErrorDetails = [System.Management.Automation.ErrorDetails]::new($errRecord.Exception.Message)
        $errRecord.ErrorDetails.RecommendedAction = $RecommendedAction
    }

    # Return the ErrorRecord to the caller, who will then throw it.
    return $errRecord
}
