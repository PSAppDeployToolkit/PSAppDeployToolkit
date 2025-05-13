@{
    BalloonTip = @{
        Start = @{
            Install = 'インストールが開始されました。'
            Repair = '修復が開始されました。'
            Uninstall = 'アンインストールが開始されました。'
        }
        Complete = @{
            Install = 'インストールが完了しました。'
            Repair = '修復が完了しました。'
            Uninstall = 'アンインストールが完了しました。'
        }
        RestartRequired = @{
            Install = 'インストールが完了しました。再起動が必要です。'
            Repair = '修復が完了しました。再起動が必要です。'
            Uninstall = 'アンインストールが完了しました。再起動が必要です。'
        }
        FastRetry = @{
            Install = 'インストールが完了していません。'
            Repair = '修復が完了していません。'
            Uninstall = 'アンインストールが完了していません。'
        }
        Error = @{
            Install = 'インストールに失敗しました。'
            Repair = '修復に失敗しました。'
            Uninstall = 'アンインストールに失敗しました。'
        }
    }
    BlockExecutionText = @{
        Message = @{
            Install = 'インストール操作を完了させるため、このアプリケーションの起動が一時的にブロックされました。'
            Repair = '修復操作を完了させるため、このアプリケーションの起動が一時的にブロックされました。'
            Uninstall = 'アンインストール操作を完了させるため、このアプリケーションの起動が一時的にブロックされました。'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - アプリケーションのインストール'
            Repair = 'PSAppDeployToolkit - アプリケーションの修復'
            Uninstall = 'PSAppDeployToolkit - アプリケーションのアンインストール'
        }
    }
    DiskSpaceText = @{
        Message = @{
            Install = "インストールを完了するには十分なディスク領域がありません。:`n{0}`n`n必要なディスク領域: {1}MB`n使用可能なディスク領域: {2}MB`n`nインストールを続行するには、十分なディスク領域を確保してください。"
            Repair = "修復を完了するには十分なディスク領域がありません。`n{0}`n`n必要な容量: {1}MB`n使用可能な容量: {2}MB`n`n修復を実行するには、十分なディスク領域を確保してください。"
            Uninstall = "アンインストールを完了するにはディスク容量が不足しています。:`n{0}`n`n必要な容量: {1}MB`n使用可能な容量: {2}MB`n`nアンインストールを実行するには、十分なディスク容量を確保してください。"
        }
    }
    InstallationPrompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - アプリケーションのインストール'
            Repair = 'PSAppDeployToolkit - アプリケーションの修復'
            Uninstall = 'PSAppDeployToolkit - アプリケーションのアンインストール'
        }
    }
    ProgressPrompt = @{
        Message = @{
            Install = 'インストール中です。しばらくお待ちください...'
            Repair = '修復中です。しばらくお待ちください...'
            Uninstall = 'アンインストール中です。しばらくお待ちください...'
        }
        MessageDetail = @{
            Install = 'インストールが完了すると、このウィンドウは自動的に閉じられます。'
            Repair = '修復が完了すると、このウィンドウは自動的に閉じられます。'
            Uninstall = 'アンインストールが完了すると、このウィンドウは自動的に閉じられます。'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - アプリケーションのインストール'
            Repair = 'PSAppDeployToolkit - アプリケーションの修復'
            Uninstall = 'PSAppDeployToolkit - アプリケーションのアンインストール'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = '最小化'
        ButtonRestartNow = '今すぐ再起動'
        Message = @{
            Install = 'インストールを完了するには、コンピュータを再起動する必要があります。'
            Repair = '修復を完了するには、コンピュータを再起動する必要があります。'
            Uninstall = 'アンインストールを完了するには、コンピュータを再起動する必要があります。'
        }
        CustomMessage = ''
        MessageRestart = 'カウントダウンの終了時にコンピュータが自動的に再起動されます。'
        MessageTime = '作業内容を保存し、指定時間内に再起動してください。'
        TimeRemaining = '残り時間:'
        Title = '再起動が必要です'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - アプリケーションのインストール'
            Repair = 'PSAppDeployToolkit - アプリケーションの修復'
            Uninstall = 'PSAppDeployToolkit - アプリケーションのアンインストール'
        }
    }
    CloseAppsPrompt = @{
        Classic = @{
            WelcomeMessage = @{
                Install = '次のアプリケーションがインストールされようとしています。'
                Repair = '以下のアプリケーションを修理中です。'
                Uninstall = '次のアプリケーションがアンインストールされようとしています。'
            }
            CloseAppsMessage = @{
                Install = "インストールを続行するには、次のプログラムを閉じなければなりません。`n`n作業内容を保存し、プログラムを閉じてから続行してください。または、作業内容を保存し、[プログラムの終了] をクリックしてください。"
                Repair = "修復を続行するには、次のプログラムを閉じなければなりません。`n`n作業内容を保存し、プログラムを閉じてから続行してください。または、作業内容を保存し、[プログラムの終了] をクリックしてください。"
                Uninstall = "アンインストールを続行するには、次のプログラムを閉じてください。`n`n作業内容を保存し、プログラムを閉じてから続行してください。または、作業内容を保存し、[プログラムの終了] をクリックしてください。"
            }
            ExpiryMessage = @{
                Install = '延期期間が終了するまでインストールを延期することができます。'
                Repair = '延期期間が切れるまで修理を延期することもできます。'
                Uninstall = '延期期間が切れるまでアンインストールを延期する選択肢もあります。'
            }
            DeferralsRemaining = '繰り延べ残高：'
            DeferralDeadline = '期限:'
            ExpiryWarning = '猶予期間が終了すると、猶予のオプションはなくなります。'
            CountdownDefer = @{
                Install = 'インストールは自動的に続行されます。'
                Repair = '修復は自動的に続行されます。'
                Uninstall = 'アンインストールは自動的に続行されます。'
            }
            CountdownClose = '注意: プログラムは、次の時間で自動的に閉じられます:'
            ButtonClose = '閉じる &Programs'
            ButtonDefer = '&延期'
            ButtonContinue = '&続行'
            ButtonContinueTooltip = '上記にリストされたアプリケーションをすべて閉じた後にのみ、「続行」を選択してください。'
        }
        Fluent = @{
            DialogMessage = '次のアプリケーションが自動的に閉じられるので、作業を保存してから続行してください。'
            DialogMessageNoProcesses = @{
                Install = 'インストールを選択してインストールを続行してください。保留中のタスクが残っている場合は、インストールを延期することもできます。'
                Repair = '修復を続行するには、[修復] を選択してください。 延期されたものが残っている場合は、修復を延期することもできます。'
                Uninstall = 'アンインストールを続行するには、[アンインストール] を選択してください。 延期されたものが残っている場合は、アンインストールを延期することもできます。'
            }
            AutomaticStartCountdown = '自動スタートカウントダウン'
            DeferralsRemaining = '残りの延期'
            DeferralDeadline = '延期期限'
            ButtonLeftText = '延期'
            ButtonRightText = @{
                Install = 'アプリを終了してインストールします。'
                Repair = 'アプリを終了して修理'
                Uninstall = 'アプリを終了してアンインストールします。'
            }
            ButtonRightTextNoProcesses = @{
                Install = 'インストールする'
                Repair = '修理'
                Uninstall = 'アンインストール'
            }
            Subtitle = @{
                Install = 'PSAppDeployToolkit - アプリのインストール'
                Repair = 'PSAppDeployToolkit - アプリケーションの修復'
                Uninstall = 'PSAppDeployToolkit - アプリケーションのアンインストール'
            }
        }
        CustomMessage = ''
    }
}
