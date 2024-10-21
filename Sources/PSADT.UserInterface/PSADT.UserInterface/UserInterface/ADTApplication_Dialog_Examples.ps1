# Import the necessary .NET assemblies
Add-Type -Path "C:\Path\To\PSADT.UserInterface.dll"

# Create an instance of the ProcessEvaluationService
$processEvaluationService = New-Object PSADT.UserInterface.Services.ProcessEvaluationService

# Define functions to demonstrate each dialog using UnifiedAdtApplication

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
        DeferRemainText   = "remain"
        DeferButtonText   = "Defer"
        ContinueButtonText= "Continue"
    }

    try {
        # Show Welcome Dialog using UnifiedAdtApplication
        $result = [PSADT.UserInterface.UnifiedAdtApplication]::ShowWelcomeDialog(
            $params.AppTitle,
            $params.Subtitle,
            $params.TopMost,
            $params.DefersRemaining,
            $params.AppsToClose,
            $params.AppIconImage,
            $params.BannerImageLight,
            $params.BannerImageDark,
            $params.CloseAppMessage,
            $params.DeferRemainText,
            $params.DeferButtonText,
            $params.ContinueButtonText,
            $processEvaluationService
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
        AppTitle              = "Progress Dialog Example"
        Subtitle              = "PSADT User Interface"
        TopMost               = $true
        AppIconImage          = "C:\Path\To\Icon.ico"
        BannerImageLight      = "C:\Path\To\BannerLight.png"
        BannerImageDark       = "C:\Path\To\BannerDark.png"
        ProgressMessage       = "Starting installation..."
        ProgressMessageDetail = "Preparing for installation."
    }

    try {
        # Show the Progress Dialog using UnifiedAdtApplication
        [PSADT.UserInterface.UnifiedAdtApplication]::ShowProgressDialog(
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
            @{ Percent = 10; Message = "Downloading files..."; Detail = "Step 1 of 5" },
            @{ Percent = 30; Message = "Installing components..."; Detail = "Step 2 of 5" },
            @{ Percent = 40; Message = "Configuring settings..."; Detail = "Step 3 of 5" },
            @{ Percent = 80; Message = "Updating registry..."; Detail = "Step 4 of 5" },
            @{ Percent = 100; Message = "Installation complete!"; Detail = "Step 5 of 5" }
        )

        foreach ($step in $steps) {
            # Update progress
            [PSADT.UserInterface.UnifiedAdtApplication]::UpdateProgress(
                $step.Percent,
                $step.Message,
                $step.Detail
            )
            Start-Sleep -Seconds 2

            # Note: Removed SetIndeterminateProgress as it's not implemented in UnifiedAdtApplication
        }
    }
    catch {
        Write-Error "Error in Progress Dialog: $_"
    }
    finally {
        # Ensure the Progress Dialog is closed
        [PSADT.UserInterface.UnifiedAdtApplication]::CloseCurrentDialog()
    }
}

# Example 3: Show Custom Dialog
function Show-CustomDialogExample {
    $params = @{
        AppTitle         = "Custom Dialog Example"
        Subtitle         = "PSADT User Interface"
        TopMost          = $true
        AppIconImage     = "C:\Path\To\Icon.ico"
        BannerImageLight = "C:\Path\To\BannerLight.png"
        BannerImageDark  = "C:\Path\To\BannerDark.png"
        CustomMessage    = "Do you want to proceed with the installation?"
        Button1Text      = "Yes"
        Button2Text      = "No"
        Button3Text      = "More Info"
    }

    try {
        # Show Custom Dialog using UnifiedAdtApplication
        $result = [PSADT.UserInterface.UnifiedAdtApplication]::ShowCustomDialog(
            $params.AppTitle,
            $params.Subtitle,
            $params.TopMost,
            $params.AppIconImage,
            $params.BannerImageLight,
            $params.BannerImageDark,
            $params.CustomMessage,
            $params.Button1Text,
            $params.Button2Text,
            $params.Button3Text
        )

        Write-Host "Custom Dialog Result: $result"

        if ($result -eq "More Info") {
            $additionalInfo = "This installation will update your software to the latest version. It may take up to 10 minutes."
            [PSADT.UserInterface.UnifiedAdtApplication]::ShowCustomDialog(
                "Additional Information",
                $params.Subtitle,
                $params.TopMost,
                $params.AppIconImage,
                $params.BannerImageLight,
                $params.BannerImageDark,
                $additionalInfo,
                "OK", "", ""
            )
        }
    }
    catch {
        Write-Error "Error in Custom Dialog: $_"
    }
}

