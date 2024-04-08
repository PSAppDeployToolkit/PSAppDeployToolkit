#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

# Dot-source all submodules.
(Get-ChildItem -Path $PSScriptRoot\Submodules\*.ps1).FullName.ForEach({. $_})
