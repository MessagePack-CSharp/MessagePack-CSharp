using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RuntimeUnitTestToolkit
{
    public class UnitTestRunner : MonoBehaviour
    {
        UnitTestData testData;
        List<Pair> additionalActionsOnFirst = new List<Pair>();

        public Button clearButton;
        public RectTransform list;
        public Scrollbar listScrollBar;

        public Text logText;
        public Scrollbar logScrollBar;

        readonly Color passColor = new Color(0f, 1f, 0f, 1f); // green
        readonly Color failColor = new Color(1f, 0f, 0f, 1f); // red
        readonly Color normalColor = new Color(1f, 1f, 1f, 1f); // white

        bool allTestGreen = true;
        bool logClear = false;

        void Start()
        {
            try
            {
                UnityEngine.Application.logMessageReceived += (a, b, c) =>
                {
                    if (a.Contains("Mesh can not have more than 65000 vertices"))
                    {
                        logClear = true;
                    }
                    else
                    {
                        AppendToGraphicText("[" + c + "]" + a + "\n");
                        WriteToConsole("[" + c + "]" + a);
                    }
                };

                // register all test types
                testData = UnitTestData.CreateFromAllAssemblies();

                var executeAll = new List<Func<Coroutine>>();
                foreach (var ___item in testData.TestGroups)
                {
                    var actionList = ___item; // be careful, capture in lambda

                    executeAll.Add(() => StartCoroutine(RunTestInCoroutine(actionList.Key, actionList.Value, true)));
                    Add(actionList.Key, actionList.Value.Name, () => StartCoroutine(RunTestInCoroutine(actionList.Key, actionList.Value, false)));
                }

                var executeAllButton = Add("$<>__AllTest", "Run All Tests", () => StartCoroutine(ExecuteAllInCoroutine(executeAll)));

                clearButton.gameObject.GetComponent<Image>().color = new Color(170 / 255f, 170 / 255f, 170 / 255f, 1);
                executeAllButton.gameObject.GetComponent<Image>().color = new Color(250 / 255f, 150 / 255f, 150 / 255f, 1);
                executeAllButton.transform.SetSiblingIndex(1);

                additionalActionsOnFirst.Reverse();
                foreach (var item in additionalActionsOnFirst)
                {
                    var newButton = GameObject.Instantiate(clearButton);
                    newButton.name = item.Name;
                    newButton.onClick.RemoveAllListeners();
                    newButton.GetComponentInChildren<Text>().text = item.Name;
                    newButton.onClick.AddListener(item.Action);
                    newButton.transform.SetParent(list);
                    newButton.transform.SetSiblingIndex(1);
                }

                clearButton.onClick.AddListener(() =>
                {
                    logText.text = "";
                    foreach (var btn in list.GetComponentsInChildren<Button>())
                    {
                        btn.interactable = true;
                        btn.GetComponent<Image>().color = normalColor;
                    }
                    executeAllButton.gameObject.GetComponent<Image>().color = new Color(250 / 255f, 150 / 255f, 150 / 255f, 1);
                });

                listScrollBar.value = 1;
                logScrollBar.value = 1;

                if (Application.isBatchMode)
                {
                    // run immediately in player
                    StartCoroutine(ExecuteAllInCoroutine(executeAll));
                }
            }
            catch (Exception ex)
            {
                if (Application.isBatchMode)
                {
                    // when failed(can not start runner), quit immediately.
                    WriteToConsole(ex.ToString());
                    Application.Quit(1);
                }
                else
                {
                    throw;
                }
            }
        }

        Button Add(string key, string title, UnityAction test)
        {
            var newButton = GameObject.Instantiate(clearButton);
            newButton.name = key;
            newButton.onClick.RemoveAllListeners();
            newButton.GetComponentInChildren<Text>().text = title;
            newButton.onClick.AddListener(test);

            newButton.transform.SetParent(list);
            return newButton;
        }

        static IEnumerable<Type> GetTestTargetTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var n = assembly.FullName;
                if (n.StartsWith("UnityEngine")) continue;
                if (n.StartsWith("mscorlib")) continue;
                if (n.StartsWith("System")) continue;

                foreach (var item in assembly.GetTypes())
                {
                    foreach (var method in item.GetMethods())
                    {
                        TestAttribute t1 = null;
                        try
                        {
                            t1 = method.GetCustomAttribute<TestAttribute>(true);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("TestAttribute Load Fail, Assembly:" + assembly.FullName);
                            Debug.LogException(ex);
                            goto NEXT_ASSEMBLY;
                        }
                        if (t1 != null)
                        {
                            yield return item;
                            break;
                        }

                        UnityTestAttribute t2 = null;
                        try
                        {
                            t2 = method.GetCustomAttribute<UnityTestAttribute>(true);
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("UnityTestAttribute Load Fail, Assembly:" + assembly.FullName);
                            Debug.LogException(ex);
                            goto NEXT_ASSEMBLY;
                        }
                        if (t2 != null)
                        {
                            yield return item;
                            break;
                        }
                    }
                }

                NEXT_ASSEMBLY:
                continue;
            }
        }

        public void AddCutomAction(string name, UnityAction action)
        {
            additionalActionsOnFirst.Add(new Pair { Name = name, Action = action });
        }

        System.Collections.IEnumerator ScrollLogToEndNextFrame()
        {
            yield return null;
            yield return null;
            logScrollBar.value = 0;
        }

        IEnumerator RunTestInCoroutine(string key, TestGroup testGroup, bool withoutGlobalSetup)
        {
            Button self = null;
            foreach (var btn in list.GetComponentsInChildren<Button>())
            {
                btn.interactable = false;
                if (btn.name == key) self = btn;
            }
            if (self != null)
            {
                self.GetComponent<Image>().color = normalColor;
            }

            var allGreen = true;

            AppendToGraphicText("<color=yellow>" + testGroup.Name + "</color>\n");
            WriteToConsole("Begin Test Class: " + testGroup.Name);
            yield return null;

            // GlobalSetup
            if (!withoutGlobalSetup)
            {
                foreach (var item in this.testData.GlobalSetups) item();
            }

            // OnetimeSetup
            foreach (var item in testGroup.OnetimeSetups) item();

            var totalExecutionTime = new List<double>();
            foreach (var item2 in testGroup.Tests)
            {
                try
                {
                    AppendToGraphicText("<color=teal>" + item2.name + "</color>\n");
                    yield return null;

                    // SetUp
                    foreach (var setup in testGroup.Setups) setup();

                    // UnitySetUp
                    foreach (var setup in testGroup.UnitySetUps) yield return StartCoroutine(setup());

                    // before start, cleanup
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    var v = item2.test;

                    var methodStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    Exception exception = null;
                    if (v is Action)
                    {
                        try
                        {
                            ((Action)v).Invoke();
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                    }
                    else
                    {
                        var coroutineFactory = (Func<IEnumerator>)v;
                        IEnumerator coroutine = null;
                        try
                        {
                            coroutine = coroutineFactory();
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                        if (exception == null)
                        {
                            yield return StartCoroutine(UnwrapEnumerator(coroutine, ex =>
                            {
                                exception = ex;
                            }));
                        }
                    }
                    methodStopwatch.Stop();
                    totalExecutionTime.Add(methodStopwatch.Elapsed.TotalMilliseconds);
                    if (exception == null)
                    {
                        AppendToGraphicText("OK, " + methodStopwatch.Elapsed.TotalMilliseconds.ToString("0.00") + "ms\n");
                        WriteToConsoleResult(item2.name + ", " + methodStopwatch.Elapsed.TotalMilliseconds.ToString("0.00") + "ms", true);
                    }
                    else
                    {
                        AppendToGraphicText("<color=red>" + exception.ToString() + "</color>\n");
                        WriteToConsoleResult(item2.name + ", " + exception.ToString(), false);
                        allGreen = false;
                        allTestGreen = false;
                    }
                }
                finally
                {
                    // TearDown
                    foreach (var teardown in testGroup.TearDowns) teardown();
                }

                // UnityTearDown
                foreach (var teardown in testGroup.UnityTearDowns) yield return StartCoroutine(teardown());
            }

            // OnetimeTearDown
            foreach (var item in testGroup.OneTimeTearDowns) item();

            // GlobalTeardown
            if (!withoutGlobalSetup)
            {
                foreach (var item in this.testData.GlobalTearDowns) item();
            }

            AppendToGraphicText("[" + testGroup.Name + "]" + totalExecutionTime.Sum().ToString("0.00") + "ms\n\n");
            foreach (var btn in list.GetComponentsInChildren<Button>()) btn.interactable = true;
            if (self != null)
            {
                self.GetComponent<Image>().color = allGreen ? passColor : failColor;
            }

            yield return StartCoroutine(ScrollLogToEndNextFrame());
        }

        IEnumerator ExecuteAllInCoroutine(List<Func<Coroutine>> tests)
        {
            allTestGreen = true;

            foreach (var item in this.testData.GlobalSetups) item();

            foreach (var item in tests)
            {
                yield return item();
            }

            foreach (var item in this.testData.GlobalTearDowns) item();

            if (Application.isBatchMode)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                bool disableAutoClose = (scene.name.Contains("DisableAutoClose"));

                if (allTestGreen)
                {
                    WriteToConsole("Test Complete Successfully");
                    if (!disableAutoClose)
                    {
                        Application.Quit();
                    }
                }
                else
                {
                    WriteToConsole("Test Failed, please see [NG] log.");
                    if (!disableAutoClose)
                    {
                        Application.Quit(1);
                    }
                }
            }
        }

        IEnumerator UnwrapEnumerator(IEnumerator enumerator, Action<Exception> exceptionCallback)
        {
            var hasNext = true;
            while (hasNext)
            {
                try
                {
                    hasNext = enumerator.MoveNext();
                }
                catch (Exception ex)
                {
                    exceptionCallback(ex);
                    hasNext = false;
                }

                if (hasNext)
                {
                    // unwrap self for bug of Unity
                    // https://issuetracker.unity3d.com/issues/does-not-stop-coroutine-when-it-throws-exception-in-movenext-at-first-frame
                    var moreCoroutine = enumerator.Current as IEnumerator;
                    if (moreCoroutine != null)
                    {
                        yield return StartCoroutine(UnwrapEnumerator(moreCoroutine, ex =>
                        {
                            exceptionCallback(ex);
                            hasNext = false;
                        }));
                    }
                    else
                    {
                        yield return enumerator.Current;
                    }
                }
            }
        }

        static void WriteToConsole(string msg)
        {
            if (Application.isBatchMode)
            {
                Console.WriteLine(msg);
            }
        }

        void AppendToGraphicText(string msg)
        {
            if (!Application.isBatchMode)
            {
                if (logClear)
                {
                    logText.text = "";
                    logClear = false;
                }

                try
                {
                    logText.text += msg;
                }
                catch
                {
                    logClear = true;
                }
            }
        }

        static void WriteToConsoleResult(string msg, bool green)
        {
            if (Application.isBatchMode)
            {
                if (!green)
                {
                    var currentForeground = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[NG]");
                    Console.ForegroundColor = currentForeground;
                }
                else
                {
                    var currentForeground = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[OK]");
                    Console.ForegroundColor = currentForeground;
                }

                System.Console.WriteLine(msg);
            }
        }

        struct Pair
        {
            public string Name;
            public UnityAction Action;
        }
    }
}
