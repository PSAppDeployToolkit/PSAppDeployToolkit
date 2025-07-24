@{
    BalloonText = @{
        Complete = "ukončená."
        Error = "sa nepodarila."
        FastRetry = "nedokončená."
        RestartRequired = "ukončená. Je nutný reštart."
        Start = "spustená."
    }
    BlockExecution = @{
        Message = "Spustenie tejto aplikácie bolo dočasne zablokované, aby mohla byť inštalácia dokončená úspešne."
    }
    ClosePrompt = @{
        ButtonClose = "Ukončiť programy"
        ButtonContinue = "Pokračovať"
        ButtonContinueTooltip = "Kliknite na `"Pokračovať`", keď zavriete vyššie uvedené aplikácie."
        ButtonDefer = "Oddialiť"
        CountdownMessage = "Poznámka: Programy budú automaticky ukončené za:"
        Message = "Nasledujúce programy musia byť zatvorené, než bude inštalácia pokračovať.`n`nProsím, uložte svoju prácu, zatvorte dané programy a potom kliknite na pokračovať. Prípadne môžete uložiť svoju prácu a potom kliknite na tlačidlo `"Ukončiť programy`"."
    }
    DeferPrompt = @{
        Deadline = "Termín:"
        ExpiryMessage = "Inštaláciu môžete niekoľkokrát odložiť:"
        RemainingDeferrals = "Zostávajúce odklady:"
        WarningMessage = "Akonáhle odklady uplynú, už nebudete mať možnosť odložiť inštaláciu."
        WelcomeMessage = "Nasledujúca aplikácia bude nainštalovaná:"
    }
    DeploymentType = @{
        Install = "Inštalácia"
        Repair = "Oprava"
        Uninstall = "Odinštalácia"
    }
    DiskSpace = @{
        Message = "Nemáte dostatok voľného miesta na dokončenie inštalácie:`n{0}`n`nPotrebné miesto: {1}MB`nVoľné miesto: {2}MB`n`nProsím, uvoľnite dostatok miesta pre pokračovanie inštalácie."
    }
    Progress = @{
        MessageInstall = "Inštalácia sa vykonáva. Prosím čakajte..."
        MessageInstallDetail = "Toto okno sa po dokončení inštalácie automaticky zatvorí."
        MessageRepair = "Vykonáva sa oprava. Prosím čakajte..."
        MessageRepairDetail = "Toto okno sa po dokončení opravy automaticky zatvorí."
        MessageUninstall = "Prebieha odinštalácia. Prosím čakajte..."
        MessageUninstallDetail = "Toto okno sa po dokončení odinštalovania automaticky zatvorí."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimalizovať"
        ButtonRestartNow = "Reštartovať Teraz"
        Message = "Na dokončenie inštalácie musíte váš počítač reštartovať."
        MessageRestart = "Na konci odpočítavania, bude váš počítač automaticky reštartovaný."
        MessageTime = "Prosím, uložte si prácu a reštartujte počítač v stanovenej lehote."
        TimeRemaining = "Zostávajúci čas:"
        Title = "Je nutný reštart."
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} bude automaticky pokračovať za:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - Aplikácia {0}'
            DialogMessage = 'Pred pokračovaním uložte svoju prácu, pretože nasledujúce aplikácie sa automaticky zatvoria.'
            DialogMessageNoProcesses = 'Ak chcete pokračovať v inštalácii, vyberte možnosť Inštalovať. Ak máte ešte nejaké odklady, môžete tiež zvoliť odloženie inštalácie.'
            ButtonDeferRemaining = 'zostať'
            ButtonLeftText = 'Odloženie'
            ButtonRightText = 'Zatvoriť aplikácie a nainštalovať'
            ButtonRightTextNoProcesses = 'Inštalácia'
        }
    }
}
