@{
    BalloonText = @{
        Complete = @{
            Install = 'Installation wurde abgeschlossen.'
            Repair = 'Reparatur wurde abgeschlossen.'
            Uninstall = 'Deinstallation wurde abgeschlossen.'
        }
        Error = @{
            Install = 'Installation ist fehlgeschlagen.'
            Repair = 'Reparatur ist fehlgeschlagen.'
            Uninstall = 'Deinstallation ist fehlgeschlagen.'
        }
        FastRetry = @{
            Install = 'Installation wurde nicht abgeschlossen.'
            Repair = 'Reparatur wurde nicht abgeschlossen.'
            Uninstall = 'Deinstallation wurde nicht abgeschlossen.'
        }
        RestartRequired = @{
            Install = 'Installation wurde abgeschlossen. Neustart erforderlich.'
            Repair = 'Reparatur wurde abgeschlossen. Neustart erforderlich.'
            Uninstall = 'Deinstallation wurde abgeschlossen. Neustart erforderlich.'
        }
        Start = @{
            Install = 'Installation wurde gestartet.'
            Repair = 'Reparatur wurde gestartet.'
            Uninstall = 'Deinstallation wurde gestartet.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Installationsvorgang abgeschlossen werden kann.'
            Repair = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Reparaturvorgang abgeschlossen werden kann.'
            Uninstall = 'Das Starten dieser Anwendung wurde vorübergehend blockiert, damit ein Deinstallationsvorgang abgeschlossen werden kann.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Installation der Anwendung'
            Repair = 'PSAppDeployToolkit - Reparatur der Anwendung'
            Uninstall = 'PSAppDeployToolkit - Neuinstallieren der Anwendung'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "Der Speicherplatz reicht nicht aus, um die Installation abzuschließen:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie genügend Speicherplatz frei, um mit der Installation fortzufahren."
            Repair = "Der Speicherplatz reicht nicht aus, um die Reparatur von:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie ausreichend Speicherplatz frei, um mit der Reparatur fortzufahren."
            Uninstall = "Der Speicherplatz reicht nicht aus, um die Deinstallation von:`n{0}`n`nErforderlicher Speicherplatz: {1} MB`nVerfügbarer Speicherplatz: {2} MB`n`nBitte geben Sie ausreichend Speicherplatz frei, um mit der Deinstallation fortzufahren."
        }
    }
    Progress = @{
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
            Install = 'PSAppDeployToolkit - Installation der Anwendung'
            Repair = 'PSAppDeployToolkit - Reparatur der Anwendung'
            Uninstall = 'PSAppDeployToolkit - Deinstallieren der Anwendung'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Installation der Anwendung'
            Repair = 'PSAppDeployToolkit - Reparatur der Anwendung'
            Uninstall = 'PSAppDeployToolkit - Deinstallieren der Anwendung'
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
        MessageRestart = 'Ihr Computer wird am Ende des Countdowns automatisch neu gestartet.'
        MessageTime = 'Bitte speichern Sie Ihre Arbeit und starten Sie innerhalb der vorgegebenen Zeit neu.'
        TimeRemaining = 'Restzeit:'
        Title = 'Neustart erforderlich'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Installation der Anwendung'
            Repair = 'PSAppDeployToolkit - Reparatur der Anwendung'
            Uninstall = 'PSAppDeployToolkit - Deinstallieren der Anwendung'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Programme &schließen'
                ButtonContinue = '&Weiter'
                ButtonContinueTooltip = 'Wählen Sie erst „Weiter“, nachdem Sie die oben aufgeführten Anwendung(en) geschlossen haben.'
                ButtonDefer = '&Aufschieben'
                CountdownMessage = 'HINWEIS: Die Programme werden automatisch geschlossen in:'
                Message = @{
                    Install = "Die folgenden Programme müssen geschlossen werden, bevor die Installation fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Programme und fahren Sie dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Programme schließen`“ klicken."
                    Repair = "Die folgenden Programme müssen geschlossen werden, bevor die Reparatur fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Programme und fahren Sie dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Programme schließen`“ klicken."
                    Uninstall = "Die folgenden Programme müssen geschlossen werden, bevor die Deinstallation fortgesetzt werden kann.`n`nBitte speichern Sie Ihre Arbeit, schließen Sie die Programme und fahren Sie dann fort. Alternativ können Sie Ihre Arbeit speichern und auf `„Programme schließen`“ klicken."
                }
            }
            Defer = @{
                Deadline = 'Frist:'
                ExpiryMessage = @{
                    Install = 'Die Installation kann bis zum Ablauf der Aufschiebungsfrist aufschoben werden:'
                    Repair = 'Die Reparatur kann bis zum Ablauf der Aufschiebungsfrist aufschoben werden:'
                    Uninstall = 'Die Deinstallation kann bis zum Ablauf der Aufschiebungsfrist aufschoben werden:'
                }
                RemainingDeferrals = 'Verbleibende Aufschiebungen:'
                WarningMessage = 'Nach Ablauf der Aufschiebung haben Sie keine Möglichkeit mehr, die Aufschiebung zu nutzen.'
                WelcomeMessage = @{
                    Install = 'Die folgende Anwendung wird installiert:'
                    Repair = 'Die folgende Anwendung wird repariert:'
                    Uninstall = 'Die folgende Anwendung wird deinstalliert:'
                }
            }
            CountdownMessage = @{
                Install = 'Die Installation wird automatisch fortgesetzt in:'
                Repair = 'Die Reparatur wird automatisch fortgesetzt in:'
                Uninstall = 'Die Deinstallation wird automatisch fortgesetzt in:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Installation der Anwendung'
                Repair = 'PSAppDeployToolkit - Reparatur der Anwendung'
                Uninstall = 'PSAppDeployToolkit - Deinstallieren der Anwendung'
            }
            DialogMessage = 'Bitte speichern Sie Ihre Arbeit, bevor Sie fortfahren, da die folgenden Anwendungen automatisch geschlossen werden.'
            DialogMessageNoProcesses = @{
                Install = 'Wählen Sie Installieren aus, um mit der Installation fortzufahren. Wenn Sie noch Aufschübe haben, können Sie die Installation auch verschieben.'
                Repair = 'Wählen Sie Reparieren aus, um mit der Reparatur fortzufahren. Wenn Sie noch Aufschübe haben, können Sie auch die Reparatur aufschieben.'
                Uninstall = 'Wählen Sie Deinstallieren aus, um mit der Deinstallation fortzufahren. Wenn Sie noch Aufschübe haben, können Sie auch die Deinstallation aufschieben.'
            }
            ButtonDeferRemaining = 'Beibehalten'
            ButtonLeftText = 'Aufschieben'
            ButtonRightText = @{
                Install = 'Anwendungen schließen und Installieren'
                Repair = 'Anwendungen schließen und Reparieren'
                Uninstall = 'Anwendungen schließen und Deinstallieren'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Installieren'
                Repair = 'Reparatur'
                Uninstall = 'Deinstallieren'
            }
        }
    }
}
