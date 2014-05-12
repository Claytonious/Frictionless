using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityTest
{
	[Serializable]
	public class IntegrationTestsRunnerWindow : EditorWindow
	{
		[SerializeField]
		private IntegrationTestRunnerRenderer renderer;

		#region runner steerign vars
		private List<TestComponent> testsToRun;
		private bool readyToRun;
		private bool isRunning;
		private bool isCompiling;
		private bool isBuilding;
		private bool consoleErrorOnPauseValue;

		#endregion

		public IntegrationTestsRunnerWindow ()
		{
			title = "Integration Tests Runner";
			renderer = new IntegrationTestRunnerRenderer(RunTests);
			
			EditorApplication.hierarchyWindowItemOnGUI += renderer.OnHierarchyWindowItemOnGui;
			renderer.InvalidateTestList();
		}

		public void OnDestroy ()
		{
			EditorApplication.hierarchyWindowItemOnGUI -= renderer.OnHierarchyWindowItemOnGui;
		}

		public void OnSelectionChange()
		{
			if (isRunning || EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			if (Selection.objects != null 
				&& Selection.objects.All (o => o is GameObject))
			{
				if (Selection.objects.Count () == 1)
				{
					var go = Selection.objects.Single () as GameObject;
					var temp = go.transform;
					while (temp != null)
					{
						var tc = temp.gameObject.GetComponent<TestComponent> ();
						if (tc != null && !tc.gameObject.activeSelf)
						{
							renderer.SelectInHierarchy (new[] { temp.gameObject });
							break;
						}
						temp = temp.parent;
					}
				}
				else if (Selection.objects.All (o => (o as GameObject).GetComponent<TestComponent> () != null))
				{
					renderer.SelectInHierarchy (Selection.objects.Select (o => o as GameObject));
					Repaint ();
				}
				
			}
		}

		public void OnHierarchyChange()
		{
			renderer.OnHierarchyChange(isRunning);
			if(renderer.forceRepaint) Repaint ();
		}

		private void RunTests ( IList<TestComponent> tests )
		{
			if (!tests.Any () || EditorApplication.isCompiling)
				return;
			Focus ();
			testsToRun = tests.ToList ();
			readyToRun = true;
			TestRunner.DisableAllTests ();
			EditorApplication.isPlaying = true;
		}

		public void Update()
		{
			if (readyToRun && EditorApplication.isPlaying)
			{
				readyToRun = false;
				var testRunner = TestRunner.GetTestRunner();
				testRunner.TestRunnerCallback.Add (new RunnerCallback (this));
				testRunner.InitRunner (testsToRun.Cast<ITestComponent> ().ToList ());
				consoleErrorOnPauseValue = IntegrationTestsRunnerWindow.GetConsoleErrorPause ();
				IntegrationTestsRunnerWindow.SetConsoleErrorPause (false);
				isRunning = true;

				if (renderer.blockUIWhenRunning)
					EditorUtility.DisplayProgressBar("Integration Test Runner",
												"Initializing",0);
			}

			if (EditorApplication.isCompiling)
			{
				isCompiling = true;
			}
			else if(isCompiling)
			{
				isCompiling = false;
				renderer.InvalidateTestList ();
				EditorApplication.RepaintHierarchyWindow ();
			}
		}

		public static bool GetConsoleErrorPause ()
		{
			Assembly assembly = Assembly.GetAssembly (typeof (SceneView));
			Type type = assembly.GetType ("UnityEditorInternal.LogEntries");
			PropertyInfo method = type.GetProperty ("consoleFlags");
			var result = (int)method.GetValue(new object (), new object[]{});
			return (result & (1 << 2)) != 0;
		}

		public static void SetConsoleErrorPause ( bool b )
		{
			Assembly assembly = Assembly.GetAssembly (typeof (SceneView));
			Type type = assembly.GetType ("UnityEditorInternal.LogEntries");
			MethodInfo method = type.GetMethod ("SetConsoleFlag");
			method.Invoke (new object (), new object[] { 1 << 2, b });
		}

		public void OnDidOpenScene()
		{
			renderer.InvalidateTestList();
			Repaint();
		}

		public void OnGUI()
		{
			if (isRunning)
			{
				Repaint ();
				if (!EditorApplication.isPlaying)
				{
					isRunning = false;
					Debug.Log ("Test run was interrupted. Reseting Test Runner.");
				}
			}

			
#if !UNITY_4_0 && !UNITY_4_0_1 && !UNITY_4_1 && !UNITY_4_2
			if (BuildPipeline.isBuildingPlayer)
			{
				isBuilding = true;
			}
			else if (isBuilding)
			{
				isBuilding = false;
				renderer.InvalidateTestList();
				Repaint ();
			}
#endif
			renderer.PrintHeadPanel(isRunning);
			renderer.PrintTestList();
			renderer.PrintSelectedTestDetails();
			
			if (renderer.forceRepaint)
			{
				renderer.forceRepaint = false;
				Repaint ();
			}
		}

		public void OnInspectorUpdate ()
		{
			if(focusedWindow != this)
				Repaint ();
		}

		public void RunSelectedTest()
		{
			if (Selection.activeGameObject != null)
			{
				var activeGO = Selection.activeGameObject;
				var topActiveGO = TestManager.FindTopGameObject(activeGO);
				if (topActiveGO.GetComponent<TestComponent>() != null)
					RunTests (new List<TestComponent> () { topActiveGO.GetComponent<TestComponent> () });
				else
					Debug.LogWarning("Selected object or it's parent has no TestComponent attached.");
			}
			else
			{
				Debug.LogWarning("No object is selected");
			}
		}

		class RunnerCallback : IntegrationTestRunner.ITestRunnerCallback
		{
			private IntegrationTestsRunnerWindow integrationTestRunnerWindow;
			private int testNumber=0;
			private int currentTestNumber = 0;

			public RunnerCallback(IntegrationTestsRunnerWindow integrationTestRunnerWindow)
			{
				this.integrationTestRunnerWindow = integrationTestRunnerWindow;
			}

			public void RunStarted (string platform, List<TestResult> testsToRun)
			{
				testNumber = testsToRun.Count;
				testsToRun.ForEach (t => t.Reset ());
				integrationTestRunnerWindow.renderer.UpdateResults (testsToRun);
			}

			public void RunFinished (List<TestResult> testResults)
			{
				integrationTestRunnerWindow.isRunning = false;
				integrationTestRunnerWindow.renderer.OnTestRunFinished();
				integrationTestRunnerWindow.Repaint ();
				EditorApplication.isPlaying = false;
				if (integrationTestRunnerWindow.renderer.blockUIWhenRunning)
					EditorUtility.ClearProgressBar();
				IntegrationTestsRunnerWindow.SetConsoleErrorPause (integrationTestRunnerWindow.consoleErrorOnPauseValue);
			}

			public void TestStarted (TestResult test)
			{
				if (integrationTestRunnerWindow.renderer.blockUIWhenRunning
					&& EditorUtility.DisplayCancelableProgressBar("Integration Test Runner",
																"Running " + test.Name,
																(float) currentTestNumber / testNumber))
				{
					integrationTestRunnerWindow.isRunning = false;
					EditorApplication.isPlaying = false;
				}

				integrationTestRunnerWindow.renderer.UpdateResults (new List<TestResult> {test});
				integrationTestRunnerWindow.Repaint();
			}

			public void TestFinished (TestResult test)
			{
				currentTestNumber++;
				integrationTestRunnerWindow.renderer.UpdateResults(new List<TestResult> { test });
				integrationTestRunnerWindow.Repaint();
			}

			public void TestRunInterrupted(List<TestResult> testsNotRun)
			{
				Debug.Log("Test run interrupted");
				RunFinished(new List<TestResult>());
			}
		}

		public void RunAllTests()
		{
			RunTests (renderer.GetVisibleNotIgnoredTests ());
		}

		[MenuItem ("Unity Test Tools/Integration Tests/Integration Test Runner %#&t")]
		public static IntegrationTestsRunnerWindow ShowWindow()
		{
			var w = GetWindow(typeof(IntegrationTestsRunnerWindow));
			w.Show();
			return w as IntegrationTestsRunnerWindow;
		}

		[MenuItem ("Unity Test Tools/Integration Tests/Run all integration tests %#t")]
		public static void RunAllTestsMenu()
		{
			var trw = ShowWindow();
			trw.RunAllTests();
		}

		[MenuItem ("Unity Test Tools/Integration Tests/Run selected integration test %t")]
		[MenuItem("CONTEXT/TestComponent/Run")]
		public static void RunSelectedMenu()
		{
			var trw = ShowWindow();
			trw.RunSelectedTest();
		}
	}
}
