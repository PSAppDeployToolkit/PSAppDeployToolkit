@{
    BalloonText = @{
        Complete = "完成。"
        Error = "失敗。"
        FastRetry = "未完成。"
        RestartRequired = "完成。需重啟。"
        Start = "已啟動。"
    }
    BlockExecution = @{
        Message = "為完成安裝過程，暫時禁止啟動本款應用程式。"
    }
    ClosePrompt = @{
        ButtonClose = "關閉程序"
        ButtonContinue = "繼續"
        ButtonContinueTooltip = "關閉上列應用程式後才選擇`"繼續`"。"
        ButtonDefer = "延遲"
        CountdownMessage = "注：下列程序將自動關閉："
        Message = "在繼續安裝前必須關閉下列程序。`n`n請保存您的工作，關閉程序，然後繼續。 或者保存您的工作，然後點擊`"關閉程序`"。"
    }
    DeferPrompt = @{
        Deadline = "最後期限："
        ExpiryMessage = "在延期失效前，可選擇延遲安裝："
        RemainingDeferrals = "所剩延期："
        WarningMessage = "延期失效後，再也無法延遲安裝。"
        WelcomeMessage = "即將安裝下列應用程式："
    }
    DeploymentType = @{
        Install = "安裝"
        Repair = "修復"
        Uninstall = "卸載"
    }
    DiskSpace = @{
        Message = "沒有足夠的磁盤空間來完成下列安裝：`n{0}`n`n所需空間： {1}MB`n可用空間： {2}MB`n`n請釋放足夠的磁盤空間以繼續安裝。"
    }
    Progress = @{
        MessageInstall = "安裝中。請稍等。。。"
        MessageInstallDetail = "安裝完成後，此視窗會自動關閉。。。"
        MessageRepair = "修復中。請稍等。。。"
        MessageRepairDetail = "修復完成後，此視窗會自動關閉。。。"
        MessageUninstall = "卸載中。請稍等。。。"
        MessageUninstallDetail = "卸載完成後，此視窗將自動關閉。。。"
    }
    RestartPrompt = @{
        ButtonRestartLater = "最小化"
        ButtonRestartNow = "現在重啟"
        Message = "未完成安裝過程，需重啟計算機。"
        MessageRestart = "倒計時結束後，計算機將自動重啟。"
        MessageTime = "請保存您的工作，然後在容許時間重啟計算機。"
        TimeRemaining = "剩餘時間："
        Title = "需重啟"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0}會自動繼續:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - 應用程式 {0}'
            DialogMessage = '由於下列應用程式將自動關閉，請先保存您的工作再繼續。'
            DialogMessageNoProcesses = '請選擇「安裝」繼續安裝。如果您有任何延遲安裝的剩餘時間，您也可以選擇延遲安裝。'
            ButtonDeferRemaining = '留下'
            ButtonLeftText = '延遲'
            ButtonRightText = '關閉應用程式並安裝'
            ButtonRightTextNoProcesses = '安裝'
        }
    }
}
