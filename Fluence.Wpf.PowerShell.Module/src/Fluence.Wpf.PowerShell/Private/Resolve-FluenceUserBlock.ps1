<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>


function Resolve-FluenceUserBlock
{
    <#
    .SYNOPSIS
        Selects the live user scriptblock when the UI thread shares the caller's runspace, otherwise
        recreates it from text so it binds to the UI runspace's module session state.
    .DESCRIPTION
        On an inline-STA or runspace-sharing foreign host the live block is returned with closures and
        caller scope intact. On an MTA host the UI thread is a separate module-owned runspace, so the
        block is recreated from its text (closures and caller-scope references are not available there).
    .PARAMETER LiveBlock
        The original scriptblock as transported (a live object; valid only on the caller's runspace).
    .PARAMETER BlockText
        The .ToString() text of the block, captured at the caller.
    .PARAMETER CallerRunspaceId
        The InstanceId of the runspace that called Show-FluenceWindow.
    .NOTES
        Runs on the UI thread inside Set-FluenceWindowContent.
    #>
    [CmdletBinding()]
    [OutputType([scriptblock])]
    param
    (
        [Parameter()]
        [scriptblock]$LiveBlock,

        [Parameter()]
        [string]$BlockText,

        [Parameter(Mandatory = $true)]
        [guid]$CallerRunspaceId
    )

    $current = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace
    if ($null -ne $current -and $current.InstanceId -eq $CallerRunspaceId -and $null -ne $LiveBlock)
    {
        return $LiveBlock
    }
    if ([string]::IsNullOrWhiteSpace($BlockText))
    {
        return $null
    }
    return [scriptblock]::Create($BlockText)
}
