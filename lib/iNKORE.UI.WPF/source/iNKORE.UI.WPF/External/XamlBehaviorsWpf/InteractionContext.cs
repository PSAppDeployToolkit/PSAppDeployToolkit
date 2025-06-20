// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Navigation;

    internal static class InteractionContext
    {
        #region private fields

        // Our Navigation actions can no longer take a hard dependency on PlayerContext. Fortunately we use very little of the PlayerContext from the runtime
        // so we accumulate reflection info here to use to call the runtime dynamically. 
        private static Assembly runtimeAssembly;
        private static object playerContextInstance;
        private static object activeNavigationViewModelObject;
        private static PropertyInfo libraryNamePropertyInfo;
        private static PropertyInfo activeNavigationViewModelPropertyInfo;
        private static PropertyInfo canGoBackPropertyInfo;
        private static PropertyInfo canGoForwardPropertyInfo;
        private static PropertyInfo sketchFlowAnimationPlayerPropertyInfo;
        private static MethodInfo goBackMethodInfo;
        private static MethodInfo goForwardMethodInfo;
        private static MethodInfo navigateToScreenMethodInfo;
        private static MethodInfo invokeStateChangeMethodInfo;
        private static MethodInfo playSketchFlowAnimationMethodInfo;

        private static NavigationService navigationService;
        private static readonly string LibraryName;
        private static readonly Dictionary<string, Serializer.Data> NavigationData =
            new Dictionary<string, Serializer.Data>(StringComparer.OrdinalIgnoreCase);


        #endregion private fields

        static InteractionContext()
        {
            InteractionContext.runtimeAssembly = InteractionContext.FindPlatformRuntimeAssembly();

            if (InteractionContext.runtimeAssembly != null)
            {
                InitializeRuntimeNavigation();
                InteractionContext.LibraryName = (string)InteractionContext.libraryNamePropertyInfo.GetValue(InteractionContext.playerContextInstance, null);

                InteractionContext.LoadNavigationData(InteractionContext.LibraryName);
            }
            else
            {
                InteractionContext.InitalizePlatformNavigation();
            }
        }

        #region properties

        public static object ActiveNavigationViewModelObject
        {
            get
            {
                return
                    activeNavigationViewModelObject ??
                    InteractionContext.activeNavigationViewModelPropertyInfo.GetValue(
                        InteractionContext.playerContextInstance,
                        null);
            }

            internal set // for unit test
            {
                activeNavigationViewModelObject = value;
            }
        }

        private static bool IsPrototypingRuntimeLoaded
        {
            get { return InteractionContext.runtimeAssembly != null; }
        }

        private static bool CanGoBack
        {
            get { return (bool)InteractionContext.canGoBackPropertyInfo.GetValue(ActiveNavigationViewModelObject, null); }
        }

        private static bool CanGoForward
        {
            get { return (bool)InteractionContext.canGoForwardPropertyInfo.GetValue(ActiveNavigationViewModelObject, null); }
        }

        #endregion properties

        #region public methods

        public static void GoBack()
        {
            if (InteractionContext.IsPrototypingRuntimeLoaded)
            {
                if (InteractionContext.CanGoBack)
                {
                    InteractionContext.goBackMethodInfo.Invoke(ActiveNavigationViewModelObject, null);
                }
            }
            else
            {
                InteractionContext.PlatformGoBack();
            }
        }

        public static void GoForward()
        {
            if (InteractionContext.IsPrototypingRuntimeLoaded)
            {
                if (InteractionContext.CanGoForward)
                {
                    InteractionContext.goForwardMethodInfo.Invoke(ActiveNavigationViewModelObject, null);
                }
            }
            else
            {
                InteractionContext.PlatformGoForward();
            }
        }

        public static bool IsScreen(string screenName)
        {
            if (!InteractionContext.IsPrototypingRuntimeLoaded)
            {
                return false;
            }

            return InteractionContext.GetScreenClassName(screenName) != null;
        }

        public static void GoToScreen(string screenName, Assembly assembly)
        {
            if (InteractionContext.IsPrototypingRuntimeLoaded)
            {
                string screenClassName = InteractionContext.GetScreenClassName(screenName);

                if (string.IsNullOrEmpty(screenClassName))
                {
                    return;
                }

                //	an array that ends up being parameters to NavigationViewModel.NavigateToScreen(string name, bool record)
                object[] paramArrary = new object[] { screenClassName, true };

                InteractionContext.navigateToScreenMethodInfo.Invoke(ActiveNavigationViewModelObject, paramArrary);
            }
            else
            {
                // Verify we could tell where we were
                if (assembly == null)
                {
                    return;
                }

                // The Assembly which is hosting the calling behavior is the one we want to go to the component in
                AssemblyName assemblyName = new AssemblyName(assembly.FullName);
                if (assemblyName != null)
                {
                    string hostAssembly = assemblyName.Name;
                    InteractionContext.PlatformGoToScreen(hostAssembly, screenName);
                }
            }
        }

        public static void GoToState(string screen, string state)
        {
            // If you have XAML like the following - 

            //	<i:Interaction.Triggers>
            //		<i:EventTrigger>
            //			<pb:ActivateStateAction/>
            //		</i:EventTrigger>
            //	</i:Interaction.Triggers>
            //
            // ...then the Action fires on the Load event without any sort of initialization.
            // The param checks below are intended to keep such actions (and others like it)
            // from doing any harm when triggered
            if (string.IsNullOrEmpty(screen) || string.IsNullOrEmpty(state))
            {
                return;
            }

            if (InteractionContext.IsPrototypingRuntimeLoaded)
            {
                //	an array that ends up being parameters to NavigationViewModel.InvokeStateChange(string screen, string state, bool record)
                object[] paramArrary = new object[] { screen, state, false };

                InteractionContext.invokeStateChangeMethodInfo.Invoke(ActiveNavigationViewModelObject, paramArrary);
            }
            else
            {
                // Neither platform (WPF or Silverlight) currently supports this as an option
            }
        }

        public static void PlaySketchFlowAnimation(string sketchFlowAnimation, string owningScreen)
        {
            // If you have XAML like the following - 

            //	<i:Interaction.Triggers>
            //		<i:EventTrigger>
            //			<pb:PlaySketchFlowAnimationAction/>
            //		</i:EventTrigger>
            //	</i:Interaction.Triggers>
            //
            // ...then the Action fires on the Load event without any sort of initialization.
            // The param checks below are intended to keep such actions (and others like it)
            // from doing any harm when triggered

            if (string.IsNullOrEmpty(sketchFlowAnimation) || string.IsNullOrEmpty(owningScreen))
            {
                return;
            }

            if (InteractionContext.IsPrototypingRuntimeLoaded)
            {
                object activeNavigationViewModel = InteractionContext.activeNavigationViewModelPropertyInfo.GetValue(InteractionContext.playerContextInstance, null);

                object[] paramArrary = new object[]
                {
                    sketchFlowAnimation,
                    owningScreen
                };

                InteractionContext.playSketchFlowAnimationMethodInfo.Invoke(activeNavigationViewModel, paramArrary);
            }
            else
            {
                // Neither platform (WPF or Silverlight) currently supports this as an option
            }
        }

        #endregion public methods

        #region private methods

        private static void InitializeRuntimeNavigation()
        {
            Type playerContextType = InteractionContext.runtimeAssembly.GetType("Microsoft.Expression.Prototyping.Services.PlayerContext");
            PropertyInfo instancePropertyInfo = playerContextType.GetProperty("Instance");

            InteractionContext.activeNavigationViewModelPropertyInfo = playerContextType.GetProperty("ActiveNavigationViewModel");
            InteractionContext.libraryNamePropertyInfo = playerContextType.GetProperty("LibraryName");
            InteractionContext.playerContextInstance = instancePropertyInfo.GetValue(null, null);

            Type navigationViewModelType = InteractionContext.runtimeAssembly.GetType("Microsoft.Expression.Prototyping.Navigation.NavigationViewModel");
            InteractionContext.canGoBackPropertyInfo = navigationViewModelType.GetProperty("CanGoBack");
            InteractionContext.canGoForwardPropertyInfo = navigationViewModelType.GetProperty("CanGoForward");
            InteractionContext.goBackMethodInfo = navigationViewModelType.GetMethod("GoBack");
            InteractionContext.goForwardMethodInfo = navigationViewModelType.GetMethod("GoForward");
            InteractionContext.navigateToScreenMethodInfo = navigationViewModelType.GetMethod("NavigateToScreen");
            InteractionContext.invokeStateChangeMethodInfo = navigationViewModelType.GetMethod("InvokeStateChange");
            InteractionContext.playSketchFlowAnimationMethodInfo = navigationViewModelType.GetMethod("PlaySketchFlowAnimation");
            InteractionContext.sketchFlowAnimationPlayerPropertyInfo = navigationViewModelType.GetProperty("SketchFlowAnimationPlayer");
        }

        private static Serializer.Data LoadNavigationData(string assemblyName)
        {
            Serializer.Data data = null;
            if (InteractionContext.NavigationData.TryGetValue(assemblyName, out data))
            {
                return data;
            }

            Application app = Application.Current;
            string path = string.Format(CultureInfo.InvariantCulture, "/{0};component/Sketch.Flow", assemblyName);
            try
            {
                var info = Application.GetResourceStream(new Uri(path, UriKind.Relative));
                if (info != null)
                {
                    data = Serializer.Deserialize(info.Stream);
                    InteractionContext.NavigationData[assemblyName] = data;
                }
            }
            catch (IOException) { }
            catch (InvalidOperationException) { }

            return data ?? new Serializer.Data();
        }

        private static string GetScreenClassName(string screenName)
        {
            Serializer.Data data = null;
            InteractionContext.NavigationData.TryGetValue(InteractionContext.LibraryName, out data);
            if (data == null || data.Screens == null)
            {
                return null;
            }

            if (!data.Screens.Any(screen => screen.ClassName == screenName))
            {
                screenName = data.Screens
                    .Where(screen => screen.DisplayName == screenName)
                    .Select(screen => screen.ClassName)
                    .FirstOrDefault();
            }

            return screenName;
        }

        private static void InitalizePlatformNavigation()
        {
            NavigationWindow navigationWindow = Application.Current.MainWindow as NavigationWindow;
            if (navigationWindow != null)
            {
                InteractionContext.navigationService = navigationWindow.NavigationService;
            }
        }

        private static Assembly FindPlatformRuntimeAssembly()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name.Equals("Microsoft.Expression.Prototyping.Runtime"))
                {
                    return assembly;
                }
            }
            return null;
        }

        public static void PlatformGoBack()
        {
            if (InteractionContext.navigationService != null && InteractionContext.PlatformCanGoBack)
            {
                InteractionContext.navigationService.GoBack();
            }
        }

        public static void PlatformGoForward()
        {
            if (InteractionContext.navigationService != null && InteractionContext.PlatformCanGoForward)
            {
                InteractionContext.navigationService.GoForward();
            }
        }

        public static void PlatformGoToScreen(string assemblyName, string screen)
        {
            System.Runtime.Remoting.ObjectHandle handle = Activator.CreateInstance(assemblyName, screen);
            InteractionContext.navigationService.Navigate(handle.Unwrap());
        }

        private static bool PlatformCanGoBack
        {
            get
            {
                if (InteractionContext.navigationService != null)
                {
                    return InteractionContext.navigationService.CanGoBack;
                }
                return false;
            }
        }

        private static bool PlatformCanGoForward
        {
            get
            {
                if (InteractionContext.navigationService != null)
                {
                    return InteractionContext.navigationService.CanGoForward;
                }
                return false;
            }
        }

        #endregion private methods
    }
}
