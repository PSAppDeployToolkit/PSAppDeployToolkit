@{
    BalloonText = @{
        Complete = "elkészült."
        Error = "sikertelen."
        FastRetry = "nem lehet befejezni."
        RestartRequired = "elkészült.Újraindítás szükséges."
        Start = "elindult."
    }
    BlockExecution = @{
        Message = "A következő alkalmazások blokkolva lesznek, annak érdekében hogy a telepítés problémamentesen végrehajtódjon."
    }
    ClosePrompt = @{
        ButtonClose = "Alkalmazások bezárása"
        ButtonContinue = "Tovább"
        ButtonContinueTooltip = "Csak azután kattintson a `"Tovább`"-ra, ha a fentebb látható alkalmazás(oka)t bezárta."
        ButtonDefer = "Elhalaszt"
        CountdownMessage = "Megjegyzés: a programok automatikusan bezárásra kerülnek,:"
        Message = "Az alábbi programokat szíveskedjen bezárni, mielőtt a telepítés elkezdődik.`n`nKérjük mentse munkáját és a folytatáshoz zárja be a futó alkalmazásokat. Vagy Kérjük mentse munkáját és kattintson a `"Programok bezárása`"-ra."
    }
    DeferPrompt = @{
        Deadline = "Időpont:"
        ExpiryMessage = "A telepítést elhalaszthatja amíg a rendelkezésre álló idő lejár:"
        RemainingDeferrals = "Fennmaradó halasztás:"
        WarningMessage = "Amennyiben a rendelkezésre álló idő letelik, nem lesz lehetősége a telepítés elhalasztására."
        WelcomeMessage = "A következő alkalmazások telepítésre kerülnek:"
    }
    DeploymentType = @{
        Install = "Telepítés"
        Repair = "Javítás"
        Uninstall = "Eltávolítás"
    }
    DiskSpace = @{
        Message = "Nincs elég lemezterület a telepítés végrehajtásához:`n{0}`n`nSzükséges lemezterület: {1}MB`nSzabad lemezterület: {2}MB`nKérem szabadítson fel elegendő lemezterületet a telepítés végrehajtásához."
    }
    Progress = @{
        MessageInstall = "Telepítés folyamatban. Kérem várjon..."
        MessageInstallDetail = "Ez az ablak automatikusan bezáródik, amikor a telepítés befejeződik."
        MessageRepair = "Javítás folyamatban. Kérem várjon..."
        MessageRepairDetail = "Ez az ablak automatikusan bezáródik, ha a javítás befejeződött."
        MessageUninstall = "Eltávolítás folyamatban. Kérem várjon..."
        MessageUninstallDetail = "Ez az ablak automatikusan bezáródik, amikor az eltávolítás befejeződik."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Minimalizál"
        ButtonRestartNow = "Újraindítás most"
        Message = "A telepítés befejezéséhez a számítógépet újraindítása szükséges."
        MessageRestart = "A hátralévő idő leteltével a számítógép újraindul."
        MessageTime = "Kérem mentse munkáját, és a megadott időn belül indítsa újra.."
        TimeRemaining = "Hátralévő idő:"
        Title = "Újraindítás szükséges"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "A(z) {0} automatikusan folytatódik:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - Alkalmazás {0}'
            DialogMessage = 'Kérjük, mentse el a munkáját, mielőtt folytatná, mivel a következő alkalmazások automatikusan lezárulnak.'
            DialogMessageNoProcesses = 'Please select Install to continue with the installation. If you have any deferrals remaining, you may also choose to delay the installation.'
            ButtonDeferRemaining = 'maradjon'
            ButtonLeftText = 'Elhalasztás'
            ButtonRightText = 'Alkalmazások bezárása és telepítése'
            ButtonRightTextNoProcesses = 'Telepítse a'
        }
    }
}
