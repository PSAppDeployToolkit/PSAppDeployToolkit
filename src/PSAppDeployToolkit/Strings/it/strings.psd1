@{
    BalloonText = @{
        Complete = "Completata."
        Error = "Fallita."
        FastRetry = "Non completata."
        RestartRequired = "Completata. È necessario riavviare il computer."
        Start = "Iniziata."
    }
    BlockExecution = @{
        Message = "L'esecuzione di questa applicazione è stata temporaneamente bloccata in modo che l'operazione di installazione possa essere completata."
    }
    ClosePrompt = @{
        ButtonClose = "Chiudi Programmi"
        ButtonContinue = "Continua"
        ButtonContinueTooltip = "Seleziona `"Continua`" solo dopo la chiusura della(e) applicazione(i) elencate sopra."
        ButtonDefer = "Rimanda"
        CountdownMessage = "NOTA: il programma(s) sarà chiuso automaticamente in:"
        Message = "I seguenti programmi devono essere chiusi prima che l'installazione possa procedere.`n`nSalvare il lavoro , chiudere i programmi, e poi continuare. In alternativa, salvare il lavoro e fare clic su `"Chiudi Programmi`"."
    }
    DeferPrompt = @{
        Deadline = "Scadenza:"
        ExpiryMessage = "Si può decidere di posticipare l'installazione fino alla prossima richiesta automatica:"
        RemainingDeferrals = "Posticipi rimanenti:"
        WarningMessage = "Una volta che le richieste rimanenti saranno scadute, non sarà più possibile posticipare l'installazione."
        WelcomeMessage = "La seguente applicazione sta per essere installata:"
    }
    DeploymentType = @{
        Install = "Installazione"
        Repair = "Riparazione"
        Uninstall = "Disinstallazione"
    }
    DiskSpace = @{
        Message = "Non si dispone di spazio su disco sufficiente per completare l'installazione di:`n{0}`n`nSpazio necessario: {1}MB`nSpazio disponibile: {2}MB`n`nSi prega di spazio libero su disco sufficiente per procedere con l'installazione."
    }
    Progress = @{
        MessageInstall = "Installazione in corso. Attendere prego..."
        MessageInstallDetail = "Questa finestra si chiude automaticamente al termine dell'installazione."
        MessageRepair = "Riparazione in corso. Attendere prego..."
        MessageRepairDetail = "Questa finestra si chiuderà automaticamente al termine della riparazione."
        MessageUninstall = "Disinstallazione in corso. Attendere prego..."
        MessageUninstallDetail = "Questa finestra si chiuderà automaticamente al termine della disinstallazione."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimizzare"
        ButtonRestartNow = "Riavvia Ora"
        Message = "Per completare l'installazione, è necessario riavviare il computer."
        MessageRestart = "Il computer verrà riavviato automaticamente al termine del conto alla rovescia."
        MessageTime = "Salvare il lavoro e riavviare entro il tempo assegnato."
        TimeRemaining = "Tempo rimanente:"
        Title = "Riavvio Richiesto"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "Il {0} continuerà automaticamente in:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - App {0}'
            DialogMessage = 'Salvate il vostro lavoro prima di continuare, perché le applicazioni seguenti verranno chiuse automaticamente.'
            DialogMessageNoProcesses = "Selezionare Installa per continuare l'installazione. Se sono rimasti dei rinvii, si può anche scegliere di ritardare l'installazione."
            ButtonDeferRemaining = 'rimanere'
            ButtonLeftText = 'Rinviare'
            ButtonRightText = 'Chiudere le applicazioni e installare'
            ButtonRightTextNoProcesses = 'Installare'
        }
    }
}
