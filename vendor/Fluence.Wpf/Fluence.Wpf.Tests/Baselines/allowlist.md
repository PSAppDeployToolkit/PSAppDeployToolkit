# Permitted `--list-tests` method-name changes

Diff key: the method name alone, one line per discovered test case. Theory cases
repeat their method name once per `[InlineData]`. All 1155 method names in the
baseline are unique across classes, so no `class.method` disambiguation is used.

Baseline: 1197 cases on net10, 1195 on net472, both captured at
`950e779` and committed beside this file.

Nothing outside this list may appear in a `Compare-Object` result. Every entry
lands in Task 23 and in no other task; every earlier and later task must diff
empty against the baseline, or against the baseline plus this list once Task 23
has landed.

## Removals: 21 lines

Eighteen are deleted test cases; three are the method names retired by the
`DefaultCollectionFocusVisualStyle` theory fold.

| Line | Source | Reason |
| ---- | ------ | ------ |
| `ControlCornerRadius_PresentInLightThemeAsync` | `ThemeMetricsTests.cs:56` | D1 |
| `ControlCornerRadius_PresentInDarkThemeAsync` | `ThemeMetricsTests.cs:68` | D1 |
| `ControlCornerRadius_PresentInHighContrastThemeAsync` | `ThemeMetricsTests.cs:80` | D1 |
| `OverlayCornerRadius_PresentInLightThemeAsync` | `ThemeMetricsTests.cs:96` | D1 |
| `OverlayCornerRadius_PresentInDarkThemeAsync` | `ThemeMetricsTests.cs:108` | D1 |
| `OverlayCornerRadius_PresentInHighContrastThemeAsync` | `ThemeMetricsTests.cs:120` | D1 |
| `DefaultControlFocusVisualStyle_PresentInAllThemesAsync` | `ThemeMetricsTests.cs:169` | D2 |
| `ProgressBar_TrackBackground_UsesWinUiStrongStrokeRoleAsync` | `ControlTests.BackgroundParity.cs:114` | D3 |
| `FiveSwitches_DictionaryCountStableAsync` | `ThemeManagerTests.cs:150` | D4 |
| `MergedDictionaries_CountStableAfterMultipleSwitchesAsync` | `FluenceWindowTitleBarTests.cs:471` | D4 |
| `BuildBackdropPlan_None_ReturnsOpaqueBackground` | `FluenceWindowHardenTests.cs:223` | D5 |
| `BuildBackdropPlan_Mica_SupportedOs_ReturnsTransparent` | `FluenceWindowHardenTests.cs:240` | D6 |
| `MainWindow_ProgressNumberBox_UpdatesFirstProgressBarAsync` | `ControlTests.cs:2056` | D7 |
| `Experiment_WriteDwmAccentColor_DoesAccentPaletteRegenerateAsync` | `AccentPaletteRegenerationExperiment.cs` | D8 |
| `Experiment_WriteAllAccentValues_DoesAccentPaletteRegenerateAsync` | `AccentPaletteRegenerationExperiment.cs` | D8 |
| `Experiment_WriteAllAndBroadcast_DoesAccentPaletteRegenerateAsync` | `AccentPaletteRegenerationExperiment.cs` | D8 |
| `Probe_EnumerateColorSets_DumpsAllRamps` | `ImmersiveColorSetProbe.cs:73` | D8 |
| `Score_AllAlgorithms_AgainstCapturedFixtures` | `AccentRampScoreboard.cs:153` | D9 |
| `DefaultCollectionFocusVisualStyle_PresentInLightThemeAsync` | `ThemeMetricsTests.cs:210` | Fold 1 |
| `DefaultCollectionFocusVisualStyle_PresentInDarkThemeAsync` | `ThemeMetricsTests.cs:221` | Fold 1 |
| `DefaultCollectionFocusVisualStyle_PresentInHighContrastThemeAsync` | `ThemeMetricsTests.cs:232` | Fold 1 |

## Additions: 4 lines

