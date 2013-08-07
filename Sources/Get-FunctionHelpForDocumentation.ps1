$helpers = @()
$helpFunctions = Get-Command -CommandType Function | Where {$_.HelpUri -match "psappdeploytoolkit" -and $_.Definition -notmatch "internal script function"} | Select Name -ExpandProperty Name 
Foreach ($help in $helpFunctions) {
    $helpDetail = Get-Help $help -Detailed | Select Name,Synopsis,Description,Parameters,Examples
    $helpers += [pscustomobject][ordered]@{
        Name = $helpDetail | Select Name -ExpandProperty Name -ErrorAction SilentlyContinue
        Synopsis = $helpDetail | Select Synopsis -ExpandProperty Synopsis -ErrorAction SilentlyContinue        
        Description = $helpDetail | Select Description -ExpandProperty Description -ErrorAction SilentlyContinue | Out-String         
        Parameter = $helpDetail | Select Parameters -ExpandProperty Parameters -ErrorAction SilentlyContinue | Select Parameter -ExpandProperty Parameter -ErrorAction SilentlyContinue | Foreach-Object { $_ | Select Name -ExpandProperty Name; $_ | Select Description -ExpandProperty Description}  | Out-String  
        Examples = $helpDetail | Select Examples -ExpandProperty Examples -ErrorAction SilentlyContinue | Out-String 
    }
}

$file = "C:\Temp\functions.txt"

$helpers | Out-File $file
(Get-Content $file) | ? {$_.trim() -ne "" } | Set-Content $file -Force
