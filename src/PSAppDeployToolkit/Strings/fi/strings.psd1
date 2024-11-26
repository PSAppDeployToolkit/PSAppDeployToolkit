@{
    BalloonText = @{
        Complete = "valmis."
        Error = "epäonnistui."
        FastRetry = "ei ole valmis."
        RestartRequired = "valmis. Tietokone on käynnistettävä uudelleen."
        Start = "alkoi."
    }
    BlockExecution = @{
        Message = "Ohjelmiston käynnistäminen on tilapäisesti estetty, jotta ohjelmisto voi onnistuneesti asentua."
    }
    ClosePrompt = @{
        ButtonClose = "Sulje ohjelmat"
        ButtonContinue = "Jatka"
        ButtonContinueTooltip = "Valitse jatka, kun olet sulkenut ohjelmat."
        ButtonDefer = "Myöhemmin"
        CountdownMessage = "HUOMIO: Ohjelma(t) suljetaan automaattisesti:"
        Message = "Seuraavat ohjelmat on suljettava ennen asennusta`n`nTallenna työsi ja jatka. Vaihtoehtoisesti voit tallentaa työsi ja valita `"Sulje ohjelmat`"."
    }
    DeferPrompt = @{
        Deadline = "Määräaika:"
        ExpiryMessage = "Voit siirtää asennusta myöhemmäksi:"
        RemainingDeferrals = "Jäljellä olevia siirtoja myöhempään ajankohtaan:"
        WarningMessage = "Tietyn ajan kuluessa et voi enää siirtää asennusta myöhemmäksi."
        WelcomeMessage = "Ohjelma joka asennetaan seuraavaksi:"
    }
    DeploymentType = @{
        Install = "Asennus"
        Repair = "Korjaus"
        Uninstall = "Ohjelmiston poisto"
    }
    DiskSpace = @{
        Message = "Kiintolevyllä ei ole riittävästi tilaa asennusta varten:`n{0}`n`nVaadittu levytila: {1}MB`nLevytilaa käytettävissä: {2}MB`n`nVapauta levytilaa, jotta asennus voi jatkua."
    }
    Progress = @{
        MessageInstall = "Asentaa. Odota..."
        MessageInstallDetail = "Tämä ikkuna sulkeutuu automaattisesti, kun asennus on valmis."
        MessageRepair = "Korjaus käynnissä. Odota..."
        MessageRepairDetail = "Tämä ikkuna sulkeutuu automaattisesti, kun korjaus on valmis."
        MessageUninstall = "Ohjelmistoa poistetaan. Odota..."
        MessageUninstallDetail = "Tämä ikkuna sulkeutuu automaattisesti, kun asennuksen poisto on valmis."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Käynnistä uudelleen myöhemmin"
        ButtonRestartNow = "Käynnistä uudelleen heti"
        Message = "Tietokone on käynnistettävä uudelleen, ennen kuin ohjelmiston asennus on valmis."
        MessageRestart = "Tietokone käynnistyy uudelleen, kun laskuri on saavuttanut nollan."
        MessageTime = "Tallenna työsi ja käynnistä tietokone uudelleen aikarajan sisällä."
        TimeRemaining = "Aikaa jäljellä:"
        Title = "Tietokone on käynnistettävä uudelleen"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} jatkaa automaattisesti:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - Sovellus {0}'
            DialogMessage = 'Tallenna työsi ennen kuin jatkat, sillä seuraavat sovellukset suljetaan automaattisesti.'
            DialogMessageNoProcesses = 'Jatka asennusta valitsemalla Asenna. Jos sinulla on vielä lykkäyksiä jäljellä, voit myös lykätä asennusta.'
            ButtonDeferRemaining = 'pysyä'
            ButtonLeftText = 'Siirrä'
            ButtonRightText = 'Sulje sovellukset & asenna'
            ButtonRightTextNoProcesses = 'Asenna'
        }
    }
}