| Line | Occurrences | Reason |
| ---- | ----------: | ------ |
| `DefaultCollectionFocusVisualStyle_PresentInThemeAsync` | 3 | Fold 1: one `[Theory]` with three `[InlineData]`. |
| `RepeatedThemeSwitches_NoDictionaryAccumulationAsync` | 1 extra, taking it from 1 line to 2 | Fold 2: the D4 survivor becomes a `[Theory]` with `[InlineData(false)]` and `[InlineData(true)]`. |

Net: 21 removed, 4 added, 17 fewer cases. 1197 to 1180 on net10, 1195 to 1178 on net472.

## Status

Consumed. The baseline files beside this one were refreshed to the post-deletion
state in the same commit, so every task after this one diffs empty against them.

## Task 38: final branch-close regeneration

Baseline files were regenerated once, at the end of the branch, per Ruling R4.
This is the only other task allowed to touch this folder. No commit before
this one touched this folder, so the regenerated capture reflects every case
added anywhere on the branch since the Task 23 point above: 1208 cases on
net10 (was 1180), 1206 on net472 (was 1178), a rise of 28 on each TFM.

### Renames: 1 line

| Old name | New name | Commit | Reason |
| ---- | ---- | ---- | ------ |
| `ApplyApplicationAccent_RaisesAccentColorChangedOnceAsync` | `ApplyCustomAccent_WindowsBlue_RaisesAccentColorChangedOnceAsync` | `9451f89` | `ApplyApplicationAccent` was removed as a one-line alias for a Windows blue `ApplyCustomAccent` call, so the test that exercised it now calls `ApplyCustomAccent` directly with the Windows blue color and is named to match. |

### Additions: 28 lines, none colliding with an existing name

