@{
    BalloonText = @{
        Complete = "slutförd."
        Error = "misslyckades."
        FastRetry = "ej slutförd."
        RestartRequired = "slutförd. En omstart av datorn är nödvändig."
        Start = "startad."
    }
    BlockExecution = @{
        Message = "Den här applikationen har temporärt blockerats så att installationen kan slutföras."
    }
    ClosePrompt = @{
        ButtonClose = "Stäng Program"
        ButtonContinue = "Fortsätt"
        ButtonContinueTooltip = "Välj `"Fortsätt`" endast efter att ha stängt applikation(er) i ovanstående lista."
        ButtonDefer = "Skjut upp"
        CountdownMessage = "OBS: Programmen kommer automatiskt att avslutas om:"
        Message = "Följande program måste stängas innan installationen kan fortsätta.`n`nSe till att spara ditt arbete, stäng de öppna programmen och klicka sen på `"Fortsätt`".`nAlternativt, spara ditt arbete och klicka på `"Stäng Program`"."
    }
    DeferPrompt = @{
        Deadline = "Deadline:"
        ExpiryMessage = "Du kan välja att fördröja installationen ett begränsat antal gånger under en begränsad tid:"
        RemainingDeferrals = "Antal återstående fördröjningar:"
        WarningMessage = "När antalet fördröjningar är slut eller deadlinen inträffar är detta alternativ inte längre tillgängligt."
        WelcomeMessage = "Följande applikation kommer att installeras:"
    }
    DeploymentType = @{
        Install = "Installation"
        Repair = "Reparation"
        Uninstall = "Avinstallation"
    }
    DiskSpace = @{
        Message = "Du har inte tillräckligt med ledigt diskutrymme för att kunna installera:`n{0}`n`nDiskutrymme som krävs: {1}MB`nLedigt diskutrymme: {2}MB`n`nFrigör utrymme på hårddisken och försök igen."
    }
    Progress = @{
        MessageInstall = "Installation pågår. Var god vänta..."
        MessageInstallDetail = "Detta fönster stängs automatiskt när installationen är klar."
        MessageRepair = "Reparation pågår. Var god vänta..."
        MessageRepairDetail = "Detta fönster stängs automatiskt när reparationen är klar."
        MessageUninstall = "Avinstallation pågår. Var god vänta..."
        MessageUninstallDetail = "Detta fönster stängs automatiskt när avinstallationen är klar."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimera"
        ButtonRestartNow = "Starta Om Nu"
        Message = "För att installationen ska kunna slutföras måste din dator startas om."
        MessageRestart = "Din dator kommer automatiskt att starta om när nedräkningen är slut."
        MessageTime = "Se till att spara ditt arbete innan tiden går ut och en automatisk omstart sker."
        TimeRemaining = "Återstående tid:"
        Title = "Omstart Krävs"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} kommer att fortsätta automatiskt i:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - App {0}'
            DialogMessage = 'Spara ditt arbete innan du fortsätter eftersom följande applikationer kommer att stängas automatiskt.'
            DialogMessageNoProcesses = 'Välj Install för att fortsätta med installationen. Om du har några uppskjutna betalningar kvar kan du också välja att skjuta upp installationen.'
            ButtonDeferRemaining = 'kvarstå'
            ButtonLeftText = 'Skjut upp'
            ButtonRightText = 'Stäng appar och installera'
            ButtonRightTextNoProcesses = 'Installera'
        }
    }
}
