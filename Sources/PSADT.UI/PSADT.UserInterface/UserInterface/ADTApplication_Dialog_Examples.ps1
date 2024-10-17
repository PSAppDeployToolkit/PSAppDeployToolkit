# Import the necessary .NET assemblies
Add-Type -Path "C:\Path\To\PSADT.UserInterface.dll"

# Create an instance of the ProcessEvaluationService
$processEvaluationService = New-Object PSADT.UserInterface.Services.ProcessEvaluationService

# Create an instance of the AdtApplication class
$adtApp = New-Object PSADT.UserInterface.AdtApplication($processEvaluationService)

# Example 1: Show Welcome Dialog
function Show-WelcomeDialogExample {
    $params = @{
        AppTitle          = "Welcome Dialog Example"
        Subtitle          = "PSADT User Interface"
        TopMost           = $true
        DefersRemaining   = 3
        AppsToClose       = @(
            [PSADT.UserInterface.Services.AppProcessInfo]@{
                ProcessName        = "notepad"
                ProcessDescription = "Notepad"
            },
            [PSADT.UserInterface.Services.AppProcessInfo]@{
                ProcessName        = "calc"
                ProcessDescription = "Calculator"
            }
        )
        AppIconImage      = "C:\Path\To\Icon.ico"
        BannerImageLight  = "C:\Path\To\BannerLight.png"
        BannerImageDark   = "C:\Path\To\BannerDark.png"
        CloseAppMessage   = "Please close the following applications:"
        ButtonLeftText    = "Defer"
        ButtonRightText   = "Continue"
    }

    try {
        $result = $adtApp.ShowWelcomeDialog(
            $params.AppTitle,
            $params.Subtitle,
            $params.TopMost,
            $params.DefersRemaining,
            $params.AppsToClose,
            $params.AppIconImage,
            $params.BannerImageLight,
            $params.BannerImageDark,
            $params.CloseAppMessage,
            $params.ButtonLeftText,
            $params.ButtonRightText
        )

        Write-Host "Welcome Dialog Result: $result"
    }
    catch {
        Write-Error "Error in Welcome Dialog: $_"
    }
}

# Example 2: Show Progress Dialog with updates
function Show-ProgressDialogExample {
    $params = @{
        AppTitle             = "Progress Dialog Example"
        Subtitle             = "PSADT User Interface"
        TopMost              = $true
        AppIconImage         = "C:\Path\To\Icon.ico"
        BannerImageLight     = "C:\Path\To\BannerLight.png"
        BannerImageDark      = "C:\Path\To\BannerDark.png"
        ProgressMessage      = "Starting installation..."
        ProgressMessageDetail= "Preparing for installation."
    }

    try {
        # Show the Progress Dialog
        $adtApp.ShowProgressDialog(
            $params.AppTitle,
            $params.Subtitle,
            $params.TopMost,
            $params.AppIconImage,
            $params.BannerImageLight,
            $params.BannerImageDark,
            $params.ProgressMessage,
            $params.ProgressMessageDetail
        )

        # Simulate a complex installation process
        $steps = @(
            @{Percent = 10; Message = "Downloading files..."; Detail = "Step 1 of 5"},
            @{Percent = 30; Message = "Installing components..."; Detail = "Step 2 of 5"},
            @{Percent = 40; Message = "Configuring settings..."; Detail = "Step 3 of 5"},
            @{Percent = 80; Message = "Updating registry..."; Detail = "Step 4 of 5"},
            @{Percent = 100; Message = "Installation complete!"; Detail = "Step 5 of 5"}
        )
         
        foreach ($step in $steps) {
            # Update progress
            $adtApp.UpdateProgress($step.Percent, $step.Message, $step.Detail)
            Start-Sleep -Seconds 2

            if ($step.Percent -eq 30) {
                # Example of setting indeterminate progress if supported
                $adtApp.SetIndeterminateProgress("Extracting files...", "Please wait...")
                Start-Sleep -Seconds 3
            }
        }
    }
    catch {
        Write-Error "Error in Progress Dialog: $_"
    }
    finally {
        # Ensure the Progress Dialog is closed
        $adtApp.CloseCurrentDialog()
    }
}

