@{
    BalloonTip = @{
        Start = @{
            Install = '安裝開始。'
            Repair = '修復開始。'
            Uninstall = '卸載開始。'
        }
        Complete = @{
            Install = '安裝完成。'
            Repair = '修復完成。'
            Uninstall = '解除安裝完成。'
        }
        RestartRequired = @{
            Install = '安裝完成。需要重新啟動。'
            Repair = '修復完成。需要重新啟動。'
            Uninstall = '卸載完成。需要重新啟動。'
        }
        FastRetry = @{
            Install = '安裝未完成。'
            Repair = '修復未完成。'
            Uninstall = '解除安裝未完成。'
        }
        Error = @{
            Install = '安裝失敗。'
            Repair = '維修失敗。'
            Uninstall = '解除安裝失敗。'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = '啟動此應用程式已被暫時阻止，以便完成安裝作業。'
            Repair = '啟動此應用程式已被暫時阻止，以便完成修復操作。'
            Uninstall = '啟動此應用程式已暫時受阻，以便完成卸載作業。'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - 應用程式安裝'
            Repair = '{Toolkit\CompanyName} - 應用程式維修'
            Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "您沒有足夠的磁碟空間來完成安裝：`n{0}`n`n所需空間： {1}MB `n 可用空間： {2}MB`n`n請釋放足夠的磁碟空間，以便繼續安裝。"
            Repair = "您沒有足夠的磁碟空間來完成修復:`n{0}`n`n所需空間： {1}MB`n可用空間： {2}MB`n`n請釋放足夠的磁碟空間，以便繼續進行修復。"
            Uninstall = "您沒有足夠的磁碟空間來完成卸載:`n{0}`n`n所需空間： {1}MB`n可用空間： {2}MB`n`n請釋放足夠的磁碟空間，以便繼續卸載。"
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - 應用程式安裝'
            Repair = '{Toolkit\CompanyName} - 應用程式維修'
            Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = '安裝中。 請稍候…'
            Repair = '維修中。 請稍候…'
            Uninstall = '正在卸載。 請稍候…'
        }
        MessageDetail = @{
            Install = '安裝完成後，此視窗將自動關閉。'
            Repair = '維修完成後，此視窗將自動關閉。'
            Uninstall = '卸載完成後，此視窗將自動關閉。'
        }
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - 應用程式安裝'
            Repair = '{Toolkit\CompanyName} - 應用程式維修'
            Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = '最小化'
        ButtonRestartNow = '現在重新啟動'
        Message = @{
            Install = '為了完成安裝，您必須重新啟動電腦。'
            Repair = '為了完成維修，您必須重新啟動電腦。'
            Uninstall = '為了讓卸載完成，您必須重新啟動電腦。'
        }
        CustomMessage = ''
        MessageRestart = '您的電腦會在倒數計時結束時自動重新啟動。'
        MessageTime = '請儲存您的工作，並在指定時間內重新啟動。'
        TimeRemaining = '剩餘時間：'
        Title = '需要重新啟動'
        Subtitle = @{
            Install = '{Toolkit\CompanyName} - 應用程式安裝'
            Repair = '{Toolkit\CompanyName} - 應用程式維修'
            Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = '下列應用程式即將安裝：'
                Repair = '下列應用程式即將被修復：'
                Uninstall = '下列應用程式即將被解除安裝：'
            }
            CloseAppsMessage = @{
                Install = "在繼續安裝之前，必須關閉下列程式。`n`n請儲存您的工作，關閉程式，然後繼續。或者，保存您的工作，然後按一下`「關閉程式`」。"
                Repair = "在進行修復之前，必須關閉下列程式。`n`n請儲存您的工作，關閉程式，然後繼續。或者，保存您的工作并单击 「关闭程序」。"
                Uninstall = "卸載程式前，必須先關閉下列程式。或者，保存您的工作並點擊 「關閉程式」。"
            }
            ExpiryMessage = @{
                Install = '您可以選擇延遲安裝，直到延遲到期:'
                Repair = '您可以選擇延遲修復，直到延遲到期：'
                Uninstall = '您可以選擇延遲卸載，直到延遲期限到期：'
            }
            DeferralsRemaining = '剩餘的延遲：'
            DeferralDeadline = '截止日期:'
            ExpiryWarning = '一旦延遲到期，您將無法再選擇延遲。'
            CountdownDefer = @{
                Install = '安裝會自動繼續：'
                Repair = '維修會自動繼續進行：'
                Uninstall = '卸載將自動繼續中：'
            }
            CountdownClose = '注意：程式會自動關閉：'
            ButtonClose = '關閉程式'
            ButtonDefer = '延遲'
            ButtonContinue = '繼續'
            ButtonContinueTooltip = '僅在關閉上述列出的應用程式後選擇 「繼續」。'
        }
        Fluent = @{
            DialogMessage = '請先保存您的工作再繼續，因為下列應用程式會自動關閉。'
            DialogMessageNoProcesses = @{
                Install = '請選擇「安裝」繼續安裝。 如果您有任何剩餘的延遲，您也可以選擇延遲安裝。'
                Repair = '請選擇「維修」繼續進行維修。 如果您有任何剩餘的延遲，您也可以選擇延遲修復。'
                Uninstall = '請選擇「卸載」繼續進行卸載。 如果您有任何剩餘的延遲，您也可以選擇延遲解除安裝。'
            }
            AutomaticStartCountdown = '自動啟動倒數計時'
            DeferralsRemaining = '剩餘延期'
            DeferralDeadline = '延期截止日期'
            ButtonLeftText = @{
                Install = '關閉應用程式並安裝'
                Repair = '關閉應用程式與修復'
                Uninstall = '關閉應用程式並卸載'
            }
            ButtonLeftNoProcessesText = @{
                Install = '安裝'
                Repair = '修復'
                Uninstall = '解除安裝'
            }
            ButtonRightText = '延遲'
            Subtitle = @{
                Install = '{Toolkit\CompanyName} - 應用程式安裝'
                Repair = '{Toolkit\CompanyName} - 應用程式維修'
                Uninstall = '{Toolkit\CompanyName} - 應用程式卸載'
            }
        }
        CustomMessage = ''
    }
}
