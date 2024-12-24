﻿@{
    BalloonText = @{
        Complete = @{
            Install = 'Inštalácia dokončená.'
            Repair = 'Oprava dokončená.'
            Uninstall = 'Odinštalovanie dokončené.'
        }
        Error = @{
            Install = 'Inštalácia zlyhala.'
            Repair = 'Oprava zlyhala.'
            Uninstall = 'Odinštalovanie zlyhalo.'
        }
        FastRetry = @{
            Install = 'Inštalácia nebola dokončená.'
            Repair = 'Oprava nebola dokončená.'
            Uninstall = 'Odinštalovanie nebolo dokončené.'
        }
        RestartRequired = @{
            Install = 'Inštalácia dokončená. Vyžaduje sa reštart.'
            Repair = 'Oprava dokončená. Vyžaduje sa reštart.'
            Uninstall = 'Odinštalovanie dokončené. Vyžaduje sa reštart.'
        }
        Start = @{
            Install = 'Inštalácia sa začala.'
            Repair = 'Začala sa oprava.'
            Uninstall = 'Začala sa odinštalácia.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = 'Spustenie tejto aplikácie bolo dočasne zablokované, aby sa mohla dokončiť inštalačná operácia.'
            Repair = 'Spustenie tejto aplikácie bolo dočasne zablokované, aby sa mohla dokončiť operácia opravy.'
            Uninstall = 'Spustenie tejto aplikácie bolo dočasne zablokované, aby sa mohla dokončiť operácia odinštalovania.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Inštalácia Aplikácie'
            Repair = 'PSAppDeployToolkit - Oprava Aplikácií'
            Uninstall = 'PSAppDeployToolkit - Odinštalovanie Aplikácie'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "Nemáte dostatok miesta na disku na dokončenie inštalácie:`n{0}`n`nPotrebné miesto: {1}MB`nDostupné miesto: {2}`n`nUvoľnite, prosím, dostatok miesta na disku, aby ste mohli pokračovať v inštalácii."
            Oprava = "Nemáte dostatok miesta na disku na dokončenie opravy:`n{0}`n`Potrebné miesto: {1}MB`nPriestor je k dispozícii: {2}`n`nUvoľnite, prosím, dostatok miesta na disku, aby ste mohli pokračovať v oprave."
            Uninstall = "You do not have enough disk space to complete the uninstallation of:`n{0}`n`nSpace required: {1}MB`nPriestor je k dispozícii: {2}`n`nUvoľnite, prosím, dostatok miesta na disku, aby ste mohli pokračovať v odinštalácii."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Inštalácia prebieha. Počkajte prosím...'
            Repair = 'Prebieha oprava. Počkajte prosím...'
            Uninstall = 'Prebieha odinštalovanie. Prosím, počkajte...'
        }
        MessageDetail = @{
            Install = 'Toto okno sa po dokončení inštalácie automaticky zatvorí.'
            Repair = 'Toto okno sa automaticky zatvorí po dokončení opravy.'
            Uninstall = 'Toto okno sa automaticky zatvorí po dokončení odinštalovania.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Inštalácia Aplikácie'
            Repair = 'PSAppDeployToolkit - Oprava Aplikácií'
            Uninstall = 'PSAppDeployToolkit - Odinštalovanie Aplikácie'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Inštalácia Aplikácie'
            Repair = 'PSAppDeployToolkit - Oprava Aplikácií'
            Uninstall = 'PSAppDeployToolkit - Odinštalovanie Aplikácie'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimalizovať'
        ButtonRestartNow = 'Reštartovať Teraz'
        Message = @{
            Install = 'Aby sa inštalácia dokončila, musíte reštartovať počítač.'
            Repair = 'Aby sa oprava dokončila, musíte reštartovať počítač.'
            Uninstall = 'Aby sa odinštalovanie dokončilo, musíte reštartovať počítač.'
        }
        MessageRestart = 'Váš počítač sa automaticky reštartuje na konci odpočítavania.'
        MessageTime = 'Uložte si svoju prácu a reštartujte ju v stanovenom čase.'
        TimeRemaining = 'Zostávajúci čas:'
        Title = 'Vyžaduje sa Reštart'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Inštalácia Aplikácie'
            Repair = 'PSAppDeployToolkit - Oprava Aplikácií'
            Uninstall = 'PSAppDeployToolkit - Odinštalovanie Aplikácie'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Zatvoriť &Programy'
                ButtonContinue = '&Pokračovať'
                ButtonContinueTooltip = 'Vyberte „Pokračovať“ až po zatvorení vyššie uvedených aplikácií.'
                ButtonDefer = '&Odloženie'
                CountdownMessage = 'POZNÁMKA: Program(-y) sa automaticky ukončí(-ú) v:'
                Message = @{
                    Install = "Pred pokračovaním inštalácie je potrebné zatvoriť nasledujúce programy.`n`nProsím, uložte si prácu, zatvorte programy a potom pokračujte. Prípadne uložte svoju prácu a kliknite na `„Zatvoriť programy`“."
                    Repair = "Pred pokračovaním opravy musia byť nasledujúce programy zatvorené.`n`nProsím, uložte svoju prácu, zatvorte programy a potom pokračujte. Prípadne uložte svoju prácu a kliknite na `„Zatvoriť programy`“."
                    Uninstall = "Predtým, ako bude možné pokračovať v odinštalovaní, musia byť nasledujúce programy zatvorené.`n`nProsím, uložte svoju prácu, zatvorte programy a potom pokračujte. Prípadne uložte svoju prácu a kliknite na `„Zavrieť programy`“."
                }
            }
            Defer = @{
                Deadline = 'Termín:'
                ExpiryMessage = @{
                    Install = 'Môžete sa rozhodnúť odložiť inštaláciu až do uplynutia odkladu:'
                    Repair = 'Môžete sa rozhodnúť odložiť opravu až do uplynutia odkladu:'
                    Uninstall = 'Môžete sa rozhodnúť odložiť odinštalovanie až do uplynutia odkladu:'
                }
                RemainingDeferrals = 'Zostávajúce odklady:'
                WarningMessage = 'Po uplynutí odkladu už nebudete mať možnosť odložiť.'
                WelcomeMessage = @{
                    Install = 'Nasledujúca aplikácia sa práve inštaluje:'
                    Repair = 'Nasledujúca aplikácia bude opravená:'
                    Uninstall = 'Nasledujúca aplikácia bude odinštalovaná:'
                }
            }
            CountdownMessage = @{
                Install = 'Inštalácia bude automaticky pokračovať v:'
                Repair = 'Oprava bude automaticky pokračovať za:'
                Uninstall = 'Odinštalovanie bude automaticky pokračovať za:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Inštalácia Aplikácie'
                Repair = 'PSAppDeployToolkit - Oprava Aplikácií'
                Uninstall = 'PSAppDeployToolkit - Odinštalovanie Aplikácie'
            }
            DialogMessage = 'Pred pokračovaním uložte svoju prácu, pretože nasledujúce aplikácie budú automaticky ukončené.'
            DialogMessageNoProcesses = @{
                Install = 'Prosím, vyberte Install, aby ste mohli pokračovať v inštalácii. Ak máte ešte nejaké odklady, môžete tiež zvoliť odloženie inštalácie.'
                Repair = 'Prosím, vyberte Repair (Opraviť), ak chcete pokračovať v oprave. Ak máte ešte nejaké odklady, môžete opravu odložiť.'
                Uninstall = 'Vyberte Uninstall (Odinštalovať), ak chcete pokračovať v odinštalovaní. Ak máte ešte nejaké odklady, môžete tiež odložiť odinštalovanie.'
            }
            ButtonDeferRemaining = 'zostať'
            ButtonLeftText = 'Odložiť'
            ButtonRightText = @{
                Install = 'Zavrieť aplikácie a Nainštalovať'
                Repair = 'Zatvoriť aplikácie a Opraviť'
                Uninstall = 'Zatvoriť aplikácie a Odinštalovať'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Inštalovať'
                Repair = 'Opraviť'
                Uninstall = 'Odinštalovať'
            }
        }
    }
}