# Example 3: Show Custom Dialog
function Show-CustomDialogExample {
    $params = @{
        Title         = "Custom Dialog Example"
        Logo          = "C:\Path\To\Logo.png"
        CustomMessage = "Do you want to proceed with the installation?"
        Button1       = "Yes"
        Button2       = "No"
        Button3       = "More Info"
    }

    try {
        $result = $adtApp.ShowCustomDialog(
            $params.Title,
            $params.Logo,
            $params.CustomMessage,
            $params.Button1,
            $params.Button2,
            $params.Button3
        )

        Write-Host "Custom Dialog Result: $result"

        if ($result -eq "More Info") {
            $additionalInfo = "This installation will update your software to the latest version. It may take up to 10 minutes."
            $adtApp.ShowCustomDialog(
                "Additional Information",
                $params.Logo,
                $additionalInfo,
                "OK", "", ""
            )
        }
    }
    catch {
        Write-Error "Error in Custom Dialog: $_"
    }
}

# Example 4: Complex scenario combining multiple dialogs
function Show-ComplexScenario {
    try {
        # Show Welcome Dialog
        $welcomeResult = $adtApp.ShowWelcomeDialog(
            "Complex Installation Scenario",
            "Multiple Dialog Example",
            $true,
            2,
            $null,
            "C:\Path\To\Icon.ico",
            "C:\Path\To\BannerLight.png",
            "C:\Path\To\BannerDark.png",
            "Please close any open applications before proceeding.",
            "Defer",
            "Start Installation"
        )

        if ($welcomeResult -eq "Defer") {
            Write-Host "Installation deferred."
            return
        }

        # Show Custom Dialog for installation options
        $customResult = $adtApp.ShowCustomDialog(
            "Installation Options",
            "C:\Path\To\Logo.png",
            "Choose your installation type:",
            "Full",
            "Minimal",
            "Custom"
        )

        # Show Progress Dialog
        $adtApp.ShowProgressDialog(
            "Installing...",
            "Please wait while we install the software",
            $true,
            "C:\Path\To\Icon.ico",
            "C:\Path\To\BannerLight.png",
            "C:\Path\To\BannerDark.png",
            "Preparing for installation...",
            "This may take several minutes."
        )

        switch ($customResult) {
            "Full" { 
                Perform-Installation -TotalSteps 100 -StepSize 1 -Prefix "Full"
            }
            "Minimal" {
                Perform-Installation -TotalSteps 50 -StepSize 2 -Prefix "Minimal"
            }
            "Custom" {
                Perform-Installation -TotalSteps 20 -StepSize 5 -Prefix "Custom"
            }
        }

        # Close Progress Dialog
        $adtApp.CloseCurrentDialog()

        # Show Completion Dialog
        $adtApp.ShowCustomDialog(
            "Installation Complete",
            "C:\Path\To\Logo.png",
            "The $customResult installation has finished successfully.",
            "OK", "", ""
        )
    }
    catch {
        Write-Error "Error in Complex Scenario: $_"
    }
}

function Perform-Installation {
    param (
        [int]$TotalSteps,
        [int]$StepSize,
        [string]$Prefix
    )

    $adtApp.UpdateProgress(0, "Starting $Prefix installation...", "This may take up to $([math]::Ceiling($TotalSteps / 10)) minutes")
    for ($i = 1; $i -le 100; $i += $StepSize) {
        $stepNumber = [math]::Ceiling($i / $StepSize)
        $adtApp.UpdateProgress($i, "$Prefix installation in progress...", "Step $stepNumber of $TotalSteps")
        Start-Sleep -Milliseconds 100
    }
}

# Run the examples
try {
    Show-WelcomeDialogExample
    Show-ProgressDialogExample
    Show-CustomDialogExample
    Show-ComplexScenario
}
catch {
    Write-Error "An error occurred during script execution: $_"
}
finally {
    # Dispose of the ADTApplication instance to shut down the WPF Application
    $adtApp.Dispose()
}
