@{
    BalloonTip = @{
        Start = @{
            Install = 'Installation wurde gestartet.'
            Repair = 'Reparatur wurde gestartet.'
            Uninstall = 'Deinstallation wurde gestartet.'
        }
        Complete = @{
            Install = 'Installation wurde abgeschlossen.'
            Repair = 'Reparatur wurde abgeschlossen.'
            Uninstall = 'Deinstallation wurde abgeschlossen.'
        }
        RestartRequired = @{
            Install = 'Installation wurde abgeschlossen. Neustart erforderlich.'
            Repair = 'Reparatur wurde abgeschlossen. Neustart erforderlich.'
            Uninstall = 'Deinstallation wurde abgeschlossen. Neustart erforderlich.'
        }
        FastRetry = @{
            Install = 'Installation wurde nicht abgeschlossen.'
            Repair = 'Reparatur wurde nicht abgeschlossen.'
            Uninstall = 'Deinstallation wurde nicht abgeschlossen.'
        }
        Error = @{
            Install = 'Installation ist fehlgeschlagen.'
            Repair = 'Reparatur ist fehlgeschlagen.'
            Uninstall = 'Deinstallation ist fehlgeschlagen.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Installationsvorgang abgeschlossen werden kann.'
            Repair = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Reparaturvorgang abgeschlossen werden kann.'
            Uninstall = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Deinstallationsvorgang abgeschlossen werden kann.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation der Anwendung'
            Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
            Uninstall = '{Toolkit\CompanyName} - Neuinstallieren der Anwendung'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Der Speicherplatz reicht nicht aus, um die Installation abzuschließen:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie genügend Speicherplatz frei, um mit der Installation fortzufahren."
            Repair = "Der Speicherplatz reicht nicht aus, um die Reparatur von:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie ausreichend Speicherplatz frei, um mit der Reparatur fortzufahren."
            Uninstall = "Der Speicherplatz reicht nicht aus, um die Deinstallation von:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie ausreichend Speicherplatz frei, um mit der Deinstallation fortzufahren."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation der Anwendung'
            Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
            Uninstall = '{Toolkit\CompanyName} - Deinstallieren der Anwendung'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Installation wird ausgeführt. Bitte warten…'
            Repair = 'Reparatur wird ausgeführt. Bitte warten…'
            Uninstall = 'Deinstallation wird ausgeführt. Bitte warten…'
        }
        MessageDetail = @{
            Install = 'Dieses Fenster wird automatisch geschlossen, wenn die Installation abgeschlossen ist.'
            Repair = 'Dieses Fenster wird automatisch geschlossen, wenn die Reparatur abgeschlossen ist.'
            Uninstall = 'Dieses Fenster wird automatisch geschlossen, wenn die Deinstallation abgeschlossen ist.'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation der Anwendung'
            Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
            Uninstall = '{Toolkit\CompanyName} - Deinstallieren der Anwendung'
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
        CustomMessage = $null
        MessageRestart = 'Ihr Computer wird am Ende des Countdowns automatisch neu gestartet.'
        MessageTime = 'Bitte speichern Sie Ihre Arbeit und starten Sie innerhalb der vorgegebenen Zeit neu.'
        TimeRemaining = 'Restzeit:'
        Title = 'Neustart erforderlich'
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - Installation der Anwendung'
            Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
            Uninstall = '{Toolkit\CompanyName} - Deinstallieren der Anwendung'
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
                Install = "Die folgenden Anwendungen müssen geschlossen werden, bevor die Installation fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Anwendungen und fahren dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Anwendungen schließen`“ klicken."
                Repair = "Die folgenden Anwendungen müssen geschlossen werden, bevor die Reparatur fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Anwendungen und fahren dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Anwendungen schließen`“ klicken."
                Uninstall = "Die folgenden Anwendungen müssen geschlossen werden, bevor die Deinstallation fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Anwendungen und fahren dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Anwendungen schließen`“ klicken."
            }
            ExpiryMessage = @{
                Install = 'Die Installation kann bis zum Ablauf der Aufschiebefrist verschoben werden:'
                Repair = 'Die Reparatur kann bis zum Ablauf der Aufschiebefrist verschoben werden:'
                Uninstall = 'Die Deinstallation kann bis zum Ablauf der Aufschiebefrist verschoben werden:'
            }
            DeferralsRemaining = 'Verbleibende Aufschiebungen:'
            DeferralDeadline = 'Frist:'
            ExpiryWarning = 'Nach Ablauf der Aufschiebung haben Sie keine Möglichkeit mehr zu verschieben.'
            CountdownDefer = @{
                Install = 'Die Installation wird automatisch fortgesetzt in:'
                Repair = 'Die Reparatur wird automatisch fortgesetzt in:'
                Uninstall = 'Die Deinstallation wird automatisch fortgesetzt in:'
            }
            CountdownClose = @{
                Install = 'HINWEIS: Die Anwendungen werden automatisch geschlossen in:'
                Repair = 'HINWEIS: Die Anwendungen werden automatisch geschlossen in:'
                Uninstall = 'HINWEIS: Die Anwendungen werden automatisch geschlossen in:'
            }
            ButtonClose = 'Anwendungen &schließen'
            ButtonDefer = '&Verschieben'
            ButtonContinue = '&Weiter'
            ButtonContinueTooltip = 'Wählen Sie erst „Weiter“, nachdem Sie die oben aufgeführten Anwendung(en) geschlossen haben.'
        }
        Fluent = @{
            DialogMessage = @{
                Install = 'Bitte speichern Sie Ihre Arbeit, bevor Sie fortfahren, da die folgenden Anwendungen automatisch geschlossen werden.'
                Repair = 'Bitte speichern Sie Ihre Arbeit, bevor Sie fortfahren, da die folgenden Anwendungen automatisch geschlossen werden.'
                Uninstall = 'Bitte speichern Sie Ihre Arbeit, bevor Sie fortfahren, da die folgenden Anwendungen automatisch geschlossen werden.'
            }
            DialogMessageNoProcesses = @{
                Install = 'Wählen Sie Installieren aus, um mit der Installation fortzufahren.'
                Repair = 'Wählen Sie Reparieren aus, um mit der Reparatur fortzufahren.'
                Uninstall = 'Wählen Sie Deinstallieren aus, um mit der Deinstallation fortzufahren.'
            }
            AutomaticStartCountdown = 'Automatischer Start-Countdown'
            DeferralsRemaining = 'Verbleibende Aufschiebungen'
            DeferralDeadline = 'Aufschiebefrist'
            ButtonLeftText = @{
                Install = 'Schließen und Installieren'
                Repair = 'Schließen und Reparieren'
                Uninstall = 'Schließen und Deinstallieren'
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Installieren'
                Repair = 'Reparatur'
                Uninstall = 'Deinstallieren'
            }
            ButtonRightText = 'Verschieben'
            Subtitle = @{
                Install = '{Toolkit\CompanyName} - Installation der Anwendung'
                Repair = '{Toolkit\CompanyName} - Reparatur der Anwendung'
                Uninstall = '{Toolkit\CompanyName} - Deinstallieren der Anwendung'
            }
        }
        CustomMessage = $null
    }
}
