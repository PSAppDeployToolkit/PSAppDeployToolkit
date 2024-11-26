@{
    BalloonText = @{
        Complete = "abgeschlossen."
        Error = "ist fehlgeschlagen."
        FastRetry = "nicht abgeschlossen werden."
        RestartRequired = "abgeschlossen. Ein Neustart ist erforderlich."
        Start = "gestartet."
    }
    BlockExecution = @{
        Message = "Das Starten dieser Anwendung(en) wurde vorübergehend blockiert, damit der Installationsvorgang erfolgreich durchgeführt werden kann."
    }
    ClosePrompt = @{
        ButtonClose = "Programme &schließen"
        ButtonContinue = "&Weiter"
        ButtonContinueTooltip = "Klicken Sie erst auf `"Weiter`", nachdem Sie die obigen Anwendung(en) geschlossen haben."
        ButtonDefer = "&Aufschieben"
        CountdownMessage = "HINWEIS: Diese Programme werden automatisch geschlossen:"
        Message = "Die folgenden Programme müssen geschlossen werden, bevor die Installation fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Programme und fahren Sie dann fort. Alternativ können Sie Ihre Arbeit speichern und dann auf `"Programme Schließen`" klicken."
    }
    DeferPrompt = @{
        Deadline = "Termin:"
        ExpiryMessage = "Sie können die Installation verzögern, bis die Rückstellung abläuft:"
        RemainingDeferrals = "Verbleibende Rückstellungen:"
        WarningMessage = "Sobald die Rückstellung abgelaufen ist, werden Sie keine Möglichkeit mehr haben die Installation zu verschieben."
        WelcomeMessage = "Die folgende Anwendung soll installiert werden:"
    }
    DeploymentType = @{
        Install = "Installation"
        Repair = "Reparatur"
        Uninstall = "Deinstallation"
    }
    DiskSpace = @{
        Message = "Sie haben nicht genug freien Speicherplatz um die Installation abzuschließen: {0}`n`nPlatzbedarf: {1}MB`nFreier Speicherplatz: {2}MB`n`nBitte geben Sie ausreichend Speicherplatz frei, um mit der Installation fortzufahren."
    }
    Progress = @{
        MessageInstall = "Installation wird durchgeführt. Bitte warten..."
        MessageInstallDetail = "Dieses Fenster wird automatisch geschlossen, wenn die Installation abgeschlossen ist."
        MessageRepair = "Reparatur wird durchgeführt. Bitte warten..."
        MessageRepairDetail = "Dieses Fenster wird automatisch geschlossen, wenn die Reparatur abgeschlossen ist."
        MessageUninstall = "Deinstallation wird durchgeführt. Bitte warten..."
        MessageUninstallDetail = "Dieses Fenster wird automatisch geschlossen, wenn die Deinstallation abgeschlossen ist."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimieren"
        ButtonRestartNow = "Jetzt Neustarten"
        Message = "Zum Abschluss der Installation müssen Sie Ihren Computer neu starten."
        MessageRestart = "Am Ende des Countdowns wird Ihr Computer automatisch neu gestartet."
        MessageTime = "Bitte speichern Sie Ihre Arbeit und starten Sie den Computer innerhalb der vorgegebenen Zeit neu."
        TimeRemaining = "Verbleibende Zeit:"
        Title = "Neustart Erforderlich"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "Die {0} wird automatisch fortgesetzt in:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - Anwendung {0}'
            DialogMessage = 'Bitte speichern Sie Ihre Arbeit, bevor Sie fortfahren, da die folgenden Anwendungen automatisch geschlossen werden.'
            DialogMessageNoProcesses = 'Bitte wählen Sie Installieren, um mit der Installation fortzufahren. Wenn Sie noch Aufschübe haben, können Sie die Installation auch aufschieben.'
            ButtonDeferRemaining = 'bleiben'
            ButtonLeftText = 'Aufschieben'
            ButtonRightText = 'Apps schließen & installieren'
            ButtonRightTextNoProcesses = 'Installieren Sie'
        }
    }
}
