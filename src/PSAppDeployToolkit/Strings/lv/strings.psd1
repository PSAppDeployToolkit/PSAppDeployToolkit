﻿@{
    BalloonText = @{
        Complete = @{
            Install = 'Uzstādīšana pabeigta.'
            Repair = 'Labošana pabeigta.'
            Uninstall = 'Atinstalēšana pabeigta.'
        }
        Error = @{
            Install = 'Instalēšana neizdevās.'
            Repair = 'Labošana neizdevās.'
            Uninstall = 'Atinstalēšana neizdevās.'
        }
        FastRetry = @{
            Install = 'Instalēšana nav pabeigta.'
            Repair = 'Labošana nav pabeigta.'
            Uninstall = 'Atinstalēšana nav pabeigta.'
        }
        RestartRequired = @{
            Install = 'Uzstādīšana pabeigta. Nepieciešams restartēt datoru.'
            Repair = 'Labošana pabeigta. Nepieciešams restartēt datoru.'
            Uninstall = 'Atinstalēšana pabeigta. Nepieciešams restartēt datoru.'
        }
        Start = @{
            Install = 'Uzstādīšana uzsākta.'
            Repair = 'Uzsākta labošana.'
            Uninstall = 'Uzsākta atinstalēšana.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = 'Šīs lietojumprogrammas palaišana ir uz laiku bloķēta, lai varētu pabeigt instalēšanas operāciju.'
            Repair = 'Šīs lietojumprogrammas palaišana ir uz laiku bloķēta, lai varētu pabeigt labošanas operāciju.'
            Uninstall = 'Šīs lietojumprogrammas palaišana ir uz laiku bloķēta, lai varētu pabeigt atinstalēšanas operāciju.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Lietotņu Instalēšana'
            Repair = 'PSAppDeployToolkit - Lietotņu Labošana'
            Uninstall = 'PSAppDeployToolkit - Lietotņu Atinstalēšana'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "Jums nav pietiekami daudz vietas uz diska, lai pabeigtu instalēšanu:`n{0}`n`nNepieciešamā vieta: {1}MB`nPieejamā vieta: {2}MB`n`nLūdzu, atbrīvojiet pietiekami daudz vietas uz diska, lai varētu turpināt instalēšanu."
            Repair = "Jums nav pietiekami daudz vietas uz diska, lai pabeigtu labošanu:`n{0}`n`nNepieciešamā vieta: {1}MB`nPieejamā vieta: {2}MB`n`nLūdzu, atbrīvojiet pietiekami daudz vietas uz diska, lai varētu turpināt labošanu."
            Uninstall = "Jums nav pietiekami daudz vietas uz diska, lai pabeigtu atinstalēšanu no:`n{0}`n`nNepieciešamā vieta: {1}MB`nPieejamā vieta: {2}MB`n`nLūdzu, atbrīvojiet pietiekami daudz vietas diskā, lai varētu turpināt atinstalēšanu."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Notiek instalēšana. Lūdzu, uzgaidiet...'
            Repair = 'Notiek labošana. Lūdzu, uzgaidiet...'
            Uninstall = 'Notiek atinstalēšana. Lūdzu, uzgaidiet...'
        }
        MessageDetail = @{
            Install = 'Šis logs aizvērsies automātiski, kad instalēšana būs pabeigta.'
            Repair = 'Šis logs aizvērsies automātiski, kad labošana būs pabeigta.'
            Uninstall = 'Šis logs aizvērsies automātiski, kad būs pabeigta atinstalēšana.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Lietotņu Instalēšana'
            Repair = 'PSAppDeployToolkit - Lietotņu Labošana'
            Uninstall = 'PSAppDeployToolkit - Lietotņu Atinstalēšana'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Lietotņu Instalēšana'
            Repair = 'PSAppDeployToolkit - Lietotņu Labošana'
            Uninstall = 'PSAppDeployToolkit - Lietotņu Atinstalēšana'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Minimizēt'
        ButtonRestartNow = 'Restartēt tagad'
        Message = @{
            Install = 'Lai instalēšana tiktu pabeigta, dators ir jārestartē.'
            Repair = 'Lai labošana tiktu pabeigta, dators jārestartē.'
            Uninstall = 'Lai pabeigtu atinstalēšanu, dators jārestartē.'
        }
        MessageRestart = 'Jūsu dators tiks automātiski restartēts pēc laika atksaites beigām.'
        MessageTime = 'Lūdzu, saglabājiet savu darbu un restartējiet datoru atļautajā laikā.'
        TimeRemaining = 'Atlikušais laiks:'
        Title = 'Nepieciešams restartēt datoru'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Lietotņu Instalēšana'
            Repair = 'PSAppDeployToolkit - Lietotņu Labošana'
            Uninstall = 'PSAppDeployToolkit - Lietotņu Atinstalēšana'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = 'Aizvērt &Programmas'
                ButtonContinue = '&Turpināt'
                ButtonContinueTooltip = 'Izvēlieties “Turpināt” tikai pēc iepriekš minētās(-o) programmas(-u) slēgšanas.'
                ButtonDefer = '&Atlikt'
                CountdownMessage = 'PIEZĪME: Programma(-as) tiks automātiski aizvērta(-as):'
                Message = @{
                    Install = "Pirms instalēšanas turpināšanas ir jāaizver šādas programmas.`n`nLūdzu, saglabājiet savu darbu, aizveriet programmas un pēc tam turpiniet. Vai arī saglabājiet darbu un noklikšķiniet uz `“Aizvērt programmas`”."
                    Repair = "Pirms var turpināt labošanu, ir jāaizver šādas programmas.`n`nLūdzu, saglabājiet savu darbu, aizveriet programmas un pēc tam turpiniet. Vai arī saglabājiet darbu un noklikšķiniet uz `“Aizvērt programmas`”."
                    Uninstall = "Pirms var turpināt atinstalēšanu, ir jāaizver šādas programmas.`n`nLūdzu, saglabājiet savu darbu, aizveriet programmas un pēc tam turpiniet. Vai arī saglabājiet darbu un noklikšķiniet uz `“Aizvērt programmas`”."
                }
            }
            Defer = @{
                Deadline = 'Termiņš:'
                ExpiryMessage = @{
                    Install = 'Jūs varat izvēlēties atlikt instalēšanu līdz atlikšanas termiņa beigām:'
                    Repair = 'Jūs varat izvēlēties atlikt labošanu līdz atlikšanas termiņa beigām:'
                    Uninstall = 'Jūs varat izvēlēties atlikt atinstalēšanu līdz atlikšanas termiņa beigām:'
                }
                RemainingDeferrals = 'Iespējas atlikt:'
                WarningMessage = 'Kad atlikšanas termiņš būs beidzies, vairs nebūs iespējas atlikt.'
                WelcomeMessage = @{
                    Install = 'Tiks instalēta šāda lietojumprogramma:'
                    Repair = 'Tiks remontēta šāda lietojumprogramma:'
                    Uninstall = 'Tiks atinstalēta šāda programma:'
                }
            }
            CountdownMessage = @{
                Install = 'Instalēšana automātiski turpināsies:'
                Repair = 'Labošana automātiski turpināsies pēc:'
                Uninstall = 'Atinstalēšana automātiski turpināsies pēc:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Lietotņu Instalēšana'
                Repair = 'PSAppDeployToolkit - Lietotņu Labošana'
                Uninstall = 'PSAppDeployToolkit - Lietotņu Atinstalēšana'
            }
            DialogMessage = 'Lūdzu, saglabājiet savu darbu pirms turpināšanas, jo šādas lietojumprogrammas tiks automātiski aizvērtas.'
            DialogMessageNoProcesses = @{
                Install = 'Lūdzu, izvēlieties Instalēt, lai turpinātu instalēšanu. Ja jums ir pieejama iespēja atlikt, varat arī izvēlēties atlikt instalēšanu.'
                Repair = 'Lūdzu, izvēlieties Labot, lai turpinātu labošanu. Ja jums ir pieejama iespēja atlikt, varat arī izvēlēties atlikt labošanu.'
                Uninstall = 'Lūdzu, izvēlieties Atinstalēt, lai turpinātu atinstalēšanu. Ja jums ir pieejama iespēja atlikt, varat arī izvēlēties atlikt atinstalēšanu.'
            }
            ButtonDeferRemaining = 'atlikušas'
            ButtonLeftText = 'Atlikt'
            ButtonRightText = @{
                Install = 'Aizvērt programmas un Instalēt'
                Repair = 'Aizvērt programmas un Labot'
                Uninstall = 'Aizvērt programmas un Atinstalēt'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Instalēt'
                Repair = 'Labot'
                Uninstall = 'Atinstalēt'
            }
        }
    }
}