# Example 4: Show Restart Dialog
function Show-RestartDialogExample {
    $params = @{
        AppTitle            = "Restart Dialog Example"
        Subtitle            = "PSADT User Interface"
        TopMost             = $true
        AppIconImage        = "C:\Path\To\Icon.ico"
        BannerImageLight    = "C:\Path\To\BannerLight.png"
        BannerImageDark     = "C:\Path\To\BannerDark.png"
        RestartCountdownMins= 5
        RestartMessage      = "The installation will begin in 5 minutes. You can restart your computer now or wait for the countdown to complete."
        DismissButtonText   = "Dismiss"
        RestartButtonText   = "Restart Now"
    }

    try {
        # Show Restart Dialog using UnifiedAdtApplication
        $result = [PSADT.UserInterface.UnifiedAdtApplication]::ShowRestartDialog(
            $params.AppTitle,
            $params.Subtitle,
            $params.TopMost,
            $params.AppIconImage,
            $params.BannerImageLight,
            $params.BannerImageDark,
            $params.RestartCountdownMins,
            $params.RestartMessage,
            $params.DismissButtonText,
            $params.RestartButtonText
        )

        Write-Host "Restart Dialog Result: $result"

        if ($result -eq "Restart") {
            Write-Host "Proceeding with installation after restart."
            # Implement actual restart logic here, e.g., triggering a system restart
            # Example:
            # Start-Process "shutdown.exe" -ArgumentList "/r /t 0" -NoNewWindow -Wait
        }
        elseif ($result -eq "Defer") {
            Write-Host "Installation deferred by the user."
        }
    }
    catch {
        Write-Error "Error in Restart Dialog: $_"
    }
}

# Example 5: Complex scenario combining multiple dialogs
function Show-ComplexScenario {
    try {
        # Show Welcome Dialog
        $welcomeResult = [PSADT.UserInterface.UnifiedAdtApplication]::ShowWelcomeDialog(
            "Complex Installation Scenario",
            "Multiple Dialog Example",
            $true,
            2,
            @(
                [PSADT.UserInterface.Services.AppProcessInfo]@{
                    ProcessName        = "notepad"
                    ProcessDescription = "Notepad"
                },
                [PSADT.UserInterface.Services.AppProcessInfo]@{
                    ProcessName        = "chrome"
                    ProcessDescription = "Google Chrome"
                }
            ),
            "C:\Path\To\Icon.ico",
            "C:\Path\To\BannerLight.png",
            "C:\Path\To\BannerDark.png",
            "Please close any open applications before proceeding.",
            "remain",
            "Defer",
            "Start Installation",
            $processEvaluationService
        )

        if ($welcomeResult -eq "Defer") {
            Write-Host "Installation deferred."
            return
        }

        # Show Custom Dialog for installation options
        $customResult = [PSADT.UserInterface.UnifiedAdtApplication]::ShowCustomDialog(
            "Installation Options",
            "PSADT User Interface",
            $true,
            "C:\Path\To\Icon.ico",
            "C:\Path\To\BannerLight.png",
            "C:\Path\To\BannerDark.png",
            "Choose your installation type:",
            "Full",
            "Minimal",
            "Custom"
        )

        # Show Progress Dialog
        [PSADT.UserInterface.UnifiedAdtApplication]::ShowProgressDialog(
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
        [PSADT.UserInterface.UnifiedAdtApplication]::CloseCurrentDialog()

        # Show Completion Dialog
        [PSADT.UserInterface.UnifiedAdtApplication]::ShowCustomDialog(
            "Installation Complete",
            "PSADT User Interface",
            $true,
            "C:\Path\To\Icon.ico",
            "C:\Path\To\BannerLight.png",
            "C:\Path\To\BannerDark.png",
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

    # Initialize progress
    [PSADT.UserInterface.UnifiedAdtApplication]::UpdateProgress(
        0,
        "Starting $Prefix installation...",
        "This may take up to $([math]::Ceiling($TotalSteps / 10)) minutes"
    )

    for ($i = 1; $i -le 100; $i += $StepSize) {
        $stepNumber = [math]::Ceiling($i / $StepSize)
        [PSADT.UserInterface.UnifiedAdtApplication]::UpdateProgress(
            $i,
            "$Prefix installation in progress...",
            "Step $stepNumber of $TotalSteps"
        )
        Start-Sleep -Milliseconds 100
    }
}

# Run the examples
try {
    Show-WelcomeDialogExample
    Show-ProgressDialogExample
    Show-CustomDialogExample
    Show-RestartDialogExample
    Show-ComplexScenario
}
catch {
    Write-Error "An error occurred during script execution: $_"
}
finally {
    # Dispose of the UnifiedAdtApplication to shut down the WPF Application
    [PSADT.UserInterface.UnifiedAdtApplication]::Dispose()
}
