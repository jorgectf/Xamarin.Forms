﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources.Core;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.UWP;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Xamarin.Forms
{
	public static partial class Forms
	{
		const string LogFormat = "[{0}] {1}";

		//static ApplicationExecutionState s_state;

		public static bool IsInitialized { get; private set; }
		
		public static void Init(Microsoft.UI.Xaml.LaunchActivatedEventArgs launchActivatedEventArgs, IEnumerable<Assembly> rendererAssemblies = null)
		{
			if (IsInitialized)
				return;

			var accent = (WSolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["SystemColorControlAccentBrush"];
			Color.SetAccent(accent.ToFormsColor());

#if !UWP_16299
			Log.Listeners.Add(new DelegateLogListener((c, m) => Debug.WriteLine(LogFormat, c, m)));
#else
			Log.Listeners.Add(new DelegateLogListener((c, m) => Trace.WriteLine(m, c)));
#endif
			if (!Microsoft.UI.Xaml.Application.Current.Resources.ContainsKey("RootContainerStyle"))
			{
				Microsoft.UI.Xaml.Application.Current.Resources.MergedDictionaries.Add(GetTabletResources());
			}

			try
			{
				Microsoft.UI.Xaml.Application.Current.Resources.MergedDictionaries.Add(new Microsoft.UI.Xaml.Controls.XamlControlsResources());
			}
			catch
			{
				Log.Warning("Resources", "Unable to load WinUI resources. Try adding Xamarin.Forms nuget to UWP project");
			}

			Device.SetIdiom(TargetIdiom.Tablet);
			Device.SetFlowDirection(GetFlowDirection());

			var platformServices = new WindowsPlatformServices(Window.Current.DispatcherQueue);

			Device.PlatformServices = platformServices;
			Device.PlatformInvalidator = platformServices;
			
			// TODO SHANE WINUI
			//if(Window.Current?.DispatcherQueue != null)
			//	Device.PlatformServices = new WindowsPlatformServices(Window.Current.DispatcherQueue);

			Device.SetFlags(s_flags);
			Device.Info = new WindowsDeviceInfo();

			switch (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily)
			{
				case "Windows.Desktop":
					if (Windows.UI.ViewManagement.UIViewSettings.GetForCurrentView().UserInteractionMode ==
						Windows.UI.ViewManagement.UserInteractionMode.Touch)
						Device.SetIdiom(TargetIdiom.Tablet);
					else
						Device.SetIdiom(TargetIdiom.Desktop);
					break;
				case "Windows.Mobile":
					Device.SetIdiom(TargetIdiom.Phone);
					break;
				case "Windows.Xbox":
					Device.SetIdiom(TargetIdiom.TV);
					break;
				default:
					Device.SetIdiom(TargetIdiom.Unsupported);
					break;
			}

			ExpressionSearch.Default = new WindowsExpressionSearch();

			Registrar.ExtraAssemblies = rendererAssemblies?.ToArray();
		}

#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
		public static void InitDispatcher(Microsoft.System.DispatcherQueue dispatcher)
#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.
		{
			Device.PlatformServices = new WindowsPlatformServices(dispatcher);

			var assemblies = Device.GetAssemblies();
			// TODO WINUI
			//Registrar.Registered.Register(typeof(ContentPage), typeof(PageRenderer));
			//Registrar.Registered.Register(typeof(Label), typeof(LabelRenderer));

			//Registrar.Registered.Register(typeof(ContentPage), typeof(PageRenderer));

			Registrar.RegisterAll(new[] { typeof(ExportRendererAttribute), typeof(ExportCellAttribute), typeof(ExportImageSourceHandlerAttribute), typeof(ExportFontAttribute) });

			IsInitialized = true;
			// TODO SHANE
			//s_state = launchActivatedEventArgs.PreviousExecutionState;

			//Platform.UWP.Platform.SubscribeAlertsAndActionSheets();
		}

		static FlowDirection GetFlowDirection()
		{
			string resourceFlowDirection = ResourceContext.GetForCurrentView().QualifierValues["LayoutDirection"];
			if (resourceFlowDirection == "LTR")
				return FlowDirection.LeftToRight;
			else if (resourceFlowDirection == "RTL")
				return FlowDirection.RightToLeft;

			return FlowDirection.MatchParent;
		}

		internal static Microsoft.UI.Xaml.ResourceDictionary GetTabletResources()
		{
			return new Microsoft.UI.Xaml.ResourceDictionary {
				Source = new Uri("ms-appx:///Xamarin.Forms.Platform.UAP/Resources.xbf")
			};
		}
	}
}
