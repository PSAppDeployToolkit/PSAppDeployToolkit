Try {
	# Check if we are running in Session 0
	If (([System.Diagnostics.Process]::GetCurrentProcess() | Select "SessionID" -ExpandProperty "SessionID") -eq 0) { 
		Exit 1 # Running in Session 0 
	}
	Else {
		Exit 0 # Not running in Session 0
	}
}
Catch {
	Exit 2 # Failure
}