No other method name changed; every other new line is a genuinely new test case
with a name that never appeared in the Task 23 baseline, so none needed an
allowlist entry of its own. For the record, grouped by source file:

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `Opened_WhenShown_RaisesWithOpenedEventArgsAsync` | 1 | `ContentDialogTests.cs` |
| `Closed_EachClosePath_ReportsMatchingResultAsync` | 4 | `ContentDialogTests.cs` |
| `Closed_CloseButtonClicked_ReportsCloseButtonReasonAsync` | 1 | `InfoBarTests.cs` |
| `Closed_IsOpenSetFalse_ReportsProgrammaticReasonOnceAsync` | 1 | `InfoBarTests.cs` |
| `Closing_CloseButtonClicked_ReportsCloseButtonReasonAsync` | 1 | `InfoBarTests.cs` |
| `Closed_CloseButtonClicked_ReportsCloseButtonReasonAsync` | 1 | `TeachingTipTests.cs` |
| `Closed_EscapeKeyDismissal_ReportsLightDismissReasonAsync` | 1 | `TeachingTipTests.cs` |
| `Closed_IsOpenSetFalse_ReportsProgrammaticReasonAsync` | 1 | `TeachingTipTests.cs` |
| `Closed_PopupClosedOutsideIsOpen_ReportsLightDismissReasonAsync` | 1 | `TeachingTipTests.cs` |
| `CloseRequested_TypedHandler_ReceivesArgsWithoutCastAsync` | 1 | `TabViewTests.cs` |
| `TabCloseRequested_StillBubblesToAParentHandlerAsync` | 1 | `TabViewTests.cs` |
| `RemovedKeys_DoNotResolveInAnyThemeAsync` | 3 | `ResourceAliasTests.cs` |
| `BackgroundBrushAlias_ResolvesToTheSameBrushAsync` | 3 | `ResourceAliasTests.cs` |
| `FontFamilyAlias_ResolvesToTheSameFamilyAsync` | 1 | `ResourceAliasTests.cs` |
| `TitleBar_AutomationPeer_PrefersExplicitAutomationNameAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `TitleBar_AutomationPeer_ReportsTitleBarControlTypeAndTitleAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `InfoBadge_AutomationPeer_ReportsValueAsNameAsync` | 3 | `Control/Rules/AutomationPeerTests.cs` |
| `FlyoutPresenter_AutomationPeer_ReportsGroupControlTypeAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `PublicKeyInventory_MatchesFrozenSetAsync` | 1 | `ThemeParityTests.cs` |

Note the task-38 dispatch's own case tally (27 new cases: 1 ContentDialog.Opened,
4 ContentDialog.Closed, 2 InfoBar.Closed, 3 TeachingTip.Closed, 2 TabView, 3
RemovedKeys, 4 aliases, 2 TitleBar peer, 3 InfoBadge peer, 1 FlyoutPresenter
peer, 1 InfoBar.Closing reason, 1 PublicKeyInventory) undercounts
`TeachingTipTests.cs` by one: that file added four new `Closed_`-prefixed facts,
not three, which is confirmed above by both the `Compare-Object` multiset diff
and direct inspection of the file. The measured total is 28 new cases, not 27;
this table is the corrected record.

Zero pure deletions on this stretch of the branch: the only line that left the
multiset is the rename source above.

### Status

Consumed. This is the branch's last baseline regeneration; there is no further
task after this one that touches `Baselines/`.

## Branch `fix/gallery-visual-defects`: NavigationView pane chrome regeneration

The baseline files were regenerated again on this branch, for the two regression
tests the pane chrome fixes brought with them. Both are new names that never
appeared in an earlier capture, so neither needed an entry of its own; this
section is the record of the regeneration.

### Additions: 23 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `NavigationView_NestedInContent_KeepsItsOwnPaneChromeAsync` | 1 | `Control/NavigationViewTests.cs` |
| `NavigationView_CompactRailWidth_DoesNotFollowTheBackButtonAsync` | 1 | `Control/NavigationViewTests.cs` |
| `NavigationViewItem_InfoBadge_StaysOnAClosedPaneAsync` | 1 | `Control/NavigationViewTests.cs` |
| `SlideNavigationPresenter_OutgoingContent_KeepsTheContentTemplateAsync` | 1 | `Control/SlideNavigationPresenterTests.cs` |
| `SelectorBar_EmptiedSelection_PutsThePreviousItemBackAsync` | 1 | `Control/SelectorBarTests.cs` |
| `InfoBar_CloseButton_RaisesClickAndRunsTheCommandBeforeClosingAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBar_CloseButtonStyle_ReachesTheButtonAndRestoresOnClearAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBar_Content_RendersUnderTheBannerAndTakesItWhenThereIsNoneAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBadge_DisplayKind_PrefersValueOverIconAndFallsBackAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `InfoBar_Banner_LaysOutOnOneLineUntilItStopsFittingAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBadge_ValueBadge_IsNeverNarrowerThanItIsTallAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `NavigationViewItem_ClosedPane_GivesTheIconItsFullColumnAsync` | 2 | `Control/NavigationViewTests.cs`, one `[Theory]` with `[InlineData]` for Left and LeftCompact |
| `InfoBar_Opened_RaisesOnTheOpenTransitionButNotOnACancelledCloseRevertAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBar_CustomIcon_IsClampedToTheIconBoxAsync` | 1 | `Control/InfoBarTests.cs` |
| `InfoBadge_Value_RejectsAnythingBelowMinusOneAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `InfoBadge_GetStyleGlyph_ReturnsWinUiGlyphPerSeverity` | 1 | `Control/InfoBadgeTests.cs` |
| `InfoBadge_SeverityWithAValue_ShowsTheValueNotAGlyphAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `TabViewItem_LeadingSeparator_SitsInTheMiddleOfTheGapAsync` | 1 | `Control/TabViewTests.cs` |
| `ComboBoxItem_CornerRadius_ComesFromTheKeyedResourceAsync` | 1 | `Control/ComboBoxTests.cs` |
| `InfoBadge_ValueText_FitsThePillAndIsCentredAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `MainWindow_TitleBarSearch_IsNotClippedAsync` | 1 | `Gallery/DemoShellTests.cs` |
| `Slider_ThumbScale_TakesTheDurationOfTheStateItEntersAsync` | 1 | `Control/SliderTests.cs` |
| `NavigationView_LeftMode_IndicatorTravelsWithoutFadingOutAsync` | 1 | `Control/NavigationViewTests.cs` |

