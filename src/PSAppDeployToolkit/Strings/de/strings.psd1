@{
    BalloonTip = @{
        Start = @{
            Install = 'Installation gestartet.'
            Repair = 'Reparatur gestartet.'
            Uninstall = 'Deinstallation gestartet.'
        }
        Complete = @{
            Install = 'Installation abgeschlossen.'
            Repair = 'Reparatur abgeschlossen.'
            Uninstall = 'Deinstallation abgeschlossen.'
        }
        RestartRequired = @{
            Install = 'Installation abgeschlossen. Neustart erforderlich.'
            Repair = 'Reparatur abgeschlossen. Neustart erforderlich.'
            Uninstall = 'Deinstallation abgeschlossen. Neustart erforderlich.'
        }
        FastRetry = @{
            Install = 'Installation nicht abgeschlossen.'
            Repair = 'Reparatur nicht abgeschlossen.'
            Uninstall = 'Deinstallation nicht abgeschlossen.'
        }
        Error = @{
            Install = 'Installation fehlgeschlagen.'
            Repair = 'Reparatur fehlgeschlagen.'
            Uninstall = 'Deinstallation fehlgeschlagen.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Installationsvorgang abgeschlossen werden kann.'
            Repair = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Reparaturvorgang abgeschlossen werden kann.'
            Uninstall = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Deinstallationsvorgang abgeschlossen werden kann.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installieren'
            Repair = 'PSAppDeployToolkit - App Reparatur'
            Uninstall = 'PSAppDeployToolkit - App Deinstallieren'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Sie haben nicht genügend Speicherplatz, um die Installation abzuschließen:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie genügend Speicherplatz frei, um mit der Installation fortzufahren."
            Repair = "Sie haben nicht genügend Speicherplatz, um die Reparatur von:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie ausreichend Speicherplatz frei, um mit der Reparatur fortzufahren."
            Uninstall = "Sie haben nicht genügend Speicherplatz, um die Deinstallation von:`n{0}`n`nSpace abzuschließen. Erforderlich: {1} MB`nVerfügbar: {2} MB`n`nBitte geben Sie ausreichend Speicherplatz frei, um mit der Deinstallation fortzufahren."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installieren'
            Repair = 'PSAppDeployToolkit - App Reparatur'
            Uninstall = 'PSAppDeployToolkit - App Deinstallieren'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Installation wird ausgeführt. Bitte warten...'
            Repair = 'Reparatur wird ausgeführt. Bitte warten...'
            Uninstall = 'Deinstallation wird ausgeführt. Bitte warten...'
        }
        MessageDetail = @{
            Install = 'Dieses Fenster wird automatisch geschlossen, wenn die Installation abgeschlossen ist.'
            Repair = 'Dieses Fenster wird automatisch geschlossen, wenn die Reparatur abgeschlossen ist.'
            Uninstall = 'Dieses Fenster wird automatisch geschlossen, wenn die Deinstallation abgeschlossen ist.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installieren'
            Repair = 'PSAppDeployToolkit - App Reparatur'
            Uninstall = 'PSAppDeployToolkit - App Deinstallieren'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimieren'
        ButtonRestartNow = 'Jetzt neu starten'
        Message = @{
            Install = 'Damit die Installation abgeschlossen werden kann, müssen Sie Ihren Computer neu starten.'
            Repair = 'Damit die Reparatur abgeschlossen werden kann, müssen Sie Ihren Computer neu starten.'
            Uninstall = 'Damit die Deinstallation abgeschlossen werden kann, müssen Sie Ihren Computer neu starten.'
        }
        CustomMessage = ''
        MessageRestart = 'Ihr Computer wird am Ende des Countdowns automatisch neu gestartet.'
        MessageTime = 'Bitte speichern Sie Ihre Arbeit und starten Sie innerhalb der vorgegebenen Zeit neu.'
        TimeRemaining = 'Restzeit:'
        Title = 'Neustart erforderlich'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - App Installieren'
            Repair = 'PSAppDeployToolkit - App Reparatur'
            Uninstall = 'PSAppDeployToolkit - App Deinstallieren'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'Die folgende Anwendung wird installiert:'
                Repair = 'Die folgende Anwendung wird repariert:'
                Uninstall = 'Die folgende Anwendung wird deinstalliert:'
            }
            CloseAppsMessage = @{
                Install = "Die folgenden Programme müssen geschlossen werden, bevor die Installation fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Programme und fahren Sie dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Programme schließen`“ klicken."
                Repair = "Die folgenden Programme müssen geschlossen werden, bevor die Reparatur fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Programme und fahren Sie dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Programme schließen`“ klicken."
                Uninstall = "Die folgenden Programme müssen geschlossen werden, bevor die Deinstallation fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Programme und fahren Sie dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Programme schließen`“ klicken."
            }
            ExpiryMessage = @{
                Install = 'Sie können die Installation zurückstellen, bis die Zurückstellung abläuft:'
                Repair = 'Sie können die Reparatur verschieben, bis die Verschiebung abläuft:'
                Uninstall = 'Sie können die Deinstallation verschieben, bis die Verschiebung abläuft:'
            }
            DeferralsRemaining = 'Verbleibende Verschiebungen:'
            DeferralDeadline = 'Frist:'
            ExpiryWarning = 'Nach Ablauf der Verschiebung haben Sie keine Möglichkeit mehr, die Verschiebung zu nutzen.'
            CountdownDefer = @{
                Install = 'Die Installation wird automatisch fortgesetzt in:'
                Repair = 'Die Reparatur wird automatisch fortgesetzt in:'
                Uninstall = 'Die Deinstallation wird automatisch fortgesetzt in:'
            }
            CountdownClose = 'HINWEIS: Die Programme werden automatisch geschlossen in:'
            ButtonClose = 'Programme &schließen'
            ButtonDefer = '&Aufschieben'
            ButtonContinue = '&Weiter'
            ButtonContinueTooltip = 'Wählen Sie erst „Weiter“, nachdem Sie die oben aufgeführten Anwendungen geschlossen haben.'
        }
        Fluent = @{
            DialogMessage = 'Bitte speichern Sie Ihre Arbeit, bevor Sie fortfahren, da die folgenden Anwendungen automatisch geschlossen werden.'
            DialogMessageNoProcesses = @{
                Install = 'Wählen Sie Installieren aus, um mit der Installation fortzufahren. Wenn Sie noch Aufschübe haben, können Sie die Installation auch verschieben.'
                Repair = 'Wählen Sie Reparieren aus, um mit der Reparatur fortzufahren. Wenn Sie noch Aufschübe haben, können Sie auch die Reparatur aufschieben.'
                Uninstall = 'Wählen Sie Deinstallieren aus, um mit der Deinstallation fortzufahren. Wenn Sie noch Aufschübe haben, können Sie auch die Deinstallation aufschieben.'
            }
            AutomaticStartCountdown = 'Automatischer Start-Countdown'
            DeferralsRemaining = 'Verbleibende Stundungen'
            DeferralDeadline = 'Frist für die Stundung'
            ButtonLeftText = 'Aufschieben'
            ButtonRightText = @{
                Install = 'Schließen Sie Apps & Installieren Sie'
                Repair = 'Schließen Sie Apps & Reparieren Sie'
                Uninstall = 'Schließen Sie Apps & Deinstallieren Sie'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Installieren'
                Repair = 'Reparatur'
                Uninstall = 'Deinstallieren'
            }
            Subtitle = @{
                Install = 'PSAppDeployToolkit - App Installieren'
                Repair = 'PSAppDeployToolkit - App Reparatur'
                Uninstall = 'PSAppDeployToolkit - App Deinstallieren'
            }
        }
        CustomMessage = ''
    }
}
