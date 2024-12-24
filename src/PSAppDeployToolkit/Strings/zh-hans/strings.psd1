﻿@{
    BalloonText = @{
        Complete = @{
            Install = '安装完成。'
            Repair = '修复完成。'
            Uninstall = '卸载完成。'
        }
        Error = @{
            Install = '安装失败。'
            Repair = '修复失败。'
            Uninstall = '卸载失败。'
        }
        FastRetry = @{
            Install = '安装未完成。'
            Repair = '修复未完成。'
            Uninstall = '卸载未完成。'
        }
        RestartRequired = @{
            Install = '安装完成。需要重启。'
            Repair = '修复完成。需要重启。'
            Uninstall = '卸载完成。需要重启。'
        }
        Start = @{
            Install = '安装开始。'
            Repair = '修复开始。'
            Uninstall = '卸载开始。'
        }
    }
    BlockExecution = @{
        Message = @{
            Install = '启动此应用程序已被临时阻止，以便完成安装操作。'
            Repair = '启动此应用程序已被暂时阻止，以便完成修复操作。'
            Uninstall = '启动此应用程序已被暂时阻止，以便完成卸载操作。'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - 应用程序安装'
            Repair = 'PSAppDeployToolkit - 应用程序修复'
            Uninstall = 'PSAppDeployToolkit - 应用程序卸载'
        }
    }
    DiskSpace = @{
        Message = @{
            Install = "您没有足够的磁盘空间来完成以下安装：`n{0}`n`n所需空间：{1}MB`n可用空间：{2}MB`n`n请释放足够的磁盘空间，以便继续安装。"
            Repair = "您没有足够的磁盘空间来修复：`n{0}`n`n所需空间：{1}MB`n可用空间：{2}MB`n`n请释放足够的磁盘空间以继续修复。"
            Uninstall = "您没有足够的磁盘空间来完成卸载：`n{0}`n`n所需空间：{1}MB`n可用空间：{2}MB`n`n请释放足够的磁盘空间，以便继续卸载。"
        }
    }
    Progress = @{
        Message = @{
            Install = '正在安装。请稍候......'
            Repair = '修复中。请稍候...'
            Uninstall = '卸载中。请稍候...'
        }
        MessageDetail = @{
            Install = '安装完成后，此窗口将自动关闭。'
            Repair = '修复完成后，此窗口将自动关闭。'
            Uninstall = '卸载完成后，此窗口将自动关闭。'
        }
        Subtitle = @{
            Install = 'PSAppDeployToolkit - 应用程序安装'
            Repair = 'PSAppDeployToolkit - 应用程序修复'
            Uninstall = 'PSAppDeployToolkit - 应用程序卸载'
        }
    }
    Prompt = @{
        Subtitle = @{
            Install = 'PSAppDeployToolkit - 应用程序安装'
            Repair = 'PSAppDeployToolkit - 应用程序修复'
            Uninstall = 'PSAppDeployToolkit - 应用程序卸载'
        }
    }
    RestartPrompt = @{
        ButtonRestartLater = '最小化'
        ButtonRestartNow = '立即重启'
        Message = @{
            Install = '为了完成安装，您必须重启计算机。'
            Repair = '为了完成修复，您必须重新启动计算机。'
            Uninstall = '为了完成卸载，您必须重新启动计算机。'
        }
        MessageRestart = '倒计时结束后，您的计算机将自动重启。'
        MessageTime = '请保存您的工作并在指定时间内重新启动。'
        TimeRemaining = '剩余时间：'
        Title = '需要重新启动'
        Subtitle = @{
            Install = 'PSAppDeployToolkit - 应用程序安装'
            Repair = 'PSAppDeployToolkit - 应用程序修复'
            Uninstall = 'PSAppDeployToolkit - 应用程序卸载'
        }
    }
    WelcomePrompt = @{
        Classic = @{
            Close = @{
                ButtonClose = '关闭 &程序'
                ButtonContinue = '&继续'
                ButtonContinueTooltip = '关闭以上列出的应用程序后，仅选择“继续”。'
                ButtonDefer = '&推迟'
                CountdownMessage = '注意：程序将在以下时间自动关闭：'
                Message = @{
                    Install = "在继续安装前必须关闭以下程序。`n`n请保存您的工作，关闭程序，然后继续。或者，保存您的工作并点击 `“关闭程序`”。"
                    Repair = "在继续修复前必须关闭以下程序。`n`n请保存您的工作，关闭程序，然后继续。或者，保存您的工作并点击 `“关闭程序`”。"
                    Uninstall = "在卸载前必须关闭以下程序。`n`n请保存您的工作，关闭程序，然后继续。或者，保存您的工作并点击 `“关闭程序`”。"
                }
            }
            Defer = @{
                Deadline = '截止日期：'
                ExpiryMessage = @{
                    Install = '您可以选择推迟安装，直到过期：'
                    Repair = '您可以选择推迟修复，直到延期过期：'
                    Uninstall = '您可以选择推迟卸载，直到延期过期：'
                }
                RemainingDeferrals = '剩余延期：'
                WarningMessage = '一旦延期过期，您将不再有推迟选项。'
                WelcomeMessage = @{
                    Install = '以下应用程序即将安装：'
                    Repair = '以下应用程序即将修复：'
                    Uninstall = '以下应用程序即将卸载：'
                }
            }
            CountdownMessage = @{
                Install = '安装将在以下时间自动继续：'
                Repair = '修复将在以下时间自动继续：'
                Uninstall = '卸载将在以下时间自动继续：'
            }
            CustomMessage = ''
        }
        Fluent = @{
            Subtitle = @{
                Install = 'PSAppDeployToolkit - 应用程序安装'
                Repair = 'PSAppDeployToolkit - 应用程序修复'
                Uninstall = 'PSAppDeployToolkit - 应用程序卸载'
            }
            DialogMessage = '请保存您的工作，然后继续，因为以下应用程序将自动关闭。'
            DialogMessageNoProcesses = @{
                Install = '请选择安装以继续安装。如果您还有任何延迟，也可以选择延迟安装。'
                Repair = '请选择修复以继续修复。如果您还有任何延迟，也可以选择延迟修复。'
                Uninstall = '请选择卸载以继续卸载。如果您还有任何延迟，也可以选择延迟卸载。'
            }
            ButtonDeferRemaining = '剩余'
            ButtonLeftText = '延迟'
            ButtonRightText = @{
                Install = '关闭应用程序并安装'
                Repair = '关闭应用程序并修复'
                Uninstall = '关闭应用程序并卸载'
            }
            ButtonRightTextNoProcesses = @{
                Install = '安装'
                Repair = '修复'
                Uninstall = '卸载'
            }
        }
    }
}
