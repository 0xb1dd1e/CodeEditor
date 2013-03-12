using System;
using System.IO;
using System.Linq;
using CodeEditor.Composition;
using CodeEditor.Composition.Hosting;
using CodeEditor.IO;
using CodeEditor.Logging;
using CodeEditor.ServiceClient;
using CodeEditor.Text.UI.Unity.Engine;
using UnityEditor;

namespace CodeEditor.Text.UI.Unity.Editor.Implementation
{
	[InitializeOnLoad]
	public static class Main
	{
		static Main()
		{
			CodeEditorWindow.TextViewFactory = ViewForFile;
			NavigatorWindow.ProviderAggregatorFactory = GetExportedValue<INavigateToItemProviderAggregator>;
		}

		static ITextView ViewForFile(string fileName)
		{
			return GetExportedValue<ITextViewFactory>().ViewForFile(fileName);
		}

		static T GetExportedValue<T>()
		{
			return CompositionContainer.GetExportedValue<T>();
		}

		static CompositionContainer CompositionContainer
		{
			get { return Container.Value; }
		}

		static readonly Lazy<CompositionContainer> Container = new Lazy<CompositionContainer>(CreateCompositionContainer);

		static CompositionContainer CreateCompositionContainer()
		{
			var container = new CompositionContainer(AppDomain.CurrentDomain.GetAssemblies().ToArray());
			container.AddExportedValue<IFileSystem>(new UnityEditorFileSystem());
			container.AddExportedValue<IServerExecutableProvider>(new ServerExecutableProvider(ServerExecutable));
			container.AddExportedValue<IMonoExecutableProvider>(new MonoExecutableProvider(MonoExecutable));
			container.AddExportedValue<IFileNavigationService>(new UnityEditorFileNavigationService());
			if (UnityEngine.Debug.isDebugBuild)
				container.AddExportedValue<ILogger>(new UnityLogger());
			return container;
		}

		class UnityEditorFileNavigationService : IFileNavigationService
		{
			public void NavigateTo(string fileName, IAnchor anchor = null)
			{
				CodeEditorWindow.OpenWindowFor(fileName, PositionFrom(anchor));
			}

			static Position? PositionFrom(IAnchor anchor)
			{
				var positionAnchor = anchor as PositionAnchor;
				if (positionAnchor == null)
					return null;
				return positionAnchor.Position;
			}
		}

		static string ServerExecutable
		{
			get { return Path.Combine(ProjectPath, "Library/CodeEditor/Server/CodeEditor.ServiceHost.exe"); }
		}

		static string MonoExecutable
		{
			get { return Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin/mono.exe"); }
		}

		static string ProjectPath
		{
			get { return Path.GetDirectoryName(UnityEngine.Application.dataPath); }
		}

		class MonoExecutableProvider : IMonoExecutableProvider
		{
			readonly string _monoExecutable;

			public MonoExecutableProvider(string monoExecutable)
			{
				_monoExecutable = monoExecutable;
			}

			string IMonoExecutableProvider.MonoExecutable
			{
				get { return _monoExecutable; }
			}
		}

		class UnityLogger : ILogger
		{
			public void Log(object value)
			{
				UnityEngine.Debug.Log(value);
			}

			public void LogError(Exception exception)
			{
				UnityEngine.Debug.LogException(exception);
			}
		}
	}
}