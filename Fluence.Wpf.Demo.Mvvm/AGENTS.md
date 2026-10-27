# MVVM sample guidance

Read this file when changing the Task Manager sample.

- Keep interaction logic in the view model. MainWindow code-behind should only assign DataContext and initialize the view.
- MainViewModel owns the unfiltered TaskItemViewModel collection and rebuilds DisplayedTasks after filter or completion changes. Derive StatusText and ProgressValue after the rebuild. Do not add NotifyPropertyChangedFor to _activeFilter because that notifies before DisplayedTasks is rebuilt.
- Use EnumToBoolConverter for filter radio buttons. Delete commands inside a DataTemplate resolve through the Window ancestor so TaskItemViewModel stays independent of its parent.
- App.xaml has no MergedDictionaries. ApplicationThemeManager.Apply seeds the three application resource slots; a manual Generic.xaml merge adds a stale fourth entry.
- Validate the project with its configured .NET target. The gallery-specific guidance is in Fluence.Wpf.Demo/AGENTS.md.
