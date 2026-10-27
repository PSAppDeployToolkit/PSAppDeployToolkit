---
name: fluence-consumer-starter
description: Create a new Fluence.Wpf consumer WPF app, a NavigationView MVVM shell, or a Windows Settings-inspired shell from the installed Fluence consumer templates.
---

# Fluence consumer starter

Use the shipped templates to create a consumer app; keep this skill as a router and do not reproduce template source.

## Choose a starter

| Need | `dotnet new` short name |
| --- | --- |
| Plain desktop app with a small working form | `fluence-window` |
| NavigationView shell with CommunityToolkit.Mvvm, compact navigation, and an accent action | `fluence-nav-mvvm` |
| Settings-inspired categories, Back history, collapsible navigation, and app-local options | `fluence-settings` |

If the user named a shape, choose its matching starter. Otherwise, choose the smallest starter that fits the requested app and state the choice.

## Generate and verify

1. Use a supplied template package path and custom hive when provided. Install with `dotnet new install <template-nupkg-path> --debug:custom-hive <hive-path>`, then list with `dotnet new list fluence --debug:custom-hive <hive-path>`. Pass the same custom-hive option to every `dotnet new` install, list, generate, or uninstall call. If the user supplies a local package feed, use its exact template package and its matching `Fluence.Wpf` library package. Without local paths, first check `dotnet new list fluence`; if the template is absent, request a supplied local package or an explicitly validated published template version. Never install an unpinned latest package as a fallback.
2. Generate into a fresh consumer directory with a project name matching the app, for example `dotnet new fluence-window -n Contoso.Sample -o .\Contoso.Sample --debug:custom-hive <hive-path>` when using a custom hive. Keep the generated app in the consumer's own namespace and use the consumer's own branding; the starter does not assign Fluence branding.
3. Restore and build the generated `.csproj` from the consumer directory. Prefer a supplied `NuGet.Config` and pass it with `--configfile <config-path>`, along with the supplied package cache using `--packages <packages-path>`. If only a local feed is supplied, create a `NuGet.Config` with `<clear />`, that local feed, and `https://api.nuget.org/v3/index.json`, then use it with `--configfile`. Keep the matching library package and package cache on the supplied local paths. Avoid multiple `--source` options for a local path and URL; restore may treat the URL as a local path on Windows. The generated `.gitignore` excludes a project-local `.nuget-packages/` cache, `bin/`, `obj/`, and the temporary local-feed `NuGet.Config`; confirm it is present before committing consumer code.
4. Run the app on Windows and inspect the requested routes or controls, resizing, keyboard use, and Light/Dark theme changes. Check the generated app's icon state and, when requested, verify a consumer-supplied icon. The simple starter has a generic title-bar vector first at 12 DIP, with no Back or pane toggle. In the navigation shell, check the generic icon first at 12 DIP, always-visible Back disabled at the root and enabled by real history, selected-route synchronization on Back, the hamburger, Settings compact-navigation preference, Activity accent action, and minimize glyph. In the settings shell, check Back first at 12 DIP with no title-bar icon, disabled at the root and enabled by category history, retained page state, the expanded/compact pane toggle, four bundled color sidebar images, monochrome page glyphs, no duplicate category subtitle, and minimize glyph. Confirm the two artwork notices copy to output and the app uses ordinary WPF assets without Windows App SDK components. Settings options are app-local and reset when the app restarts. Treat structural checker results as compile and source evidence; inspect the native UI to verify interactions.
5. Report the selected short name, generated path, restore/build result, app icon state, run and interaction checks performed, and anything not verified. Treat a successful build as compile evidence only.
