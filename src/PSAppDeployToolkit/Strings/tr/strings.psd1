@{
    BalloonText = @{
        Complete = "tamamlandı."
        Error = "hata oluştu."
        FastRetry = "tamamlanamadı."
        RestartRequired = "tamamlandı. Yeniden başlatma gereklidir."
        Start = "başladı."
    }
    BlockExecution = @{
        Message = "Yükleme işleminin tamamlanabilmesi için bu uygulamanın başlatılması geçici olarak engellenmiştir."
    }
    ClosePrompt = @{
        ButtonClose = "Uygulamaları kapat"
        ButtonContinue = "Devam et"
        ButtonContinueTooltip = "Aşağıdaki listedeki uygulamaları kapatıp `"Devam et`"i seçiniz."
        ButtonDefer = "Ertele"
        CountdownMessage = "NOT: Program(lar) otomatik olarak kapanacaktır:"
        Message = "Kurulumun devam edebilmesi için aşağıdaki programlar kapatılmalıdır.`n`nLütfen çalışmanızı kaydedin, programları kapatın ve ardından devam edin.`nAlternatif olarak, çalışmanızı kaydedin ve `"Programları Kapat `"a tıklayın."
    }
    DeferPrompt = @{
        Deadline = "Son tarih:"
        ExpiryMessage = "Erteleme süresi dolana kadar kurulumu ertelemeyi seçebilirsiniz:"
        RemainingDeferrals = "Kalan Ertelemeler:"
        WarningMessage = "Erteleme süresi sona erdiğinde, artık erteleme seçeneğiniz olmayacaktır."
        WelcomeMessage = "Aşağıdaki uygulama yüklenmek üzeredir:"
    }
    DeploymentType = @{
        Install = "Kurulum işlemi"
        Repair = "Onarım"
        Uninstall = "Kaldırma işlemi"
    }
    DiskSpace = @{
        Message = "Kurulumu tamamlamak için yeterli disk alanınız yok:`n{0}`n`nGerekli alan: {1}MB`nMevcut alan: {2}MB`n`nKuruluma devam etmek için lütfen yeterli disk alanı boşaltın."
    }
    Progress = @{
        MessageInstall = "Kurulum devam ediyor. Lütfen bekleyiniz..."
        MessageInstallDetail = "Kurulum tamamlandığında bu pencere otomatik olarak kapanacaktır."
        MessageRepair = "Onarım devam ediyor. Lütfen bekleyiniz..."
        MessageRepairDetail = "Onarım tamamlandığında bu pencere otomatik olarak kapanacaktır."
        MessageUninstall = "Kaldırma işlemi devam ediyor. Lütfen bekleyiniz..."
        MessageUninstallDetail = "Kaldırma işlemi tamamlandığında bu pencere otomatik olarak kapanacaktır."
    }
    RestartPrompt = @{
        ButtonRestartLater = "Simge durumuna küçült"
        ButtonRestartNow = "Bilgisayarı yeniden başlat"
        Message = "Yüklemenin tamamlanması için bilgisayarınızı yeniden başlatmanız gerekir."
        MessageRestart = "Geri sayımın sonunda bilgisayarınız otomatik olarak yeniden başlatılacaktır."
        MessageTime = "Lütfen çalışmanızı kaydedin ve belirtilen süre içinde yeniden başlatın."
        TimeRemaining = "Kalan süre:"
        Title = "Yeniden başlatma gerekmektedir"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} otomatik olarak devam edecektir:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - Uygulama {0}'
            DialogMessage = 'Aşağıdaki uygulamalar otomatik olarak kapatılacağından devam etmeden önce lütfen çalışmanızı kaydedin.'
            DialogMessageNoProcesses = "Kuruluma devam etmek için lütfen Yükle'yi seçin. Kalan ertelemeleriniz varsa, kurulumu ertelemeyi de seçebilirsiniz."
            ButtonDeferRemaining = 'kalır'
            ButtonLeftText = 'Erteleme'
            ButtonRightText = 'Uygulamaları Kapat ve Yükle'
            ButtonRightTextNoProcesses = 'Kurulum'
        }
    }
}
