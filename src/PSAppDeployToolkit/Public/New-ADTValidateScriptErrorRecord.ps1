#-----------------------------------------------------------------------------
#
# MARK: New-ADTValidateScriptErrorRecord
#
#-----------------------------------------------------------------------------

function New-ADTValidateScriptErrorRecord
{
    <#
    .SYNOPSIS
        Creates a new ErrorRecord for script validation errors.

    .DESCRIPTION
        This function creates a new ErrorRecord object for script validation errors. It takes the parameter name, provided value, exception message, and an optional inner exception to build a detailed error record. This helps in identifying and handling invalid parameter values in scripts.

    .PARAMETER ParameterName
        The name of the parameter that caused the validation error.

    .PARAMETER ProvidedValue
        The value provided for the parameter that caused the validation error.

    .PARAMETER ExceptionMessage
        The message describing the validation error.

    .PARAMETER InnerException
        An optional inner exception that provides more details about the validation error.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Management.Automation.ErrorRecord

        This function returns an ErrorRecord object.

    .EXAMPLE
        $paramName = "FilePath"
        $providedValue = "C:\InvalidPath"
        $exceptionMessage = "The specified path does not exist."
        New-ADTValidateScriptErrorRecord -ParameterName $paramName -ProvidedValue $providedValue -ExceptionMessage $exceptionMessage

        Creates a new ErrorRecord for a validation error with the specified parameters.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = "This function does not change system state.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ParameterName,

        [Parameter(Mandatory = $true)]
        [AllowNull()]
        [System.Object]$ProvidedValue,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ExceptionMessage,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Exception]$InnerException
    )

    # Build out new ErrorRecord and return it.
    $naerParams = @{
        Exception = if ($InnerException)
        {
            [System.ArgumentException]::new($ExceptionMessage, $ParameterName, $InnerException)
        }
        else
        {
            [System.ArgumentException]::new($ExceptionMessage, $ParameterName)
        }
        Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
        ErrorId = "Invalid$($ParameterName)ParameterValue"
        TargetObject = $ProvidedValue
        TargetName = $ProvidedValue
        TargetType = $(if ($null -ne $ProvidedValue) { $ProvidedValue.GetType().Name })
        RecommendedAction = "Review the supplied $($ParameterName) parameter value and try again."
    }
    return (New-ADTErrorRecord @naerParams)
}
