#-----------------------------------------------------------------------------
#
# MARK: New-ADTErrorRecord
#
#-----------------------------------------------------------------------------

function New-ADTErrorRecord
{
    <#
    .SYNOPSIS
        Creates a new ErrorRecord object.

    .DESCRIPTION
        This function creates a new ErrorRecord object with the specified exception, error category, and optional parameters. It allows for detailed error information to be captured and returned to the caller, who can then throw the error.

    .PARAMETER Exception
        The exception object that caused the error.

    .PARAMETER Category
        The category of the error.

    .PARAMETER ErrorId
        The identifier for the error. Default is 'NotSpecified'.

    .PARAMETER TargetObject
        The target object that the error is related to.

    .PARAMETER TargetName
        The name of the target that the error is related to.

    .PARAMETER TargetType
        The type of the target that the error is related to.

    .PARAMETER Activity
        The activity that was being performed when the error occurred.

    .PARAMETER Reason
        The reason for the error.

    .PARAMETER RecommendedAction
        The recommended action to resolve the error.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Management.Automation.ErrorRecord

        This function returns an ErrorRecord object.

    .EXAMPLE
        PS C:\>$exception = [System.Exception]::new("An error occurred.")
        PS C:\>$category = [System.Management.Automation.ErrorCategory]::NotSpecified
        PS C:\>New-ADTErrorRecord -Exception $exception -Category $category -ErrorId "CustomErrorId" -TargetObject $null -TargetName "TargetName" -TargetType "TargetType" -Activity "Activity" -Reason "Reason" -RecommendedAction "RecommendedAction"

        Creates a new ErrorRecord object with the specified parameters.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
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