### Removals: 2 lines

Both pinned the slide-out and slide-in the selection indicator used to play,
which the WinUI stretch-and-settle replaces. The new case above covers the
contract that replaced them: the bar holds full opacity across the move and
stretches past its rest length mid-flight.

| Line | Source | Reason |
| ---- | ------ | ------ |
| `NavigationView_LeftMode_IndicatorExitsVerticallyBeforeChangingParentChildIndentAsync` | `Control/NavigationViewTests.cs` | Asserted the departing leg of a mechanism that no longer exists |
| `NavigationView_LeftMode_IndicatorExitsUpwardWhenNewSelectionIsAboveAsync` | `Control/NavigationViewTests.cs` | Same |

Net: 23 added, 2 removed. 1275 cases to 1297 on net10, 1273 to 1295 on
net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: top-mode indicator placement

One more regeneration on the same branch, for the regression test that pins
where the top-mode selection indicator lands now that it is lifted out of the
navigation bar's bottom edge and under the item it marks.

### Additions: 1 line

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `NavigationView_TopMode_Indicator_SitsUnderTheSelectedItemAsync` | 1 | `Control/NavigationViewTests.cs` |

Net: 1 added, none removed. 1297 cases to 1298 on net10, 1295 to 1296 on
net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: teaching tip motion and the title bar search box

Two more regression tests: one pins WinUI's expand and contract on `TeachingTip`,
the other pins the title bar search box to the centre of the bar on both axes,
which is what the unused helper row inside the field used to spoil.

### Additions: 2 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `TeachingTip_ExpandAndContract_ScaleFromTheTailEdgeAsync` | 1 | `Control/TeachingTipTests.cs` |
| `MainWindow_TitleBarSearchBox_CentresOnTheTitleBarAsync` | 1 | `Gallery/DemoShellTests.cs` |

Net: 2 added, none removed. 1298 cases to 1300 on net10, 1296 to 1298 on
net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the declared font size rule

Five more regression tests. One pins the `InfoBadge` numeral centred inside its
pill; the other four are a new cross-control rule class, which pins the size a
control declares actually reaching the text a `ContentPresenter` generates for
it.

### Additions: 5 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `InfoBadge_ValueText_SitsCentredInsideThePillAsync` | 1 | `Control/InfoBadgeTests.cs` |
| `InfoBadge_ValueText_RendersAtTheDeclaredSizeAsync` | 1 | `Control/Rules/DeclaredFontSizeTests.cs` |
| `NavigationViewItemHeader_RendersAtTheDeclaredSizeAsync` | 1 | `Control/Rules/DeclaredFontSizeTests.cs` |
| `TabViewItem_Header_RendersAtTheDeclaredSizeAsync` | 1 | `Control/Rules/DeclaredFontSizeTests.cs` |
| `ToolTip_RendersAtTheDeclaredSizeAsync` | 1 | `Control/Rules/DeclaredFontSizeTests.cs` |

Net: 5 added, none removed. 1300 cases to 1305 on net10, 1298 to 1303 on
net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the two implemented properties and the flyout gesture

Five more regression tests, and the raw captures beside this file lose their
trailing summary line: it carried the absolute path of whoever generated it and
a duration in milliseconds, so every regeneration diffed even on the same
machine in a different worktree. The `.methods.txt` captures are unchanged in
shape.

The net8.0-windows smoke lane added in the same batch is not baselined. It is a
separate assembly with its own four cases, covering that the third shipped
target framework loads at all; a case added there needs no entry here.

