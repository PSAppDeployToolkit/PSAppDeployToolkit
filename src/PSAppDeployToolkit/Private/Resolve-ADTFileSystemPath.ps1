#-----------------------------------------------------------------------------
#
# MARK: Resolve-ADTFileSystemPath
#
#-----------------------------------------------------------------------------

function Private:Resolve-ADTFileSystemPath
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Container')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Leaf')]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [System.String]$LiteralPath,

        [Parameter(Mandatory = $true, ParameterSetName = 'Container')]
        [System.Management.Automation.SwitchParameter]$Directory,

        [Parameter(Mandatory = $true, ParameterSetName = 'Leaf')]
        [System.Management.Automation.SwitchParameter]$File,

        [Parameter(Mandatory = $false, ParameterSetName = 'Container')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Leaf')]
        [System.Management.Automation.SwitchParameter]$ResolveOnly
    )

    dynamicparam
    {
        # Return early if we've been given directory input.
        if ($PSBoundParameters.ContainsKey('Directory'))
        {
            return
        }

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in the optional file parameters.
        $paramDictionary.Add('ExtraPaths', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'ExtraPaths', [System.String[]], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false }
                    [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpaceAttribute]::new()
                )
            ))
        $paramDictionary.Add('DefaultExtension', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'DefaultExtension', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false }
                    [System.Management.Automation.ValidateScriptAttribute]::new({
                            if ([System.String]::IsNullOrWhiteSpace($_))
                            {
                                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DefaultExtension -ProvidedValue $_ -ExceptionMessage 'The specified Extension cannot be null, empty, or whitespace.'))
                            }
                            if (!$_.StartsWith('.'))
                            {
                                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DefaultExtension -ProvidedValue $_ -ExceptionMessage 'The specified Extension does not start with the required period.'))
                            }
                            return $true
                        })
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Internal worker function to abstract repeated boilerplate.
        function Resolve-ADTFileSystemPathImpl
        {
            [CmdletBinding()]
            param
            (
                [Parameter(Mandatory = $true)]
                [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
                [System.String]$FileSystemPath,

                [Parameter(Mandatory = $true)]
                [ValidateSet('Container', 'Leaf')]
                [System.String]$PathType
            )

            dynamicparam
            {
                # Return early if we've been given directory input.
                if ($PSBoundParameters.PathType -eq 'Container')
                {
                    return
                }

                # Define parameter dictionary for returning at the end.
                $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

                # Add in the optional file parameters.
                $paramDictionary.Add('DefaultExtension', [System.Management.Automation.RuntimeDefinedParameter]::new(
                        'DefaultExtension', [System.String], $(
                            [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false }
                            [System.Management.Automation.ValidateScriptAttribute]::new({
                                    if ([System.String]::IsNullOrWhiteSpace($_))
                                    {
                                        $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DefaultExtension -ProvidedValue $_ -ExceptionMessage 'The specified Extension cannot be null, empty, or whitespace.'))
                                    }
                                    if (!$_.StartsWith('.'))
                                    {
                                        $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName DefaultExtension -ProvidedValue $_ -ExceptionMessage 'The specified Extension does not start with the required period.'))
                                    }
                                    return $true
                                })
                        )
                    ))

                # Return the populated dictionary.
                return $paramDictionary
            }

            end
            {
                # Return the given path if it exists, otherwise test again with an extension added if a default one's been provided.
                if (Test-Path -LiteralPath $FileSystemPath -PathType $PathType)
                {
                    return $FileSystemPath
                }
                if (($PathType -eq 'Leaf') -and $PSBoundParameters.ContainsKey('DefaultExtension') -and ![System.IO.Path]::HasExtension($FileSystemPath) -and (Test-Path -LiteralPath ($fileSystemPathWithExtension = $FileSystemPath + $PSBoundParameters.DefaultExtension) -PathType $PathType))
                {
                    return $fileSystemPathWithExtension
                }
            }
        }

        # Initialise the function and set up parameters for Resolve-ADTFileSystemPathImpl.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $rafspiParams = @{ PathType = $PSCmdlet.ParameterSetName }
        if ($PSBoundParameters.ContainsKey('DefaultExtension'))
        {
            $rafspiParams.Add('DefaultExtension', $PSBoundParameters.DefaultExtension)
        }

        # Establish the search paths.
        $searchPaths = $(
            # Extra paths are searched first and foremost.
            if ($PSBoundParameters.ContainsKey('ExtraPaths'))
            {
                foreach ($extraPath in $PSBoundParameters.ExtraPaths)
                {
                    Resolve-ADTFileSystemPath -LiteralPath $extraPath -Directory
                }
            }

            # PowerShell's current path comes next. We avoid the use of $PWD
            # as it is not immutable and can be changed globally by callers.
            $ExecutionContext.SessionState.Path.CurrentFileSystemLocation.ProviderPath

            # Session paths come next.
            if ($adtSession = if (Test-ADTSessionActive) { Get-ADTSession })
            {
                if ($adtSession.DirFiles)
                {
                    $adtSession.DirFiles.FullName
                }
                if ($adtSession.DirSupportFiles)
                {
                    $adtSession.DirSupportFiles.FullName
                }
                if ($adtSession.ScriptDirectory.Count -gt 0)
                {
                    $adtSession.ScriptDirectory.FullName
                }
            }

            # The system's path variable, split and validated for valid entries.
            [PSADT.Utilities.EnvironmentUtilities]::GetEnvironmentVariable('PATH').Split([System.IO.Path]::PathSeparator, [System.StringSplitOptions]::RemoveEmptyEntries)
        )

        # Sanitise the search paths before use.
        $searchPaths = $searchPaths | & { process { if (![System.String]::IsNullOrWhiteSpace($_)) { return $_.Trim() } } } | Select-Object -Unique
    }

    process
    {
        try
        {
            try
            {
                # Strip off any provider qualifiers if they're present.
                if ($ExecutionContext.SessionState.Path.IsProviderQualified($LiteralPath))
                {
                    $providerDivider = $LiteralPath.IndexOf('::'); $providerText = $LiteralPath.Substring(0, $providerDivider)
                    if (($providerText -ne 'FileSystem') -and ($providerText -ne 'Microsoft.PowerShell.Core\FileSystem'))
                    {
                        $naerParams = @{
                            Exception = [System.FormatException]::new("The specified provider-qualified path [$LiteralPath] is not a FileSystem path.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                            ErrorId = 'ProviderQualifiedPathNotFileSystemPath'
                            TargetObject = $LiteralPath
                            RecommendedAction = "Please review the provided input and try again."
                        }
                        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                    }
                    $LiteralPath = $LiteralPath.Substring($providerDivider + 2)
                }

                # Just return what we're given if it's already fully qualified.
                if ([PSADT.FileSystem.FileSystemUtilities]::IsPathFullyQualified($LiteralPath) -and ($verifiedPath = Resolve-ADTFileSystemPathImpl -FileSystemPath $LiteralPath @rafspiParams))
                {
                    return $verifiedPath
                }

                # Attempt to resolve the path to a fully qualified one.
                $resolvedPath = try
                {
                    $PSCmdlet.GetUnresolvedProviderPathFromPSPath($LiteralPath)
                }
                catch
                {
                    $PSCmdlet.ThrowTerminatingError($_)
                }

                # Return early if we were just to resolve it.
                if ($ResolveOnly)
                {
                    return $resolvedPath
                }

                # Attempt simple resolution before trying something more complex.
                if ($verifiedPath = Resolve-ADTFileSystemPathImpl -FileSystemPath $resolvedPath @rafspiParams)
                {
                    return $verifiedPath
                }

                # Process all unique search paths after sanitising the values, returning the first successful match.
                foreach ($searchPath in $searchPaths)
                {
                    if ($verifiedPath = Resolve-ADTFileSystemPathImpl -FileSystemPath ([System.IO.Path]::Combine($searchPath, $LiteralPath)) @rafspiParams)
                    {
                        return $verifiedPath
                    }
                }

                # Throw if we weren't able to resolve the path to an existing file system object.
                if ($OriginalErrorAction -match '^(SilentlyContinue|Ignore)$')
                {
                    return
                }
                $naerParams = @{
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'LiteralPathNotFound'
                    TargetObject = [pscustomobject]@{
                        LiteralPath = $LiteralPath
                        ResolvedPath = $resolvedPath
                    }
                    RecommendedAction = "Please review the provided input and try again."
                }
                switch ($PSCmdlet.ParameterSetName)
                {
                    Container
                    {
                        $naerParams.Add('Exception', [System.IO.DirectoryNotFoundException]::new("The specified directory [$LiteralPath] could not be resolved to an existing path."))
                    }
                    Leaf
                    {
                        $naerParams.Add('Exception', [System.IO.FileNotFoundException]::new("The specified file [$LiteralPath] could not be resolved to an existing path.", $LiteralPath))
                    }
                    default
                    {
                        $naerParams2 = @{
                            Exception = [System.InvalidOperationException]::new("The function [$($MyInvocation.MyCommand.Name)] is not testing all configured parameter sets correctly.")
                            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                            ErrorId = 'FunctionInternalSetupError'
                            TargetObject = $MyInvocation.MyCommand
                            RecommendedAction = "Please review the provided input and try again."
                        }
                        throw (New-ADTErrorRecord @naerParams2)
                    }
                }
                throw (New-ADTErrorRecord @naerParams)
            }
            catch
            {
                # Re-writing the ErrorRecord with Write-Error ensures the correct PositionMessage is used.
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            # Process the caught error, log it and throw depending on the specified ErrorAction.
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -Silent
        }
    }

    end
    {
        # Finalize function.
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
