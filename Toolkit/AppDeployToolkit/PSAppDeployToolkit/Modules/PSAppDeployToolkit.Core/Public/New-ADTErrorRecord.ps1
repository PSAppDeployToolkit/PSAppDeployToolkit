function New-ADTErrorRecord
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateScript({
            if (($_ -isnot [System.Exception]) -and ($Script:ExceptionNames -notcontains $_))
            {
                $naerParams = @{
                    Exception = [System.ArgumentException]::new('The supplied value is not a valid Exception.', 'Exception')
                    Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                    ErrorId = 'InvalidExceptionParameterValue'
                    TargetObject = $_
                    TargetName = $_
                    TargetType = $(if ($null -ne $_) {$_.GetType().Name})
                    RecommendedAction = 'Review the supplied Exception parameter value and try again.'
                }
                $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
            }
            return !!$_
        })]
        [System.Object]$Exception,

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

    dynamicparam {
        # Add in extra parameters when Exception is not strongly provided.
        if ($Exception -is [System.Exception])
        {
            return
        }

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in the mandatory Message for the Exception to instantiate.
        $paramDictionary.Add('Message', [System.Management.Automation.RuntimeDefinedParameter]::new(
            'Message', [System.String], [System.Collections.Generic.List[System.Attribute]]@(
                [System.Management.Automation.ParameterAttribute]@{Mandatory = $true}
                [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
            )
        ))

        # Add in the optional InnerException for the Exception to instantiate.
        $paramDictionary.Add('InnerException', [System.Management.Automation.RuntimeDefinedParameter]::new(
            'InnerException', [System.Exception], [System.Collections.Generic.List[System.Attribute]]@(
                [System.Management.Automation.ParameterAttribute]@{Mandatory = $false}
                [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
            )
        ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin {
        # Redefine Exception if not strongly provided.
        if ($Exception -isnot [System.Exception])
        {
            $Exception = if ($PSBoundParameters.ContainsKey('InnerException'))
            {
                New-Object -TypeName $Exception -ArgumentList $PSBoundParameters.Message, $PSBoundParameters.InnerException
            }
            else
            {
                New-Object -TypeName $Exception -ArgumentList $PSBoundParameters.Message
            }
        }
    }

    end {
        # Instantiate new ErrorRecord object and populate it.
        $errRecord = [System.Management.Automation.ErrorRecord]::new($Exception, $ErrorId, $Category, $TargetObject)
        $errRecord.CategoryInfo.Activity = $Activity
        $errRecord.CategoryInfo.TargetName = $TargetName
        $errRecord.CategoryInfo.TargetType = $TargetType
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
}

Register-ArgumentCompleter -CommandName New-ADTErrorRecord -ParameterName Exception -ScriptBlock {$Script:ExceptionNames -like "$($args[2])*"}
