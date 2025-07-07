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
        ButtonContinueTooltip = "Erst auf `"Weiter`" klicken, nachdem die oben aufgeführten Anwendung(en) geschlossen wurden."
        ButtonDefer = "&Aufschieben"
        CountdownMessage = "HINWEIS: Diese Programme werden automatisch geschlossen in:"
        Message = "Die folgenden Programme müssen geschlossen werden, damit die Installation fortgesetzt werden kann.`n`nArbeit bitte speichern, Programme schließen und dann fortfahren. Alternativ Arbeit speichern und auf `"Programme Schließen`" klicken."
    }
    DeferPrompt = @{
        Deadline = "Frist:"
        ExpiryMessage = "Die Installation kann bis zum Ablauf der Aufschiebefrist verschoben werden:"
        RemainingDeferrals = "Verbleibende Aufschiebungen:"
        WarningMessage = "Nach Ablauf der Frist kann die Installation nicht mehr verschoben werden."
        WelcomeMessage = "Die folgende Anwendung soll installiert werden:"
    }
    DeploymentType = @{
        Install = "Installation"
        Repair = "Reparatur"
        Uninstall = "Deinstallation"
    }
    DiskSpace = @{
        Message = "Es steht nicht genügend freier Speicherplatz zur Verfügung, um die Installation abzuschließen: {0}`n`nBenötigter Platz: {1}MB`nFreier Speicherplatz: {2}MB`n`nBitte ausreichend Speicherplatz freigeben und anschließend die Installation erneut starten."
    }
    Progress = @{
        MessageInstall = "Installation läuft. Bitte warten..."
        MessageInstallDetail = "Dieses Fenster schließt sich automatisch, wenn die Installation abgeschlossen ist."
        MessageRepair = "Reparatur läuft. Bitte warten..."
        MessageRepairDetail = "Dieses Fenster schließt sich automatisch, wenn die Reparatur abgeschlossen ist."
        MessageUninstall = "Deinstallation läuft. Bitte warten..."
        MessageUninstallDetail = "Dieses Fenster schließt sich automatisch, wenn die Deinstallation abgeschlossen ist."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimieren"
        ButtonRestartNow = "Jetzt Neustarten"
        Message = "Damit die Installation abgeschlossen werden kann, muss der Computer neu gestartet werden."
        MessageRestart = "Der Computer wird nach Ablauf des Countdowns automatisch neu gestartet."
        MessageTime = "Arbeit bitte speichern und innerhalb des angegebenen Zeitraums neu starten."
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
            DialogMessage = 'Arbeit bitte speichern, bevor fortgesetzt wird, da die folgenden Anwendungen automatisch geschlossen werden.'
            DialogMessageNoProcesses = 'Zum Fortsetzen Installieren wählen. Sofern noch Aufschiebungen verfügbar sind, kann die Installation auch verschoben werden.'
            ButtonDeferRemaining = 'verbleibend'
            ButtonLeftText = 'Aufschieben'
            ButtonRightText = 'Apps schließen & installieren'
            ButtonRightTextNoProcesses = 'Installieren'
        }
    }
}
