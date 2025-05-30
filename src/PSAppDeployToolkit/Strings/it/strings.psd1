@{
    BalloonTip = @{
        Start = @{
            Install = 'Installazione avviata.'
            Repair = 'La riparazione è iniziata.'
            Uninstall = 'La disinstallazione è iniziata.'
        }
        Complete = @{
            Install = 'Installazione completata.'
            Repair = 'Riparazione completata.'
            Uninstall = 'Disinstallazione completata.'
        }
        RestartRequired = @{
            Install = 'Installazione completata. È necessario un riavvio.'
            Repair = 'Riparazione completata. È richiesto un riavvio.'
            Uninstall = 'Disinstallazione completata. È necessario un riavvio.'
        }
        FastRetry = @{
            Install = 'Installazione non completata.'
            Repair = 'Riparazione non completata.'
            Uninstall = 'Disinstallazione non completata.'
        }
        Error = @{
            Install = 'Installazione fallita.'
            Repair = 'Riparazione fallita.'
            Uninstall = 'Disinstallazione non riuscita.'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = "L'avvio di questa applicazione è stato temporaneamente bloccato per consentire il completamento di un'operazione di installazione."
            Repair = "L'avvio di questa applicazione è stato temporaneamente bloccato per consentire il completamento di un'operazione di riparazione."
            Uninstall = "L'avvio di questa applicazione è stato temporaneamente bloccato per consentire il completamento di un'operazione di disinstallazione."
        }
        Subtitle = @{
            Install = "{Toolkit\CompanyName} - Installazione dell'applicazione."
            Repair = "{Toolkit\CompanyName} - Riparazione dell'applicazione."
            Uninstall = "{Toolkit\CompanyName} - Disinstallazione App."
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "Non si dispone di spazio su disco sufficiente per completare l'installazione di:`n{0}`n`nSpazio richiesto: {1}MB`nSpazio disponibile: {2}MB`n`nPer favore, liberi spazio su disco sufficiente per procedere con l'installazione."
            Repair = "Non si dispone di spazio su disco sufficiente per completare la riparazione di:`n{0}`n`nSpazio richiesto: {1}MB`nSpazio disponibile: {2}MB`n`nPer favore, liberi spazio su disco sufficiente per procedere con la riparazione."
            Uninstall = "Non si dispone di spazio su disco sufficiente per completare la disinstallazione di:`n{0}`n`nSpazio richiesto: {1}MB`nSpazio disponibile: {2}MB`n`nPer favore, liberi spazio su disco sufficiente per procedere con la disinstallazione."
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = "{Toolkit\CompanyName} - Installazione dell'applicazione."
            Repair = "{Toolkit\CompanyName} - Riparazione app."
            Uninstall = "{Toolkit\CompanyName} - Disinstallazione app."
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'Installazione in corso. Attendere prego…'
            Repair = 'Riparazione in corso. Attendere…'
            Uninstall = 'Disinstallazione in corso. Attendere prego…'
        }
        MessageDetail = @{
            Install = "Questa finestra si chiuderà automaticamente al termine dell'installazione."
            Repair = "Questa finestra si chiuderà automaticamente al termine della riparazione."
            Uninstall = "Questa finestra si chiuderà automaticamente al termine della disinstallazione."
        }
        Subtitle = @{
            Install = "{Toolkit\CompanyName} - Installazione di applicazioni."
            Repair = "{Toolkit\CompanyName} - Riparazione dell'applicazione."
            Uninstall = "{Toolkit\CompanyName} - Disinstallazione dell'App."
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Riduci a icona.'
        ButtonRestartNow = 'Riavvia ora.'
        Message = @{
            Install = "Per completare l'installazione, deve riavviare il computer."
            Repair = "Affinché la riparazione sia completata, deve riavviare il computer."
            Uninstall = "Affinché la disinstallazione sia completata, deve riavviare il computer."
        }
        CustomMessage = ''
        MessageRestart = 'Il computer verrà automaticamente riavviato al termine del conto alla rovescia.'
        MessageTime = 'Salvi il suo lavoro e riavvii entro il tempo stabilito.'
        TimeRemaining = 'Tempo rimanente:'
        Title = 'Riavvio richiesto'
        Subtitle = @{
            Install = "{Toolkit\CompanyName} - Installazione di un'applicazione."
            Repair = "{Toolkit\CompanyName} - Riparazione dell'applicazione."
            Uninstall = "{Toolkit\CompanyName} - Disinstallazione App."
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = 'La seguente applicazione sta per essere installata:'
                Repair = 'La seguente applicazione sta per essere riparata:'
                Uninstall = 'La seguente applicazione sta per essere disinstallata:'
            }
            CloseAppsMessage = @{
                Install = "I seguenti programmi devono essere chiusi prima che l'installazione possa procedere.`n'nSalvi il suo lavoro, chiuda i programmi e poi continui. In alternativa, salvi il suo lavoro e clicchi su `“Chiudi programmi`”."
                Repair = "I seguenti programmi devono essere chiusi prima che la riparazione possa procedere.`n'nSalvi il suo lavoro, chiuda i programmi e poi continui. In alternativa, salvi il suo lavoro e clicchi su `“Chiudi programmi`”."
                Uninstall = "I seguenti programmi devono essere chiusi prima di procedere alla disinstallazione.`n'nSalvi il suo lavoro, chiuda i programmi e poi continui. In alternativa, salvi il suo lavoro e clicchi su `“Chiudi programmi`”."
            }
            ExpiryMessage = @{
                Install = "Può scegliere di rinviare l'installazione fino alla scadenza del rinvio:"
                Repair = "Può scegliere di rinviare la riparazione fino alla scadenza del rinvio:"
                Uninstall = "Può scegliere di rinviare la disinstallazione fino alla scadenza del rinvio:"
            }
            DeferralsRemaining = 'Rinvii rimanenti:'
            DeferralDeadline = 'Scadenza:'
            ExpiryWarning = 'Una volta scaduto il rinvio, non avrà più la possibilità di rinviare.'
            CountdownDefer = @{
                Install = "L'installazione continuerà automaticamente tra:"
                Repair = "La riparazione continuerà automaticamente in:"
                Uninstall = "La disinstallazione continuerà automaticamente tra:"
            }
            CountdownClose = 'NOTA: I programmi verranno chiusi automaticamente in:'
            ButtonClose = 'Chiudere &Programmi'
            ButtonDefer = '&Rinviare'
            ButtonContinue = '&Continuare'
            ButtonContinueTooltip = 'Selezioni “Continua” solo dopo aver chiuso le applicazioni sopra elencate.'
        }
        Fluent = @{
            DialogMessage = 'Salvi il suo lavoro prima di continuare, perché le applicazioni seguenti verranno chiuse automaticamente.'
            DialogMessageNoProcesses = @{
                Install = "Selezioni Install per continuare l'installazione. Se sono rimasti dei rinvii, può anche scegliere di ritardare l'installazione."
                Repair = "Selezioni Repair per continuare con la riparazione. Se sono rimasti dei rinvii, può anche scegliere di ritardare la riparazione."
                Uninstall = "Selezioni Disinstallazione per proseguire con la disinstallazione. Se sono rimasti dei rinvii, può anche scegliere di ritardare la disinstallazione."
            }
            AutomaticStartCountdown = "Conto alla rovescia per l'avvio automatico"
            DeferralsRemaining = 'Rimanenti differimenti'
            DeferralDeadline = 'Scadenza di rinvio'
            ButtonLeftText = @{
                Install = "Chiudi le applicazioni e installa."
                Repair = "Chiudi applicazioni e ripara."
                Uninstall = "Chiudere le applicazioni e disinstallare."
            }
            ButtonLeftNoProcessesText = @{
                Install = 'Installa'
                Repair = 'Riparare'
                Uninstall = 'Disinstalla'
            }
            ButtonRightText = 'Rimandare'
            Subtitle = @{
                Install = "{Toolkit\CompanyName} - Installazione di un'applicazione."
                Repair = "{Toolkit\CompanyName} - Riparazione dell'applicazione."
                Uninstall = "{Toolkit\CompanyName} - Disinstallazione App."
            }
        }
        CustomMessage = ''
    }
}
