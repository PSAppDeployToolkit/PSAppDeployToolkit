$helpers = @()
$helpFunctions = Get-Command -CommandType Function |
    Where-Object -FilterScript { $_.HelpUri -match "psappdeploytoolkit" -and $_.Definition -notmatch "internal script function"} |
    Select-Object -Property Name -ExpandProperty Name 
Foreach ($help in $helpFunctions) {
    $helpDetail = Get-Help -Name $help -Detailed | Select-Object -Property Name,Synopsis,Description,Parameters,Examples
    $helpers += [pscustomobject][ordered]@{
        Name = $helpDetail | Select-Object -Property Name -ExpandProperty Name -ErrorAction SilentlyContinue
        Synopsis = $helpDetail | Select-Object -Property Synopsis -ExpandProperty Synopsis -ErrorAction SilentlyContinue        
        Description = $helpDetail | Select-Object -Property Description -ExpandProperty Description -ErrorAction SilentlyContinue | Out-String         
        Parameter = $helpDetail | Select-Object -Property Parameters -ExpandProperty Parameters -ErrorAction SilentlyContinue |
            Select-Object -Property Parameter -ExpandProperty Parameter -ErrorAction SilentlyContinue |
            Foreach-Object -Process { $_ | Select-Object -Property Name -ExpandProperty Name; $_ | Select-Object -Property Description -ExpandProperty Description}  | Out-String  
        Examples = $helpDetail | Select-Object -Property Examples -ExpandProperty Examples -ErrorAction SilentlyContinue | Out-String 
    }
}

$file = "C:\Temp\functions.txt"

$helpers | Out-File -FilePath $file
(Get-Content -path $file) | Where-Object -FilterScript {$_.trim() -ne "" } | Set-Content -Path $file -Force
