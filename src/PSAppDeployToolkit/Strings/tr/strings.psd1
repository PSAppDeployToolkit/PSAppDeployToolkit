@{
    BalloonText = @{
        Complete = @{
            Install = 'Kurulum tamamlandı.'
            Repair = 'Onarım tamamlandı.'
            Uninstall = 'Kaldırma işlemi tamamlandı.'
        }
        Error = @{
            Install = 'Kurulum başarısız oldu.'
            Repair = 'Onarım başarısız oldu.'
            Uninstall = 'Kaldırma başarısız oldu.'
        }
        FastRetry = @{
            Install = 'Kurulum tamamlanmadı.'
            Repair = 'Onarım tamamlanmadı.'
            Uninstall = 'Kaldırma tamamlanmadı.'
        }
        RestartRequired = @{
            Install = 'Kurulum tamamlandı. Yeniden başlatma gerekli.'
            Repair = 'Onarım tamamlandı. Yeniden başlatma gerekli.'
            Uninstall = 'Kaldırma tamamlandı. Yeniden başlatma gereklidir.'
        }
        Start = @{
            Install = 'Kurulum başlatıldı.'
            Repair = 'Onarım başlatıldı.'
            Uninstall = 'Kaldırma işlemi başladı.'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = 'Bir yükleme işleminin tamamlanabilmesi için bu uygulamanın başlatılması geçici olarak engellendi.'
            Repair = 'Bir onarım işleminin tamamlanabilmesi için bu uygulamanın başlatılması geçici olarak engellendi.'
            Uninstall = 'Bir kaldırma işleminin tamamlanabilmesi için bu uygulamanın başlatılması geçici olarak engellendi.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Uygulama Yükleme'
            Repair = 'PSAppDeployToolkit - Uygulama Onarımı'
            Uninstall = 'PSAppDeployToolkit - Uygulama Kaldırma'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "Şunun kurulumunu tamamlamak için yeterli disk alanınız yok:`n{0}`n`nSpace required: {1}MB`nMevcut alan: {2}MB`n`nLütfen yüklemeye devam etmek için yeterli disk alanı boşaltın."
            Repair = "Şunun onarımını tamamlamak için yeterli disk alanınız yok:`n{0}`n`nSpace required: {1}MB`nMevcut alan: {2}MB`n`nOnarım işlemine devam etmek için lütfen yeterli disk alanını boşaltın."
            Uninstall = "Kaldırma işlemini tamamlamak için yeterli disk alanınız yok:`n{0}`n`nSpace required: {1}MB`nKullanılabilir alan: {2}MB`n`nKaldırma işlemine devam etmek için lütfen yeterli disk alanı boşaltın."
        }
    }
    Progress = @{
        Message = @{
            Install = 'Kurulum devam ediyor. Lütfen bekleyin...'
            Repair = 'Onarım devam ediyor. Lütfen bekleyin...'
            Uninstall = 'Kaldırma işlemi devam ediyor. Lütfen bekleyin...'
        }
        MessageDetail = @{
            Install = 'Yükleme tamamlandığında bu pencere otomatik olarak kapanacaktır.'
            Repair = 'Onarım tamamlandığında bu pencere otomatik olarak kapanacaktır.'
            Uninstall = 'Kaldırma işlemi tamamlandığında bu pencere otomatik olarak kapanacaktır.'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Uygulama Yükleme'
            Repair = 'PSAppDeployToolkit - Uygulama Onarımı'
            Uninstall = 'PSAppDeployToolkit - Uygulama Kaldırma'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Uygulama Yükleme'
            Repair = 'PSAppDeployToolkit - Uygulama Onarımı'
            Uninstall = 'PSAppDeployToolkit - Uygulama Kaldırma'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = 'Küçült'
        ButtonRestartNow = 'Şimdi Yeniden Başlat'
        Message = @{
            Install = 'Yüklemenin tamamlanması için bilgisayarınızı yeniden başlatmanız gerekir.'
            Repair = 'Onarımın tamamlanması için bilgisayarınızı yeniden başlatmanız gerekir.'
            Uninstall = 'Kaldırma işleminin tamamlanması için bilgisayarınızı yeniden başlatmanız gerekir.'
        }
        MessageRestart = 'Geri sayımın sonunda bilgisayarınız otomatik olarak yeniden başlatılacaktır.'
        MessageTime = 'Lütfen çalışmanızı kaydedin ve ayrılan süre içinde yeniden başlatın.'
        TimeRemaining = 'Kalan süre:'
        Title = 'Yeniden Başlatma Gerekli'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - Uygulama Yükleme'
            Repair = 'PSAppDeployToolkit - Uygulama Onarımı'
            Uninstall = 'PSAppDeployToolkit - Uygulama Kaldırma'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = '&Programları Kapat'
                ButtonContinue = '&Devam etmek'
                ButtonContinueTooltip = 'Yalnızca yukarıda listelenen uygulama(lar)ı kapattıktan sonra “Devam ”ı seçin.'
                ButtonDefer = '&Ertele'
                CountdownMessage = 'NOT: Program(lar) şu süre içinde otomatik olarak kapatılacaktır:'
                Message = @{
                    Install = "Kurulumun devam edebilmesi için aşağıdaki programların kapatılması gerekir.`n`nLütfen çalışmanızı kaydedin, programları kapatın ve sonra devam edin. Alternatif olarak, çalışmanızı kaydedin ve “Programları Kapat” a tıklayın."
                    Repair = "Onarımın devam edebilmesi için aşağıdaki programların kapatılması gerekir.`n`nLütfen çalışmanızı kaydedin, programları kapatın ve devam edin. Alternatif olarak, çalışmanızı kaydedin ve “Programları Kapat” a tıklayın."
                    Uninstall = "Kaldırma işleminin devam edebilmesi için aşağıdaki programların kapatılması gerekir.`n`nLütfen çalışmanızı kaydedin, programları kapatın ve devam edin. Alternatif olarak, çalışmanızı kaydedin ve “Programları Kapat” a tıklayın."
                }
            }
            Defer = @{
                Deadline = 'Son Tarih:'
                ExpiryMessage = @{
                    Install = 'Erteleme süresi dolana kadar yüklemeyi ertelemeyi seçebilirsiniz:'
                    Repair = 'Erteleme süresi dolana kadar onarımı ertelemeyi seçebilirsiniz:'
                    Uninstall = 'Erteleme süresi dolana kadar kaldırma işlemini ertelemeyi seçebilirsiniz:'
                }
                RemainingDeferrals = 'Kalan Ertelemeler:'
                WarningMessage = 'Erteleme süresi sona erdiğinde, artık erteleme seçeneğiniz olmayacaktır.'
                WelcomeMessage = @{
                    Install = 'Aşağıdaki uygulama yüklenmek üzere:'
                    Repair = 'Aşağıdaki uygulama onarılmak üzere:'
                    Uninstall = 'Aşağıdaki uygulama kaldırılmak üzere:'
                }
            }
            CountdownMessage = @{
                Install = 'Kurulum otomatik olarak şu şekilde devam edecektir:'
                Repair = 'Onarım otomatik olarak şu şekilde devam edecek:'
                Uninstall = 'Kaldırma işlemi otomatik olarak şu şekilde devam edecektir:'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - Uygulama Yükleme'
                Repair = 'PSAppDeployToolkit - Uygulama Onarımı'
                Uninstall = 'PSAppDeployToolkit - Uygulama Kaldırma'
            }
            DialogMessage = 'Aşağıdaki uygulamalar otomatik olarak kapatılacağı için devam etmeden önce lütfen çalışmanızı kaydedin.'
            DialogMessageNoProcesses = @{
                Install = "Lütfen yüklemeye devam etmek için Yükle'yi seçin. Kalan ertelemeleriniz varsa, kurulumu ertelemeyi de seçebilirsiniz."
                Repair = "Onarıma devam etmek için lütfen Onar'ı seçin. Kalan ertelemeleriniz varsa onarımı geciktirmeyi de seçebilirsiniz."
                Uninstall = "Kaldırma işlemine devam etmek için lütfen Kaldır'ı seçin. Kalan ertelemeleriniz varsa, kaldırma işlemini ertelemeyi de seçebilirsiniz."
            }
            ButtonDeferRemaining = 'kal'
            ButtonLeftText = 'Ertele'
            ButtonRightText = @{
                Install = 'Uygulamaları Kapat ve Yükle'
                Repair = 'Uygulamaları Kapat ve Onar'
                Uninstall = 'Uygulamaları Kapat ve Kaldır'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'Yükle'
                Repair = 'Onarım'
                Uninstall = 'Kaldır'
            }
        }
    }
}
