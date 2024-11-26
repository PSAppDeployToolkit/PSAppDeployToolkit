@{
    BalloonText = @{
        Complete = "完成。"
        Error = "失败。"
        FastRetry = "未完成。"
        RestartRequired = "完成。必须重新启动。"
        Start = "已启动。"
    }
    BlockExecution = @{
        Message = "为完成安装过程，暂时禁止启动这款应用程式。"
    }
    ClosePrompt = @{
        ButtonClose = "关闭程序"
        ButtonContinue = "继续"
        ButtonContinueTooltip = "在关闭上列应用程式后才选择`"继续`"。"
        ButtonDefer = "延迟"
        CountdownMessage = "注：下列程序将自动关闭："
        Message = "为继续安装，必须关闭下列程序。`n`n请保存您的工作，关闭程序，然后继续。 或者保存您的工作，点击`"关闭程序`"。"
    }
    DeferPrompt = @{
        Deadline = "最后期限："
        ExpiryMessage = "在延期失效前，可选择延迟安装："
        RemainingDeferrals = "所剩延期："
        WarningMessage = "延期失效后，再也无法延迟安装。"
        WelcomeMessage = "即将安装下列应用程式："
    }
    DeploymentType = @{
        Install = "安装"
        Repair = "修复"
        Uninstall = "卸载"
    }
    DiskSpace = @{
        Message = "没有足够的磁盘空间来完成下列安装：`n{0}`n`n所需空间：{1}MB`n可用空间：{2}MB`n`n请释放足够的磁盘空间以继续安装。"
    }
    Progress = @{
        MessageInstall = "安装中。请稍等。。。"
        MessageInstallDetail = "安装完成后，该窗口将自动关闭。。。"
        MessageRepair = "修复中。请稍等。。。"
        MessageRepairDetail = "修复完成后，该窗口将自动关闭。。。"
        MessageUninstall = "卸载中。请稍等。。。"
        MessageUninstallDetail = "卸载完成后，该窗口将自动关闭。。。"
    }
    RestartPrompt = @{
        ButtonRestartLater = "最小化"
        ButtonRestartNow = "现在重启"
        Message = "为完成安装过程，需重启计算机。"
        MessageRestart = "倒计时结束后，计算机将自动重启。"
        MessageTime = "请保存您的工作，并在容许时间重启计算机。"
        TimeRemaining = "剩余时间："
        Title = "需重启"
    }
    WelcomePrompt = @{
        Classic = @{
            CountdownMessage = "{0}会自动继续:"
            CustomMessage = ""
        }
        Fluent = @{
            Subtitle = 'PSAppDeployToolkit - 应用程序 {0}'
            DialogMessage = '请保存您的工作后再继续，因为以下应用程序将自动关闭。'
            DialogMessageNoProcesses = '请选择 “安装 ”继续安装。如果您还有任何延迟，也可以选择延迟安装。'
            ButtonDeferRemaining = '残留'
            ButtonLeftText = '推迟'
            ButtonRightText = '关闭应用程序并安装'
            ButtonRightTextNoProcesses = '安装'
        }
    }
}
