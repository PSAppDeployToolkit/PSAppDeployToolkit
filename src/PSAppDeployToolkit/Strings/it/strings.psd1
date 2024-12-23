@{
    BalloonText = @{
        Complete = @{
            Install = 'Installazione completata'.
            Repair = 'Riparazione completata'.
            Uninstall = 'Disinstallazione completata'.
        }
        Error = @{
            Install = 'Installazione fallita'.
            Repair = 'Riparazione fallita'.
            Uninstall = 'Disinstallazione non riuscita'.
        }
        FastRetry = @{
            Install = 'Installazione non completata'.
            Repair = 'Riparazione non completata'.
            Uninstall = 'Disinstallazione non completata'.
        }
        RestartRequired = @{
            Install = 'Installazione completata. È necessario un riavvio.'
            Repair = 'Riparazione completata. È richiesto un riavvio.'
            Uninstall = 'Disinstallazione completata. È necessario un riavvio.'
        }
        Start = @{
            Install = 'Installazione avviata.'
            Repair = 'La riparazione è iniziata.'
            Uninstall = 'La disinstallazione è iniziata.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = "L'avvio di questa applicazione è stato temporaneamente bloccato per consentire il completamento di un'operazione di installazione."
            Repair = "L'avvio di questa applicazione è stato temporaneamente bloccato per consentire il completamento di un'operazione di riparazione."
            Uninstall = "L'avvio di questa applicazione è stato temporaneamente bloccato per consentire il completamento di un'operazione di disinstallazione."
        }
        Subtitle = @{
            Install = "PSAppDeployToolkit - Installazione dell'applicazione."
            Repair = "PSAppDeployToolkit - Riparazione dell'applicazione."
            Uninstall = "PSAppDeployToolkit - Disinstallazione App."
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "Non si dispone di spazio su disco sufficiente per completare l'installazione di:`n{0}`n`nSpazio richiesto: {1}MB`nSpazio disponibile: {2}MB`n`nPer favore, liberi spazio su disco sufficiente per procedere con l'installazione."
            Repair = "Non si dispone di spazio su disco sufficiente per completare la riparazione di:`n{0}`n`nSpazio richiesto: {1}MB`nSpazio disponibile: {2}MB`n`nPer favore, liberi spazio su disco sufficiente per procedere con la riparazione."
            Uninstall = "Non si dispone di spazio su disco sufficiente per completare la disinstallazione di:`n{0}`n`nSpazio richiesto: {1}MB`nSpazio disponibile: {2}MB`n`nPer favore, liberi spazio su disco sufficiente per procedere con la disinstallazione."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Installazione in corso. Attendere prego...'
            Repair = 'Riparazione in corso. Attendere...'
            Uninstall = 'Disinstallazione in corso. Attendere prego...'
        }
        MessageDetail = @{
            Install = "Questa finestra si chiuderà automaticamente al termine dell'installazione."
            Repair = "Questa finestra si chiuderà automaticamente al termine della riparazione."
            Uninstall = "Questa finestra si chiuderà automaticamente al termine della disinstallazione."
        }
        Subtitle = @{
            Install = "PSAppDeployToolkit - Installazione di applicazioni."
            Repair = "PSAppDeployToolkit - Riparazione dell'applicazione."
            Uninstall = "PSAppDeployToolkit - Disinstallazione dell'App."
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = "PSAppDeployToolkit - Installazione dell'applicazione."
            Repair = "PSAppDeployToolkit - Riparazione app."
            Uninstall = "PSAppDeployToolkit - Disinstallazione app."
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Riduci a icona'.
        ButtonRestartNow = 'Riavvia ora'.
        Message = @{
            Install = "Per completare l'installazione, deve riavviare il computer."
            Repair = "Affinché la riparazione sia completata, deve riavviare il computer."
            Uninstall = "Affinché la disinstallazione sia completata, deve riavviare il computer."
        }
        MessageRestart = 'Il computer verrà automaticamente riavviato al termine del conto alla rovescia.'
        MessageTime = 'Salvi il suo lavoro e riavvii entro il tempo stabilito.'
        TimeRemaining = 'Tempo rimanente:'
        Title = 'Riavvio richiesto'
        Subtitle = @{
            Install = "PSAppDeployToolkit - Installazione di un'applicazione."
            Repair = "PSAppDeployToolkit - Riparazione dell'applicazione."
            Uninstall = "PSAppDeployToolkit - Disinstallazione App."
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Chiudere &Programmi'
                ButtonContinue = '&Continuare'
                ButtonContinueTooltip = 'Selezioni “Continua” solo dopo aver chiuso le applicazioni sopra elencate'.
                ButtonDefer = '&Rinviare'
                CountdownMessage = 'NOTA: I programmi verranno chiusi automaticamente in:'
                Message = @{
                    Install = "I seguenti programmi devono essere chiusi prima che l'installazione possa procedere.`n'nSalvi il suo lavoro, chiuda i programmi e poi continui. In alternativa, salvi il suo lavoro e clicchi su “Chiudi programmi”."
                    Repair = "I seguenti programmi devono essere chiusi prima che la riparazione possa procedere.`n'nSalvi il suo lavoro, chiuda i programmi e poi continui. In alternativa, salvi il suo lavoro e clicchi su “Chiudi programmi”."
                    Uninstall = "I seguenti programmi devono essere chiusi prima di procedere alla disinstallazione.`n'nSalvi il suo lavoro, chiuda i programmi e poi continui. In alternativa, salvi il suo lavoro e clicchi su “Chiudi programmi”."
                }
            }
            Defer = @{
                Deadline = 'Scadenza:'
                ExpiryMessage = @{
                    Install = "Può scegliere di rinviare l'installazione fino alla scadenza del rinvio:"
                    Repair = "Può scegliere di rinviare la riparazione fino alla scadenza del rinvio:"
                    Uninstall = "Può scegliere di rinviare la disinstallazione fino alla scadenza del rinvio:"
                }
                RemainingDeferrals = 'Rinvii rimanenti:'
                WarningMessage = 'Una volta scaduto il rinvio, non avrà più la possibilità di rinviare.'
                WelcomeMessage = @{
                    Install = 'La seguente applicazione sta per essere installata:'
                    Repair = 'La seguente applicazione sta per essere riparata:'
                    Uninstall = 'La seguente applicazione sta per essere disinstallata:'
                }
            }
            CountdownMessage = @{
                Install = "L'installazione continuerà automaticamente tra:"
                Repair = "La riparazione continuerà automaticamente in:"
                Uninstall = "La disinstallazione continuerà automaticamente tra:"
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = "PSAppDeployToolkit - Installazione di un'applicazione."
                Repair = "PSAppDeployToolkit - Riparazione dell'applicazione."
                Uninstall = "PSAppDeployToolkit - Disinstallazione App."
            }
            DialogMessage = 'Salvi il suo lavoro prima di continuare, perché le applicazioni seguenti verranno chiuse automaticamente'.
            DialogMessageNoProcesses = @{
                Install = "Selezioni Install per continuare l'installazione. Se sono rimasti dei rinvii, può anche scegliere di ritardare l'installazione."
                Repair = "Selezioni Repair per continuare con la riparazione. Se sono rimasti dei rinvii, può anche scegliere di ritardare la riparazione."
                Uninstall = "Selezioni Disinstallazione per proseguire con la disinstallazione. Se sono rimasti dei rinvii, può anche scegliere di ritardare la disinstallazione."
            }
            ButtonDeferRemaining = 'rimanere'
            ButtonLeftText = 'Rimandare'
            ButtonRightText = @{
                Install = "Chiudi le applicazioni e installa."
                Repair = "Chiudi applicazioni e ripara."
                Uninstall = "Chiudere le applicazioni e disinstallare."
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Installa'
                Repair = 'Riparare'
                Uninstall = 'Disinstalla'
            }
        }
    }
}
