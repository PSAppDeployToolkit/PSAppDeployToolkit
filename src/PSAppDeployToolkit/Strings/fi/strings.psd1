@{
    BalloonText = @{
        Complete = @{
            Install = 'Asennus valmis.'
            Repair = 'Korjaus valmis.'
            Uninstall = 'Asennuksen poisto valmis.'
        }
        Error = @{
            Install = 'Asennus epäonnistui.'
            Repair = 'Korjaus epäonnistui.'
            Uninstall = 'Asennuksen poisto epäonnistui.'
        }
        FastRetry = @{
            Install = 'Asennus ei ole valmis.'
            Repair = 'Korjaus ei ole valmis.'
            Uninstall = 'Asennuksen poisto ei ole valmis.'
        }
        RestartRequired = @{
            Install = 'Asennus suoritettu. Uudelleenkäynnistys vaaditaan.'
            Repair = 'Korjaus suoritettu. Uudelleenkäynnistys vaaditaan.'
            Uninstall = 'Asennuksen poisto valmis. Uudelleenkäynnistys vaaditaan.'
        }
        Start = @{
            Install = 'Asennus aloitettu.'
            Repair = 'Korjaus aloitettu.'
            Uninstall = 'Asennuksen poisto aloitettu.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = 'Tämän sovelluksen käynnistäminen on tilapäisesti estetty, jotta asennustoiminto voidaan suorittaa loppuun.'
            Repair = 'Sovelluksen käynnistäminen on tilapäisesti estetty, jotta korjaustoiminto voidaan suorittaa loppuun.'
            Uninstall = 'Sovelluksen käynnistäminen on tilapäisesti estetty, jotta asennuksen poisto voidaan suorittaa loppuun.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Sovelluksen Asennus'
            Repair = 'PSAppDeployToolkit - Sovelluksen Korjaus'
            Uninstall = 'PSAppDeployToolkit - Sovelluksen Poisto'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "Sinulla ei ole tarpeeksi levytilaa asennuksen loppuunsaattamiseen:`n{0}`n`nTilaa tarvitaan: {1}MB`nTilaa käytettävissä: {2}MB`n`nVapauta riittävästi levytilaa, jotta voit jatkaa asennusta."
            Korjaus = "Sinulla ei ole tarpeeksi levytilaa korjauksen suorittamiseen:`n{0}`n`nTilaa tarvitaan: {1}MB`nTilaa käytettävissä: {2}MB`n`nVapauta riittävästi levytilaa, jotta voit jatkaa korjausta."
            Uninstall = "Sinulla ei ole tarpeeksi levytilaa, jotta voit suorittaa asennuksen poistamisen loppuun:`n{0}`n`nTilaa tarvitaan: {1}MB`nTilaa käytettävissä: {2}MB`n`nVapauta riittävästi levytilaa, jotta voit jatkaa asennuksen poistamista."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Asennus käynnissä. Odota...'.
            Repair = 'Korjaus käynnissä. Odota...'
            Uninstall = 'Asennuksen poisto käynnissä. Odota...'
        }
        MessageDetail = @{
            Install = 'Tämä ikkuna sulkeutuu automaattisesti, kun asennus on valmis.'
            Repair = 'Tämä ikkuna sulkeutuu automaattisesti, kun korjaus on valmis.'
            Uninstall = 'Tämä ikkuna sulkeutuu automaattisesti, kun asennuksen poisto on valmis.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Sovelluksen Asennus'
            Repair = 'PSAppDeployToolkit - Sovelluksen Korjaus'
            Uninstall = 'PSAppDeployToolkit - Sovelluksen Poisto'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Sovelluksen Asennus'
            Repair = 'PSAppDeployToolkit - Sovelluksen Korjaus'
            Uninstall = 'PSAppDeployToolkit - Sovelluksen Poisto'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimoi'
        ButtonRestartNow = 'Käynnistä uudelleen nyt'
        Message = @{
            Install = 'Jotta asennus voidaan suorittaa loppuun, sinun on käynnistettävä tietokoneesi uudelleen.'
            Repair = 'Jotta korjaus saataisiin päätökseen, sinun on käynnistettävä tietokone uudelleen.'
            Uninstall = 'Jotta asennuksen poisto saataisiin päätökseen, sinun on käynnistettävä tietokone uudelleen.'
        }
        MessageRestart = 'Tietokone käynnistyy automaattisesti uudelleen lähtölaskennan päätyttyä.'
        MessageTime = 'Tallenna työsi ja käynnistä tietokone uudelleen annetussa ajassa.'
        TimeRemaining = 'Jäljellä oleva aika:'
        Title = 'Uudelleenkäynnistys Vaaditaan'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Sovelluksen Asennus'
            Repair = 'PSAppDeployToolkit - Sovelluksen Korjaus'
            Uninstall = 'PSAppDeployToolkit - Sovelluksen Poisto'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Sulje &ohjelmat'
                ButtonContinue = '&Jatka'
                ButtonContinueTooltip = 'Valitse ”Jatka” vasta, kun olet sulkenut edellä luetellut sovellukset.'
                ButtonDefer = '&Siirrä'
                CountdownMessage = 'HUOMAUTUS: Ohjelma(t) suljetaan automaattisesti:'
                Message = @{
                    Install = "Seuraavat ohjelmat on suljettava, ennen kuin asennus voi jatkua.`n`nTallenna työsi, sulje ohjelmat ja jatka sitten. Vaihtoehtoisesti tallenna työsi ja napsauta ”Sulje ohjelmat”."
                    Korjaus = "Seuraavat ohjelmat on suljettava, ennen kuin korjaus voi jatkua.`n`nTallenna työsi, sulje ohjelmat ja jatka sitten. Vaihtoehtoisesti voit tallentaa työsi ja napsauttaa ”Sulje ohjelmat”."
                    Poista asennus = "Seuraavat ohjelmat on suljettava, ennen kuin asennuksen poisto voi jatkua.`n`nTallenna työsi, sulje ohjelmat ja jatka sitten. Vaihtoehtoisesti voit tallentaa työsi ja napsauttaa ”Sulje ohjelmat”."
                }
            }
            Defer = @{
                Deadline = 'Määräaika:'
                ExpiryMessage = @{
                    Install = 'Voit halutessasi lykätä asennusta, kunnes lykkäys päättyy:'
                    Repair = 'Voit lykätä korjausta, kunnes lykkäys päättyy:'
                    Uninstall = 'Voit lykätä asennuksen poistamista, kunnes lykkäys päättyy:'
                }
                RemainingDeferrals = 'Jäljellä olevat lykkäykset:'
                WarningMessage = 'Kun lykkäys on päättynyt, et voi enää lykätä.'
                WelcomeMessage = @{
                    Install = 'Seuraava sovellus asennetaan pian:'
                    Repair = 'Seuraava sovellus korjataan:'
                    Uninstall = 'Seuraavan sovelluksen poisto on alkamassa:'
                }
            }
            CountdownMessage = @{
                Install = 'Asennus jatkuu automaattisesti:'
                Repair = 'Korjaus jatkuu automaattisesti:'
                Uninstall = 'Asennuksen poisto jatkuu automaattisesti:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Sovelluksen Asennus'
                Repair = 'PSAppDeployToolkit - Sovelluksen Korjaus'
                Uninstall = 'PSAppDeployToolkit - Sovelluksen Poisto'
            }
            DialogMessage = 'Tallenna työsi ennen kuin jatkat, sillä seuraavat sovellukset suljetaan automaattisesti.'
            DialogMessageNoProcesses = @{
                Install = 'Jatka asennusta valitsemalla Install. Jos sinulla on lykkäyksiä jäljellä, voit myös lykätä asennusta.'
                Repair = 'Jatka korjausta valitsemalla Repair. Jos sinulla on vielä lykkäyksiä jäljellä, voit myös lykätä korjausta.'
                Poista asennus = 'Jatka asennuksen poistamista valitsemalla Poista. Jos sinulla on vielä lykkäyksiä jäljellä, voit myös lykätä asennuksen poistamista.'
            }
            ButtonDeferRemaining = 'jäljellä'
            ButtonLeftText = 'Lykkää'
            ButtonRightText = @{
                Install = 'Sulje Sovellukset ja Asenna'
                Repair = 'Sulje Sovellukset ja Korjaa'
                Uninstall = 'Sulje Sovellukset & Poista Asennus'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Asenna'
                Repair = 'Korjaa'
                Uninstall = 'Poista Asennus'
            }
        }
    }
}