### Additions: 5 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `FlyoutBase_ShownFromAClick_SurvivesTheButtonReleasingCaptureAsync` | 1 | `Control/FlyoutTests.cs` |
| `ProgressBar_ShowStepMarkers_NotchesTheBarAtEveryBoundaryAsync` | 1 | `Control/ProgressBarTests.cs` |
| `ProgressBar_ShowStepMarkers_LeavesANonStepBarWholeAsync` | 1 | `Control/ProgressBarTests.cs` |
| `ListView_ViewStateGridView_WrapsItemsAcrossTheListAsync` | 1 | `Control/ListViewTests.cs` |
| `ListView_ViewStateGridView_LeavesAConsumerPanelAloneAsync` | 1 | `Control/ListViewTests.cs` |

Net: 5 added, none removed. 1305 cases to 1310 on net10, 1303 to 1308 on
net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the slider theory and the tree rail

Four more regression cases from three test methods: a theory pinning snap to
tick on both slider orientations, and two pinning where the TreeView selection
rail stands. The theory counts as two cases on net10, where `--list-tests`
expands its `InlineData`, and as one line on net472, where the listing does not;
both frameworks run both cases.

### Additions: 3 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `Slider_SnapToTick_LandsOnATickWhenDraggedAsync` | 2 on net10, 1 line on net472 | `Control/SliderTests.cs` |
| `TreeView_SelectionIndicator_StaysInOneColumnAtEveryDepthAsync` | 1 | `Control/TreeViewTests.cs` |
| `TreeView_MultipleSelection_HidesTheSelectionIndicatorAsync` | 1 | `Control/TreeViewTests.cs` |

Net: 4 added on net10, 3 lines on net472, none removed. 1310 cases to 1314 on
net10, 1308 to 1311 on net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the flyout dismissal and the radio dot

Two regression cases for the two defects fixed in the same batch: one pinning
that a flyout stays on screen through the gesture that opened it and closes on
the next press outside, and one pinning that a radio button's state storyboards
release on exit rather than stamping a dot into an unchecked ring.

### Additions: 2 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `FlyoutBase_LightDismiss_ClosesOnAPressOutsideAsync` | 1 | `Control/FlyoutTests.cs` |
| `RadioButton_StateSizeStoryboards_ReleaseRatherThanStampOnExitAsync` | 1 | `Control/RadioButtonTests.cs` |

Net: 2 added, none removed. 1314 cases to 1316 on net10, 1311 to 1313 on
net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the closed expander corners and the dropdown reveal

One regression case for the ComboBox dropdown reveal, which rested on a hidden
base value, and one rename: the expander header test now covers the closed state
as well as the open one, so the name it had (top corners only) no longer
describes what it asserts.

### Renames: 1 line

| From | To | Source |
| ---- | -- | ------ |
| `Expander_HeaderBorder_CornerRadiusTopOnlyAsync` | `Expander_HeaderBorder_KeepsTheWholeRadiusUntilItOpensAsync` | `Control/ExpanderTests.cs` |

### Additions: 1 line

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `ComboBox_DropdownReveal_RestsAtTheOpenPoseWhileItRunsAsync` | 1 | `Control/ComboBoxTests.cs` |

Net: 1 added, none removed. 1316 cases to 1317 on net10, 1313 to 1314 on net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the expander header background

One case for the property that gives the header tier back to consumers after the
box-model change moved `Background` to the content tier.

### Additions: 1 line

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `Expander_HeaderBackground_PaintsTheHeaderTierOnlyAsync` | 1 | `Control/ExpanderTests.cs` |

Net: 1 added, none removed. 1317 cases to 1318 on net10, 1314 to 1315 on net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the third review pass

Twelve cases from five test methods, all regression guards for the third
review pass on PR #73. Both theories expand their `InlineData` on both
frameworks in this capture.

