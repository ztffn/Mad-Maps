using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace MadMaps
{
	[InitializeOnLoad]
	public static class StartupWindowLauncher
	{
		static StartupWindowLauncher()
		{
			if(!EditorPrefs.GetBool(StartupWindow.SHOWATSTART_KEY, true))
			{				
				return;
			}
			var lastShowJSON = EditorPrefs.GetString(StartupWindow.LASTAUTOSHOW_KEY, null);
			if(!string.IsNullOrEmpty(lastShowJSON))
			{
				var lastShow = JsonUtility.FromJson<DateTime>(lastShowJSON);
				var now = DateTime.Now;
				if((now - lastShow).Hours > StartupWindow.MIN_HOURS_BETWEEN_AUTO)
				{
					return;
				}
			}
			StartupWindow.ShowWindow();
		}
	}

	
	public class StartupWindow : EditorWindow 
	{
		public const string VERSION = "v0.1.5";
		public const int MIN_HOURS_BETWEEN_AUTO = 6;
		public const string SHOWATSTART_KEY = "MadMaps_ShowStartup";
		public const string LASTAUTOSHOW_KEY = "MadMaps_LastStartupWindowTime";
		public const string DEMO_URL = "http://www.lrtw.net/madmaps/demo/MadMapsDemoAssets.unitypackage";
		public const string DOCUMENTATION_URL = "http://lrtw.net/madmaps/";
		public const string SUPPORT_EMAIL = "mailto:seandgfinnegan+madmaps@gmail.com";
		const string SPLASH_PATH = "MadMaps/StartupWindowSplash";

		Texture _splashTexture;

		[MenuItem("Tools/Mad Maps/Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL(DOCUMENTATION_URL);
        }

        [MenuItem("Tools/Mad Maps/Contact Support")]
        public static void ContactSupport()
        {
            Application.OpenURL(SUPPORT_EMAIL);
        }

        [MenuItem("Tools/Mad Maps/Download Demo Assets")]
        public static void DownloadDemoAssets()
        {
            Application.OpenURL(DEMO_URL);
        }

		[MenuItem("Tools/Mad Maps/Getting Started")]
		public static void ShowWindow()
		{
			GetWindow<StartupWindow>(true);
			EditorPrefs.SetString(LASTAUTOSHOW_KEY, JsonUtility.ToJson(DateTime.Now));
		}

		void OnGUI()
		{
			titleContent = GUIContent.none;
			if(!_splashTexture)
			{
				_splashTexture = Resources.Load<Texture2D>(SPLASH_PATH);
			}
			GUILayout.Label(new GUIContent(_splashTexture));
			if(GUILayout.Button("Download Demo Assets"))
			{
				DownloadDemoAssets();
			}
			if(GUILayout.Button("Contact Support"))
			{
				ContactSupport();
			}
			if(GUILayout.Button("View Documentation"))
			{
				OpenDocumentation();
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label(VERSION, GUILayout.ExpandWidth(true));
			EditorPrefs.SetBool(SHOWATSTART_KEY, GUILayout.Toggle(EditorPrefs.GetBool(SHOWATSTART_KEY, true), "Show At Startup", GUILayout.Width(130)));
			GUILayout.EndHorizontal();
		}
	}

}
