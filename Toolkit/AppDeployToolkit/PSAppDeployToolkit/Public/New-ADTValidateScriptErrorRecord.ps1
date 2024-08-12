#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function New-ADTValidateScriptErrorRecord
{
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
        [System.String]$ExceptionMessage
    )

    # Build out new ErrorRecord and return it.
    $naerParams = @{
        Exception = [System.ArgumentException]::new($ExceptionMessage, $ParameterName)
        Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
        ErrorId = "Invalid$($ParameterName)ParameterValue"
        TargetObject = $ProvidedValue
        TargetName = $ProvidedValue
        TargetType = $(if ($null -ne $ProvidedValue) {$ProvidedValue.GetType().Name})
        RecommendedAction = "Review the supplied $($ParameterName) parameter value and try again."
    }
    return (New-ADTErrorRecord @naerParams)
}