### Additions: 12 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `FlyoutBase_DismissMessages_AreNonClientPressesAndLosingTheForeground` | 7 | `Control/FlyoutTests.cs` |
| `FlyoutBase_LightDismiss_PressOnTheAnchorClosesWithoutReopeningAsync` | 1 | `Control/FlyoutTests.cs` |
| `FlyoutBase_LightDismiss_ClosesWhenTheOwningWindowMovesAsync` | 1 | `Control/FlyoutTests.cs` |
| `RadioButton_CheckedDot_KeepsItsCheckedSizeWhenAStateStoryboardIsReleasedAsync` | 1 | `Control/RadioButtonTests.cs` |
| `PublicKeyInventory_IsTheSameInEveryThemeAsync` | 2 | `Theming/ThemeParityTests.cs` |

Net: 12 added, none removed. 1318 cases to 1330 on net10, 1315 to 1327 on
net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the reviewer's second-look items

Two renames follow `ListView.ViewState` becoming `ListView.ItemsLayout`; the
golden value check becomes a theory so its high contrast case can skip, by name,
on a desktop whose system colours differ from the ones the snapshot recorded; and
one case pins that `SlideNavigationPresenter.TransitionEffect` rejects the 0 left
for WinUI's `FromBottom`.

### Renames: 2 lines

| From | To | Source |
| ---- | -- | ------ |
| `ListView_ViewStateGridView_WrapsItemsAcrossTheListAsync` | `ListView_ItemsLayoutGrid_WrapsItemsAcrossTheListAsync` | `Control/ListViewTests.cs` |
| `ListView_ViewStateGridView_LeavesAConsumerPanelAloneAsync` | `ListView_ItemsLayoutGrid_LeavesAConsumerPanelAloneAsync` | `Control/ListViewTests.cs` |

### Additions: 3 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `Rebuilt_MatchesGoldenResolvedValuesAsync` | 3, was 1: the fact became a theory over the three themes | `Theming/ThemeParityTests.cs` |
| `SlideNavigationPresenter_TransitionEffect_RejectsAnUndeclaredValueAsync` | 1 | `Control/SlideNavigationPresenterTests.cs` |

Net: 3 added, none removed, 2 renamed. 1330 cases to 1333 on net10, 1327 to 1330
on net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the security scan findings

Nine regression cases for the three findings in the 2026-09-15 security scan: the
action providers on five automation peers refusing a disabled control, the
`WM_NCLBUTTONUP` wParam decode surviving a 64-bit value, and `NumberBox` keeping
`NaN` out of `Value`.

### Additions: 9 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `SplitButton_Disabled_InvokeAndExpandCollapse_ThrowElementNotEnabledExceptionAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `ToggleSplitButton_Disabled_ToggleAndExpandCollapse_ThrowElementNotEnabledExceptionAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `ToggleSwitch_Disabled_Toggle_ThrowsElementNotEnabledExceptionAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `DropDownButton_Disabled_ExpandCollapse_ThrowElementNotEnabledExceptionAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `NavigationViewItem_Disabled_InvokeAndSelect_ThrowElementNotEnabledExceptionAsync` | 1 | `Control/Rules/AutomationPeerTests.cs` |
| `FluenceWindow_MaxButtonRelease_DecodesAnyWParamWithoutThrowing` | 1 | `Windowing/FluenceWindowTests.cs` |
| `NumberBox_DirectValue_NaN_KeepsThePreviousValueAsync` | 1 | `Control/NumberBoxTests.cs` |
| `NumberBox_TypedNaN_IsRejectedLikeAnyUnparseableTextAsync` | 1 | `Control/NumberBoxTests.cs` |
| `NumberBox_NaNBound_DoesNotSwitchClampingOffAsync` | 1 | `Control/NumberBoxTests.cs` |

Net: 9 added, none removed. 1333 cases to 1342 on net10, 1330 to 1339 on
net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the fourth review pass

The cleared state `NumberBox` gives back, the flyout dismissal that two windows
of one application never triggered, the `FontWeight` a scoped `TextBlock` style
swallowed, and the read-only `ProgressRing` value pattern that answered a client
with silence.

### Removals: 1 line

