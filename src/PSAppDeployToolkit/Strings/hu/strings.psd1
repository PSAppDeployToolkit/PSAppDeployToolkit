﻿@{
    BalloonText = @{
        Complete = @{
            Install = 'A telepítés befejeződött.'
            Repair = 'Javítás befejeződött.'
            Uninstall = 'Az eltávolítás befejeződött.'
        }
        Error = @{
            Install = 'Telepítés sikertelen.'
            Repair = 'A javítás sikertelen.'
            Uninstall = 'Az eltávolítás sikertelen.'
        }
        FastRetry = @{
            Install = 'Telepítés nem fejeződött be.'
            Repair = 'Javítás nem fejeződött be.'
            Uninstall = 'Az eltávolítás nem fejeződött be.'
        }
        RestartRequired = @{
            Install = 'Telepítés befejeződött. Újraindítás szükséges.'
            Repair = 'Javítás befejeződött. Újraindítás szükséges.'
            Uninstall = 'Az eltávolítás befejeződött. Újraindítás szükséges.'
        }
        Start = @{
            Install = 'A telepítés megkezdődött.'
            Repair = 'A javítás megkezdődött.'
            Uninstall = 'Az eltávolítás megkezdődött.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = 'Az alkalmazás indítása ideiglenesen blokkolva van, hogy egy telepítési művelet befejeződhessen.'
            Repair = 'Az alkalmazás indítása ideiglenesen blokkolva van, hogy egy javítási művelet befejeződhessen.'
            Uninstall = 'Az alkalmazás indítása ideiglenesen blokkolva van, hogy egy eltávolítási művelet befejeződhessen.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Alkalmazás Telepítése'
            Repair = 'PSAppDeployToolkit - Alkalmazás Javítása'
            Uninstall = 'PSAppDeployToolkit - Alkalmazás Eltávolítása'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "Nincs elég lemezterület a telepítés befejezéséhez:`n{0}`n`nSzükséges hely: {1}MB`n`: {2}MB`n`nKérjük, szabadítson fel elegendő lemezterületet a telepítés folytatásához."
            Repair = "Nincs elég lemezterület a javítás befejezéséhez:`n{0}`n`nSzükséges hely: {1}MB`n`: {2}MB`n`nKérem, szabadítson fel elegendő lemezterületet a javítás folytatásához."
            Uninstall = "Nincs elég lemezterület a következő eltávolításának befejezéséhez:`n{0}`n`nSzükséges hely: {1}MB`n: {2}MB`n`nKérem, szabadítson fel elegendő lemezterületet az eltávolítás folytatásához."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Telepítés folyamatban. Kérjük várjon...'
            Repair = 'Javítás folyamatban. Kérjük várjon...'
            Uninstall = 'Eltávolítás folyamatban. Kérjük várjon...'
        }
        MessageDetail = @{
            Install = 'Ez az ablak automatikusan bezáródik, ha a telepítés befejeződött.'
            Repair = 'Ez az ablak automatikusan bezáródik, ha a javítás befejeződött.'
            Uninstall = 'Ez az ablak automatikusan bezáródik, amikor az eltávolítás befejeződött.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Alkalmazás Telepítése'
            Repair = 'PSAppDeployToolkit - Alkalmazás Javítása'
            Uninstall = 'PSAppDeployToolkit - Alkalmazás Eltávolítása'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Alkalmazás Telepítése'
            Repair = 'PSAppDeployToolkit - Alkalmazás Javítása'
            Uninstall = 'PSAppDeployToolkit - Alkalmazás Eltávolítása'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimalizálás'
        ButtonRestartNow = 'Újraindítás most'
        Message = @{
            Install = 'A telepítés befejezéséhez újra kell indítania a számítógépet.'
            Repair = 'A javítás befejezéséhez újra kell indítania a számítógépet.'
            Uninstall = 'Az eltávolítás befejezéséhez újra kell indítania a számítógépet.'
        }
        MessageRestart = 'A visszaszámlálás végén a számítógép automatikusan újraindul.'
        MessageTime = 'Kérjük, mentse el munkáját, és indítsa újra a megadott időn belül.'
        TimeRemaining = 'A hátralévő idő:'
        Title = 'Újraindítás szükséges'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Alkalmazás Telepítése'
            Repair = 'PSAppDeployToolkit - Alkalmazás Javítása'
            Uninstall = 'PSAppDeployToolkit - Alkalmazás Eltávolítása'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Close &Programok bezárása'
                ButtonContinue = '&Folytatás'
                ButtonContinueTooltip = 'Csak a fent felsorolt alkalmazás(ok) bezárása után válassza a „Folytatás” lehetőséget.'
                ButtonDefer = '&Elhalasztás'
                CountdownMessage = 'MEGJEGYZÉS: A program(ok) automatikusan bezárul(nak):'
                Message = @{
                    Install = "A következő programokat be kell zárni, mielőtt a telepítés folytatódhat.`n`nKérjük, mentse el munkáját, zárja be a programokat, majd folytassa. Alternatív megoldásként mentse el a munkáját, és kattintson a `„Programok bezárása`” gombra."
                    Repair = "A következő programokat be kell zárni, mielőtt a javítás folytatódhat.`n`nKérjük, mentse el munkáját, zárja be a programokat, majd folytassa. Másik lehetőségként mentse el a munkáját, és kattintson a `„Programok bezárása`” gombra."
                    Uninstall = "Az alábbi programokat be kell zárni, mielőtt az eltávolítás folytatódhat.`n`nKérjük, mentse el munkáját, zárja be a programokat, majd folytassa. Másik lehetőségként mentse el a munkáját, és kattintson a `„Programok bezárása`” gombra."
                }
            }
            Defer = @{
                Deadline = 'Határidő:'
                ExpiryMessage = @{
                    Install = 'A telepítést a halasztás lejártáig elhalaszthatja:'
                    Repair = 'A javítást a halasztás lejártáig elhalaszthatja:'
                    Uninstall = 'Az eltávolítást elhalaszthatja a halasztás lejártáig:'
                }
                RemainingDeferrals = 'Maradék halasztások:'
                WarningMessage = 'Ha a halasztás lejár, többé nem lesz lehetősége a halasztásra.'
                WelcomeMessage = @{
                    Install = 'A következő alkalmazás telepítése folyamatban van:'
                    Repair = 'A következő alkalmazás javításra kerül:'
                    Uninstall = 'A következő alkalmazás eltávolítása folyamatban van:'
                }
            }
            CountdownMessage = @{
                Install = 'A telepítés automatikusan folytatódik:'
                Repair = 'A javítás automatikusan folytatódik:'
                Uninstall = 'Az eltávolítás automatikusan folytatódik:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Alkalmazás Telepítése'
                Repair = 'PSAppDeployToolkit - Alkalmazás Javítása'
                Uninstall = 'PSAppDeployToolkit - Alkalmazás Eltávolítása'
            }
            DialogMessage = 'Kérjük, mentse el munkáját, mielőtt folytatná, mivel a következő alkalmazások automatikusan bezárásra kerülnek.'
            DialogMessageNoProcesses = @{
                Install = 'A telepítés folytatásához válassza a Telepítés lehetőséget. Ha van még halasztása, akkor a telepítés elhalasztását is választhatja.'
                Repair = 'A javítás folytatásához válassza a Repair (Javítás) lehetőséget. Ha van még halasztása, akkor a javítás elhalasztását is választhatja.'
                Eltávolítás = 'Kérjük, válassza az Eltávolítás lehetőséget az eltávolítás folytatásához. Ha van még halasztása, akkor az eltávolítás elhalasztását is választhatja.'
            }
            ButtonDeferRemaining = 'marad'
            ButtonLeftText = 'Halasztás'
            ButtonRightText = @{
                Install = 'Alkalmazások Bezárása és Telepítése'
                Repair = 'Alkalmazások Bezárása és Javítása'
                Uninstall = 'Alkalmazások Bezárása és Eltávolítása'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Telepítés'
                Repair = 'Javítás'
                Uninstall = 'Eltávolítás'
            }
        }
    }
}
