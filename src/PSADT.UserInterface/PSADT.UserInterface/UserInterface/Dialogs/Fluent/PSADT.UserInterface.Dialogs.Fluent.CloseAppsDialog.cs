using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Threading;
using PSADT.UserInterface.Services;
using Wpf.Ui.Controls;

namespace PSADT.UserInterface.Dialogs.Fluent
{
    /// <summary>
    /// Unified dialog for PSAppDeployToolkit that consolidates all dialog types into one.
    /// </summary>
    public partial class FluentDialog : FluentWindow, IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes the UI elements and behavior for the CloseApps dialog type.
        /// </summary>
        /// <param name="appTitle">Main title of the dialog window.</param>
        /// <param name="subtitle">Subtitle displayed below the main title.</param>
        /// <param name="appIconImage">Path to the icon image file.</param>
        /// <param name="appsToClose">List of applications the user needs to close.</param>
        /// <param name="countdownDuration">Optional duration for a countdown timer before automatic action.</param>
        /// <param name="deferralsRemaining">Optional number of deferrals allowed.</param>
        /// <param name="deferralDeadline">Optional deadline until which deferral is allowed.</param>
        /// <param name="closeAppsMessageText">Message displayed when apps need closing.</param>
        /// <param name="alternativeCloseAppsMessageText">Message displayed when no apps need closing.</param>
        /// <param name="customMessageText">Message displayed underneath the primary dialog message.</param>
        /// <param name="deferralsRemainingText">Text displayed next to the deferral count.</param>
        /// <param name="deferralDeadlineText">Text displayed next to the deferral deadline.</param>
        /// <param name="automaticStartCountdownText">Heading text for the countdown timer.</param>
        /// <param name="deferButtonText">Text for the defer button.</param>
        /// <param name="continueButtonText">Text for the continue/close apps button.</param>
        /// <param name="alternativeContinueButtonText">Text for the continue button when no apps need closing.</param>
        /// <param name="processEvaluationService">Optional service for dynamic process evaluation.</param>
        public void InitializeCloseAppsDialog(
            string? appTitle,
            string? subtitle,
            string? appIconImage,
            List<AppProcessInfo>? appsToClose,
            TimeSpan? countdownDuration,
            int? deferralsRemaining,
            DateTime? deferralDeadline,
            string? closeAppsMessageText,
            string? alternativeCloseAppsMessageText,
            string? customMessageText,
            string? deferralsRemainingText,
            string? deferralDeadlineText,
            string? automaticStartCountdownText,
            string? deferButtonText,
            string? continueButtonText,
            string? alternativeContinueButtonText,
            IProcessEvaluationService? processEvaluationService = null)
        {
            // Set basic properties
            Title = appTitle ?? "Close Applications";
            AppTitleTextBlock.Text = appTitle;
            SubtitleTextBlock.Text = subtitle;

            // Set accessibility properties
            AutomationProperties.SetName(this, appTitle ?? "Close Applications Dialog");

            // Set up Close Apps properties
            _countdownDuration = countdownDuration;
            _appsToClose = appsToClose != null ? new List<AppProcessInfo>(appsToClose) : null; // Create a deep copy to avoid reference issues
            _processEvaluationService = processEvaluationService;
            _deferralsRemaining = deferralsRemaining;
            _deferralDeadline = deferralDeadline;


            // Store original and alternative texts
            _originalMessage = closeAppsMessageText ?? "Please close the following applications:";
            _alternativeMessage = alternativeCloseAppsMessageText ?? "Please continue with the installation.";
            _buttonLeftOriginalText = deferButtonText ?? "Defer";
            _buttonRightOriginalText = continueButtonText ?? "Close Apps & Install";
            _buttonRightAlternativeText = alternativeContinueButtonText ?? "Install";

            // Set up UI
            FormatMessageWithHyperlinks(MessageTextBlock, closeAppsMessageText ?? _originalMessage); // Use helper method
            FormatMessageWithHyperlinks(CustomMessageTextBlock, customMessageText ?? string.Empty); // Use empty string if customMessageText is null
            CustomMessageTextBlock.Visibility = string.IsNullOrEmpty(customMessageText) ? Visibility.Collapsed : Visibility.Visible; // Show or hide the custom message based on its content
            CloseAppsStackPanel.Visibility = Visibility.Visible;
            ProgressStackPanel.Visibility = Visibility.Collapsed;
            InputBoxStackPanel.Visibility = Visibility.Collapsed; // Ensure hidden by default
            DeferStackPanel.Visibility = deferralsRemaining.HasValue || deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            DeferralDeadlineHeadingTextBlock.Text = !deferralDeadline.HasValue ? deferralsRemainingText : deferralDeadlineText;

            CountdownStackPanel.Visibility = countdownDuration.HasValue ? Visibility.Visible : Visibility.Collapsed;
            CountdownHeadingTextBlock.Text = automaticStartCountdownText;

            // Configure buttons
            ButtonPanel.Visibility = Visibility.Visible;
            SetButtonContentWithAccelerator(ButtonLeft, _buttonLeftOriginalText);
            ButtonLeft.Visibility = deferralsRemaining.HasValue || deferralDeadline.HasValue ? Visibility.Visible : Visibility.Collapsed;
            ButtonMiddle.Visibility = Visibility.Collapsed;
            SetButtonContentWithAccelerator(ButtonRight, _buttonRightOriginalText);
            ButtonRight.Visibility = Visibility.Visible;

            // Set button automation properties
            AutomationProperties.SetName(ButtonLeft, _buttonLeftOriginalText);
            AutomationProperties.SetName(ButtonRight, _buttonRightOriginalText);

            UpdateDeferralValues();
            UpdateButtonLayout();

            // Set app icon
            SetAppIcon(appIconImage);

            // Initialize countdown if specified
            if (countdownDuration.HasValue)
            {
                InitializeCountdown(countdownDuration.Value);
            }

            // Attach to window events specific to this dialog type
            if (_processEvaluationService != null && appsToClose != null && appsToClose.Count > 0)
            {
                _processEvaluationService.ProcessStarted += ProcessEvaluationService_ProcessStarted;
                _processEvaluationService.ProcessExited += ProcessEvaluationService_ProcessExited;

                // Start monitoring processes
                UpdateAppsToCloseList();
                _processCancellationTokenSource = new CancellationTokenSource();
                _ = StartProcessEvaluationLoopAsync(_appsToClose!, _processCancellationTokenSource.Token);
            }
            else
            {
                // No apps to close
                // CloseAppsStackPanel.Visibility = Visibility.Collapsed;
                FormatMessageWithHyperlinks(MessageTextBlock, _alternativeMessage); // Use helper method
                SetButtonContentWithAccelerator(ButtonRight, _buttonRightAlternativeText);
                AutomationProperties.SetName(ButtonRight, "Install");
            }

            // Focus the continue button by default
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                ButtonRight.Focus();
            });
        }

        /// <summary>
        /// Updates the list of applications to close based on the current state of the process evaluation service.
        /// </summary>
        private void UpdateAppsToCloseList()
        {
            if (_appsToClose == null || _appsToClose.Count == 0)
            {
                // Only set to collapsed if initially there are no apps to close
                CloseAppsStackPanel.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                // Ensure the list is visible when we have apps to close
                CloseAppsStackPanel.Visibility = Visibility.Visible;
            }

            // Rest of the method remains unchanged
            if (_processEvaluationService == null)
            {
                // Populate the collection directly
                foreach (AppProcessInfo app in _appsToClose)
                {
                    if (AppsToCloseCollection.FirstOrDefault(a => a.ProcessName.Equals(app.ProcessName, StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        AppsToCloseCollection.Add(app);
                    }
                }
                return;
            }

            // Evaluate running processes and populate the collection
            var updatedAppsToClose = _processEvaluationService.EvaluateRunningProcessesAsync(_appsToClose, CancellationToken.None).GetAwaiter().GetResult();

            // Clear existing items
            AppsToCloseCollection.Clear();

            // Add updated apps
            foreach (var app in updatedAppsToClose)
            {
                if (AppsToCloseCollection.FirstOrDefault(a => a.ProcessName.Equals(app.ProcessName, StringComparison.OrdinalIgnoreCase)) == null)
                {
                    AppsToCloseCollection.Add(app);
                }
            }

            _previousProcessInfo = new List<AppProcessInfo>(updatedAppsToClose);
        }

        /// <summary>
        /// Updates the deferral values displayed in the dialog.
        /// </summary>
        private void UpdateDeferralValues()
        {
            // First handle default case - if no deferral settings, just disable the button
            if (!_deferralsRemaining.HasValue && !_deferralDeadline.HasValue)
            {
                ButtonLeft.IsEnabled = false;
                return;
            }

            // Handle deferrals remaining counter
            if (_deferralsRemaining.HasValue)
            {
                UpdateDeferralsRemainingUI();
            }
            // Handle deferral deadline
            else if (_deferralDeadline.HasValue)
            {
                UpdateDeferralDeadlineUI();
            }
        }

        /// <summary>
        /// Updates the UI to reflect the number of deferrals remaining.
        /// </summary>
        private void UpdateDeferralsRemainingUI()
        {
            // Only enable the button if there are deferrals remaining
            ButtonLeft.IsEnabled = _deferralsRemaining > 0;

            // Update text value
            var displayText = $"{_deferralsRemaining} remain";
            DeferralDeadlineValueTextBlock.Text = displayText;

            // Update accessibility properties
            AutomationProperties.SetName(DeferralDeadlineValueTextBlock, displayText);

            // Update text color based on remaining deferrals
            Brush textBrush;
            if (_deferralsRemaining == 0)
            {
                textBrush = Application.Current.Resources["SystemFillColorCriticalBrush"] as Brush;
            }
            else if (_deferralsRemaining <= 1)
            {
                textBrush = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
            }
            else
            {
                textBrush = Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
            }
            DeferralDeadlineValueTextBlock.Foreground = textBrush;
        }

        /// <summary>
        /// Updates the UI to reflect the deferral deadline.
        /// </summary>
        private void UpdateDeferralDeadlineUI()
        {
            // Calculate time remaining until deadline
            TimeSpan timeRemaining = _deferralDeadline.Value - DateTime.Now;
            bool isExpired = timeRemaining <= TimeSpan.Zero;

            // Set button state based on deadline
            ButtonLeft.IsEnabled = !isExpired;

            // Update text content
            string displayText;
            Brush textBrush;

            if (isExpired)
            {
                displayText = "Expired";
                textBrush = Application.Current.Resources["SystemFillColorCriticalBrush"] as Brush;
            }
            else
            {
                displayText = _deferralDeadline.Value.ToString("r");

                if (timeRemaining < TimeSpan.FromDays(1))
                {
                    // Less than 1 day remaining - use caution color
                    textBrush = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
                }
                else
                {
                    textBrush = Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
                }
            }

            DeferralDeadlineValueTextBlock.Text = displayText;
            DeferralDeadlineValueTextBlock.Foreground = textBrush;

            // Update accessibility properties
            AutomationProperties.SetName(DeferralDeadlineValueTextBlock, displayText);
        }

        /// <summary>
        /// Starts the process evaluation loop asynchronously.
        /// </summary>
        /// <param name="initialApps"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task StartProcessEvaluationLoopAsync(List<AppProcessInfo> initialApps, CancellationToken token)
        {
            var stopwatch = new Stopwatch();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // Wait based on adaptive delay
                        await Task.Delay(_processEvaluationDelay, token);

                        // Skip if we're in the process of closing the dialog
                        if (_isProcessing || _isDisposed)
                            break;

                        stopwatch.Restart();

                        // Acquire lock for thread safety
                        await _processEvaluationLock.WaitAsync(token);

                        try
                        {
                            // Asynchronously evaluate running processes
                            List<AppProcessInfo> updatedApps = await _processEvaluationService!.EvaluateRunningProcessesAsync(initialApps, token).ConfigureAwait(false);

                            // Check if there's any change compared to the previous list
                            if (!AreProcessListsEqual(_previousProcessInfo, updatedApps))
                            {
                                // Update the collection on the UI thread
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    if (_isDisposed) return;

                                    AppsToCloseCollection.Clear();
                                    foreach (var app in updatedApps)
                                    {
                                        if (AppsToCloseCollection.FirstOrDefault(a => a.ProcessName.Equals(app.ProcessName, StringComparison.OrdinalIgnoreCase)) == null)
                                        {
                                            AppsToCloseCollection.Add(app);
                                        }
                                    }
                                }, DispatcherPriority.Background);

                                // Update the previous process info for the next comparison
                                _previousProcessInfo = new List<AppProcessInfo>(updatedApps);
                            }

                            // If no more apps to close, exit the loop
                            if (updatedApps.Count == 0)
                            {
                                break;
                            }
                        }
                        finally
                        {
                            // Release the lock
                            _processEvaluationLock.Release();
                        }

                        stopwatch.Stop();

                        // Adjust delay based on evaluation time
                        if (stopwatch.ElapsedMilliseconds > 500)
                        {
                            // Evaluation is slow, increase delay
                            _processEvaluationDelay = TimeSpan.FromSeconds(
                                Math.Min(_processEvaluationDelay.TotalSeconds * 1.5, MAX_DELAY_SECONDS));
                        }
                        else if (stopwatch.ElapsedMilliseconds < 100)
                        {
                            // Evaluation is fast, decrease delay slightly
                            _processEvaluationDelay = TimeSpan.FromSeconds(
                                Math.Max(_processEvaluationDelay.TotalSeconds * 0.9, MIN_DELAY_SECONDS));
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Rethrow to be caught by outer handler
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during process evaluation iteration: {ex.Message}");

                        // Continue the loop unless we're being canceled
                        if (token.IsCancellationRequested)
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Task was canceled, no action needed
                Debug.WriteLine("Process evaluation loop was canceled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in process evaluation loop: {ex.Message}");
                // Consider logging to a more permanent store
            }
        }

        /// <summary>
        /// Compares two lists of AppProcessInfo objects to check if they are equal.
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        private static bool AreProcessListsEqual(List<AppProcessInfo> list1, List<AppProcessInfo> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            // Order the lists to ensure consistent comparison
            var sortedList1 = list1.OrderBy(app => app.ProcessName).ToList();
            var sortedList2 = list2.OrderBy(app => app.ProcessName).ToList();

            for (int i = 0; i < sortedList1.Count; i++)
            {
                if (!string.Equals(sortedList1[i].ProcessName, sortedList2[i].ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Handles the collection changed event for the AppsToCloseCollection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppsToCloseCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (DialogType != DialogType.CloseApps)
                return;

            try
            {
                // Update row definitions
                UpdateRowDefinition();

                // Update accessibility count
                AutomationProperties.SetName(CloseAppsListView, $"Applications to Close: {AppsToCloseCollection.Count} items");

                if (AppsToCloseCollection.Count == 0 && _alternativeMessage != null)
                {
                    // Update the message and button content with alternative texts
                    FormatMessageWithHyperlinks(MessageTextBlock, _alternativeMessage); // Use helper method
                    SetButtonContentWithAccelerator(ButtonRight, _buttonRightAlternativeText);
                    AutomationProperties.SetName(ButtonRight, _buttonRightAlternativeText ?? "Install");

                    // Hide the entire apps to close panel when there are no apps
                    // CloseAppsStackPanel.Visibility = Visibility.Collapsed;
                }
                else if (_originalMessage != null)
                {
                    // Revert to original texts
                    FormatMessageWithHyperlinks(MessageTextBlock, _originalMessage); // Use helper method
                    SetButtonContentWithAccelerator(ButtonRight, _buttonRightOriginalText);
                    AutomationProperties.SetName(ButtonRight, _buttonRightOriginalText ?? "Close Apps & Install");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AppsToCloseCollection_CollectionChanged: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the process started event from the process evaluation service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessEvaluationService_ProcessStarted(object? sender, AppProcessInfo e)
        {
            if (e == null || DialogType != DialogType.CloseApps || _isDisposed)
                return;

            try
            {
                // Check if the process is already in the collection to avoid duplicates
                Dispatcher.Invoke(() =>
                {
                    if (_isDisposed) return;

                    if (!AppsToCloseCollection.Contains(e))
                    {
                        var existingApp = AppsToCloseCollection.FirstOrDefault(a =>
                            a.ProcessName.Equals(e.ProcessName, StringComparison.OrdinalIgnoreCase));

                        if (existingApp == null)
                        {
                            AppsToCloseCollection.Add(e);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessEvaluationService_ProcessStarted: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the process exited event from the process evaluation service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessEvaluationService_ProcessExited(object? sender, AppProcessInfo e)
        {
            if (e == null || DialogType != DialogType.CloseApps || _isDisposed)
                return;

            try
            {
                // Add to recently removed cache to prevent flickering
                lock (_recentlyRemovedProcesses)
                {
                    _recentlyRemovedProcesses[e.ProcessName] = DateTime.Now;
                }

                Dispatcher.Invoke(() =>
                {
                    if (_isDisposed) return;

                    var processToRemove = AppsToCloseCollection.FirstOrDefault(a =>
                        a.ProcessName.Equals(e.ProcessName, StringComparison.OrdinalIgnoreCase));

                    if (processToRemove != null)
                    {
                        // Animation logic removed - handled by UnifiedAdtApplication.RemoveAppToClose
                        AppsToCloseCollection.Remove(processToRemove);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessEvaluationService_ProcessExited: {ex.Message}");
            }
        }
    }
}