| Method | Reason |
| ------ | ------ |
| `NumberBox_DirectValue_NaN_KeepsThePreviousValueAsync` | Superseded. `NaN` is WinUI's "value not set" sentinel and is exempt from coercion there (`NumberBox.cpp:120`, `:463`), so keeping the previous value was the wrong contract. `NumberBox_DirectValue_NaN_ClearsTheValueAndTheTextAsync` asserts the contract that replaced it, and the stuck spinner that motivated the old behaviour is now covered by `NumberBox_Click_OnAClearedValue_DoesNothingAsync`. |

### Additions: 13 lines

| Method | Occurrences | Source |
| ------ | ----------: | ------ |
| `NumberBox_DirectValue_NaN_ClearsTheValueAndTheTextAsync` | 1 | `Control/NumberBoxTests.cs` |
| `NumberBox_EmptyText_ClearsTheValueAsync` | 1 | `Control/NumberBoxTests.cs` |
| `NumberBox_Click_OnAClearedValue_DoesNothingAsync` | 1 | `Control/NumberBoxTests.cs` |
| `NumberBox_PlaceholderText_ShowsWhileTheValueIsClearedAsync` | 1 | `Control/NumberBoxTests.cs` |
| `FlyoutBase_LightDismiss_ClosesWhenAnotherWindowOfTheSameApplicationIsActivatedAsync` | 1 | `Control/FlyoutTests.cs` |
| `FlyoutBase_ForeignActivation_IsDeactivationToAWindowThatIsNotThePopup` | 6 | `Control/FlyoutTests.cs` |
| `Button_FontWeight_ReachesTheContentTextAsync` | 1 | `Control/ButtonTests.cs` |
| `ProgressRing_RangeValueSetValue_ReportsThatItIsReadOnlyAsync` | 1 | `Control/ProgressRingTests.cs` |

Net: 13 added, 1 removed. 1342 cases to 1354 on net10, 1339 to 1351 on net472.

### Status

Consumed. The baseline files beside this one carry the post-addition capture.

## Branch `fix/gallery-visual-defects`: the fifth review pass

The scoped `TextBlock` rebind covered one of the three properties the implicit
style sets, so the case that pinned it widened to all three and was renamed with
it. No case was added or removed.

### Renames: 1 line

| Old name | New name | Reason |
| ---- | ---- | ------ |
| `Button_FontWeight_ReachesTheContentTextAsync` | `Button_FontProperties_ReachTheContentTextAsync` | The rebind now covers `FontFamily` and `FontSize` as well as `FontWeight`, and the case asserts all three, so the name no longer says `FontWeight` alone. |

### Additions: none

Net: none added, none removed, 1 renamed. 1354 cases on net10 and 1351 on
net472, unchanged.

### Status

Consumed. The baseline files beside this one carry the post-rename capture.

## PowerShell integration and Gallery Home review

The branch refresh adds 17 cases per full target framework: two homepage route
and reflow cases, three theme-watcher handle cases, one footer automation case,
two tree removal/reset cases, six smooth-scroll cases, and three dialog-owner
lifetime cases. The existing homepage link case is renamed from
`GalleryHomePage_UsesHeaderLockupHeroAndGitHubLinkAsync` to
`GalleryHomePage_UsesProminentBrandAndAccessibleSocialLinksAsync` because it now
checks the larger brand and both rendered, accessible social icons.

No cases are removed. Discovery rises from 1351 to 1368 on net472 and from 1354
to 1371 on net10. The complementary runtime lanes report 1367 and 1368 cases,
respectively; these are the CI floors after screenshot filtering. The method-name diff contains only this rename and these additions.

## PR 74 reviewer follow-up

The review adds seven cases per full target framework, with no removals or renames:
one collapsed, data-bound tree selection case; two unchecked-addition cases;
one selected-addition reconciliation case; one scrolling case under sustained input;
and two hosted gallery scrolling cases for Colors and Data. The five new method names
are recorded in the method inventories. Discovery rises to 1375 on net472 and 1378
on net10. The corresponding runtime CI floors are 1374 and 1375 after screenshot
filtering, preserving the existing explicit regeneration skip.
