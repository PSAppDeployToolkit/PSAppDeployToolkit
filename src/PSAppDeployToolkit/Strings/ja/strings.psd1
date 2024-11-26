@{
    BalloonText = @{
        Complete = "完了です"
        Error = "失敗。"
        FastRetry = "未完了。"
        RestartRequired = "完了。再起動が必要です。"
        Start = "開始"
    }
    BlockExecution = @{
        Message = "アプリケーションインストールが完了するまで、このアプリケーションの起動を一時的にブロックしています。"
    }
    ClosePrompt = @{
        ButtonClose = "プログラムを強制終了"
        ButtonContinue = "続行"
        ButtonContinueTooltip = "上記に記載されているアプリケーションを終了してから「続ける」を選択してください。"
        ButtonDefer = "後で"
        CountdownMessage = "注意: これらのプログラムは自動的に閉じられます:"
        Message = "インストールを実行するために、下記のプログラムを閉じる必要があります。`n`n実行中のアプリケーションを保存し、閉じてから続行してください。 または、実行中のアプリケーションを保存し、プログラムを強制終了ボタンをクリックしてくだい"
    }
    DeferPrompt = @{
        Deadline = "デッドライン:"
        ExpiryMessage = "再試行可能回数が0になるまでは、都合の良い時にインストール可能です。"
        RemainingDeferrals = "再試行可能回数:"
        WarningMessage = "再試行可能回数が0になった場合、システムで強制インストールをします。"
        WelcomeMessage = "このアプリケーションはこれからインストールされます。"
    }
    DeploymentType = @{
        Install = "インストール"
        Repair = "修復"
        Uninstall = "アンインストール"
    }
    DiskSpace = @{
        Message = "ディスクの空き容量が不足しているため、インストールを完了できません：`n{0}`n`n必要な容量: {1}MB`n現在の空き容量: {2}MB`n`nインストールを実行するために、容量を確保してください"
    }
    Progress = @{
        MessageInstall = "インストール中です。 少々お待ちください。"
        MessageInstallDetail = "インストールが完了すると、このウィンドウは自動的に閉じます。"
        MessageRepair = "修復中です。 少々お待ちください。"
        MessageRepairDetail = "修復が完了すると、このウィンドウは自動的に閉じます。"
        MessageUninstall = "アンインストール中です。 少々お待ちください。"
        MessageUninstallDetail = "アンインストールが完了すると、このウィンドウは自動的に閉じます。"
    }
    RestartPrompt = @{
        ButtonRestartLater = "最小 化"
        ButtonRestartNow = "今すぐ再起動"
        Message = "インストールを完了するために、再起動が必要です。"
        MessageRestart = "カウントダウン後にコンピュータが再起動します。"
        MessageTime = "実行中のアプリケーションを保存し、再起動してください。"
        TimeRemaining = "残時間："
        Title = "再起動が必要です"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0} は自動的に続きます:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - アプリ {0}'
            DialogMessage = '次のアプリケーションは自動的に終了しますので、作業を続ける前に保存してください。'
            DialogMessageNoProcesses = 'インストールを選択してインストールを続行してください。延期分が残っている場合は、インストールを延期することもできます。'
            ButtonDeferRemaining = '残る'
            ButtonLeftText = '延期'
            ButtonRightText = 'アプリを閉じる＆インストール'
            ButtonRightTextNoProcesses = 'インストール'
        }
    }
}
