using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace RuntimeUnitTestToolkit
{
    public class UnitTestData
    {
        // [SetupFixture][OneTimeSetup]
        readonly List<Action> globalSetups = new List<Action>();
        // [SetupFixture][OneTimeTearDown]
        readonly List<Action> globalTearDowns = new List<Action>();

        // Key:Type.FullName
        readonly Dictionary<string, TestGroup> testGroups = new Dictionary<string, TestGroup>();

        public IEnumerable<Action> GlobalSetups => globalSetups;
        public IEnumerable<Action> GlobalTearDowns => globalTearDowns;
        public IEnumerable<KeyValuePair<string, TestGroup>> TestGroups => testGroups;

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
                    SetUpFixtureAttribute setupFixture;
                    try
                    {
                        setupFixture = item.GetCustomAttribute<SetUpFixtureAttribute>(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("TestAttribute Load Fail, Assembly:" + assembly.FullName);
                        Debug.LogException(ex);
                        goto NEXT_ASSEMBLY;
                    }
                    if (setupFixture != null)
                    {
                        yield return item;
                        continue;
                    }

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

        static void RegisterAttributeAction<T>(MethodInfo info, object instance, List<Action> list)
            where T : Attribute
        {
            var attr = info.GetCustomAttribute<T>(true);
            if (attr != null)
            {
                list.Add((Action)Delegate.CreateDelegate(typeof(Action), instance, info));
            }
        }

        public static UnitTestData CreateFromAllAssemblies()
        {
            return new UnitTestData();
        }

        UnitTestData()
        {
            foreach (var testType in GetTestTargetTypes())
            {
                var setupFixture = testType.GetCustomAttribute<SetUpFixtureAttribute>(true);
                if (setupFixture != null)
                {
                    var instance = Activator.CreateInstance(testType);

                    var methods = testType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    foreach (var item in methods)
                    {
                        RegisterAttributeAction<OneTimeSetUpAttribute>(item, instance, globalSetups);
                        RegisterAttributeAction<OneTimeTearDownAttribute>(item, instance, globalTearDowns);
                    }
                    continue;
                }
                else
                {
                    var group = new TestGroup(testType);
                    testGroups[testType.FullName] = group;
                }
            }
        }
    }

    public class TestGroup
    {
        // [Test]
        List<(string name, Action test)> syncTests = new List<(string name, Action test)>();

        // [UnityTest]
        List<(string name, Func<IEnumerator> test)> asyncTests = new List<(string name, Func<IEnumerator> test)>();

        // [SetUp]
        List<Action> setups = new List<Action>();

        // [OneTimeSetUp]
        List<Action> onetimeSetups = new List<Action>();

        // [TearDown]
        List<Action> tearDowns = new List<Action>();

        // [OneTimeTearDown]
        List<Action> oneTimeTearDowns = new List<Action>();

        // [UnitySetUp]
        List<Func<IEnumerator>> unitySetUps = new List<Func<IEnumerator>>();

        // [UnityTearDown]
        List<Func<IEnumerator>> unityTearDowns = new List<Func<IEnumerator>>();

        public string Name { get; private set; }
        public IEnumerable<(string name, Action test)> SyncTests => syncTests;
        public IEnumerable<(string name, Func<IEnumerator> test)> AsyncTests => asyncTests;
        public IEnumerable<Action> Setups => setups;
        public IEnumerable<Action> OnetimeSetups => onetimeSetups;
        public IEnumerable<Action> TearDowns => tearDowns;
        public IEnumerable<Action> OneTimeTearDowns => oneTimeTearDowns;
        public IEnumerable<Func<IEnumerator>> UnitySetUps => unitySetUps;
        public IEnumerable<Func<IEnumerator>> UnityTearDowns => unityTearDowns;

        public IEnumerable<(string name, object test)> Tests
        {
            get
            {
                foreach (var item in syncTests) yield return (item.name, item.test);
                foreach (var item in asyncTests) yield return (item.name, item.test);
            }
        }

        static void RegisterAttributeAction<T>(MethodInfo info, object instance, List<Action> list)
            where T : Attribute
        {
            var attr = info.GetCustomAttribute<T>(true);
            if (attr != null)
            {
                list.Add((Action)Delegate.CreateDelegate(typeof(Action), instance, info));
            }
        }

        static void RegisterAttributeAction<T>(MethodInfo info, object instance, List<Func<IEnumerator>> list)
            where T : Attribute
        {
            var attr = info.GetCustomAttribute<T>(true);
            if (attr != null)
            {
                list.Add((Func<IEnumerator>)Delegate.CreateDelegate(typeof(Func<IEnumerator>), instance, info));
            }
        }

        public TestGroup(Type testType)
        {
            this.Name = testType.Name; // not FullName

            var test = Activator.CreateInstance(testType);
            var methods = testType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            foreach (var item in methods)
            {
                try
                {
                    RegisterAttributeAction<NUnit.Framework.SetUpAttribute>(item, test, setups);
                    RegisterAttributeAction<NUnit.Framework.TearDownAttribute>(item, test, tearDowns);
                    RegisterAttributeAction<OneTimeSetUpAttribute>(item, test, onetimeSetups);
                    RegisterAttributeAction<OneTimeTearDownAttribute>(item, test, oneTimeTearDowns);
                    RegisterAttributeAction<UnitySetUpAttribute>(item, test, unitySetUps);
                    RegisterAttributeAction<UnityTearDownAttribute>(item, test, unityTearDowns);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(testType.Name + "." + item.Name + " failed to register setup/teardown method, exception: " + e.ToString());
                }

                try
                {
                    var iteratorTest = item.GetCustomAttribute<UnityEngine.TestTools.UnityTestAttribute>(true);
                    if (iteratorTest != null)
                    {
                        if (item.GetParameters().Length == 0 && item.ReturnType == typeof(IEnumerator))
                        {
                            var factory = (Func<IEnumerator>)Delegate.CreateDelegate(typeof(Func<IEnumerator>), test, item);
                            asyncTests.Add((factory.Method.Name, factory));
                        }
                        else
                        {
                            var testData = GetTestData(item);
                            if (testData.Count != 0)
                            {
                                foreach (var item2 in testData)
                                {
                                    Func<IEnumerator> factory;
                                    if (item.IsGenericMethod)
                                    {
                                        var method2 = InferGenericType(item, item2);
                                        factory = () => (IEnumerator)method2.Invoke(test, item2);
                                    }
                                    else
                                    {
                                        factory = () => (IEnumerator)item.Invoke(test, item2);
                                    }
                                    var name = item.Name + "(" + string.Join(", ", item2.Select(x => x?.ToString() ?? "null")) + ")";
                                    name = name.Replace(Char.MinValue, ' ').Replace(Char.MaxValue, ' ').Replace("<", "[").Replace(">", "]");
                                    asyncTests.Add((name, factory));
                                }
                            }
                            else
                            {
                                UnityEngine.Debug.Log(testType.Name + "." + item.Name + " currently does not supported in RuntumeUnitTestToolkit(multiple parameter without TestCase or return type is invalid).");
                            }
                        }
                    }

                    var standardTest = item.GetCustomAttribute<NUnit.Framework.TestAttribute>(true);
                    if (standardTest != null)
                    {
                        if (item.GetParameters().Length == 0 && item.ReturnType == typeof(void))
                        {
                            var invoke = (Action)Delegate.CreateDelegate(typeof(Action), test, item);
                            syncTests.Add((invoke.Method.Name, invoke));
                        }
                        else
                        {
                            var testData = GetTestData(item);
                            if (testData.Count != 0)
                            {
                                foreach (var item2 in testData)
                                {
                                    Action invoke = null;
                                    if (item.IsGenericMethod)
                                    {
                                        var method2 = InferGenericType(item, item2);
                                        invoke = () => method2.Invoke(test, item2);
                                    }
                                    else
                                    {
                                        invoke = () => item.Invoke(test, item2);
                                    }
                                    var name = item.Name + "(" + string.Join(", ", item2.Select(x => x?.ToString() ?? "null")) + ")";
                                    name = name.Replace(Char.MinValue, ' ').Replace(Char.MaxValue, ' ').Replace("<", "[").Replace(">", "]");
                                    syncTests.Add((name, invoke));
                                }
                            }
                            else
                            {
                                UnityEngine.Debug.Log(testType.Name + "." + item.Name + " currently does not supported in RuntumeUnitTestToolkit(multiple parameter without TestCase or return type is invalid).");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(testType.Name + "." + item.Name + " failed to register method, exception: " + e.ToString());
                }
            }
        }

        List<object[]> GetTestData(MethodInfo methodInfo)
        {
            List<object[]> testCases = new List<object[]>();

            var inlineData = methodInfo.GetCustomAttributes<NUnit.Framework.TestCaseAttribute>(true);
            foreach (var item in inlineData)
            {
                testCases.Add(item.Arguments);
            }

            var sourceData = methodInfo.GetCustomAttributes<NUnit.Framework.TestCaseSourceAttribute>(true);
            foreach (var item in sourceData)
            {
                var enumerator = GetTestCaseSource(methodInfo, item.SourceType, item.SourceName, item.MethodParams);
                foreach (var item2 in enumerator)
                {
                    var item3 = item2 as IEnumerable; // object[][]
                    if (item3 != null)
                    {
                        var l = new List<object>();
                        foreach (var item4 in item3)
                        {
                            l.Add(item4);
                        }
                        testCases.Add(l.ToArray());
                    }
                }
            }

            return testCases;
        }

        IEnumerable GetTestCaseSource(MethodInfo method, Type sourceType, string sourceName, object[] methodParams)
        {
            Type type = sourceType ?? method.DeclaringType;

            MemberInfo[] member = type.GetMember(sourceName, BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (member.Length == 1)
            {
                MemberInfo memberInfo = member[0];
                FieldInfo fieldInfo = memberInfo as FieldInfo;
                if ((object)fieldInfo != null)
                {
                    return (!fieldInfo.IsStatic) ? ReturnErrorAsParameter("The sourceName specified on a TestCaseSourceAttribute must refer to a static field, property or method.") : ((methodParams == null) ? ((IEnumerable)fieldInfo.GetValue(null)) : ReturnErrorAsParameter("You have specified a data source field but also given a set of parameters. Fields cannot take parameters, please revise the 3rd parameter passed to the TestCaseSourceAttribute and either remove it or specify a method."));
                }
                PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                if ((object)propertyInfo != null)
                {
                    return (!propertyInfo.GetGetMethod(nonPublic: true).IsStatic) ? ReturnErrorAsParameter("The sourceName specified on a TestCaseSourceAttribute must refer to a static field, property or method.") : ((methodParams == null) ? ((IEnumerable)propertyInfo.GetValue(null, null)) : ReturnErrorAsParameter("You have specified a data source property but also given a set of parameters. Properties cannot take parameters, please revise the 3rd parameter passed to the TestCaseSource attribute and either remove it or specify a method."));
                }
                MethodInfo methodInfo = memberInfo as MethodInfo;
                if ((object)methodInfo != null)
                {
                    return (!methodInfo.IsStatic) ? ReturnErrorAsParameter("The sourceName specified on a TestCaseSourceAttribute must refer to a static field, property or method.") : ((methodParams == null || methodInfo.GetParameters().Length == methodParams.Length) ? ((IEnumerable)methodInfo.Invoke(null, methodParams)) : ReturnErrorAsParameter("You have given the wrong number of arguments to the method in the TestCaseSourceAttribute, please check the number of parameters passed in the object is correct in the 3rd parameter for the TestCaseSourceAttribute and this matches the number of parameters in the target method and try again."));
                }
            }
            return null;
        }

        MethodInfo InferGenericType(MethodInfo methodInfo, object[] parameters)
        {
            var set = new HashSet<Type>();
            List<Type> genericParameters = new List<Type>();
            foreach (var item in methodInfo.GetParameters()
                .Select((x, i) => new { x.ParameterType, i })
                .Where(x => x.ParameterType.IsGenericParameter)
                .OrderBy(x => x.ParameterType.GenericParameterPosition))
            {
                if (set.Add(item.ParameterType)) // DistinctBy
                {
                    genericParameters.Add(parameters[item.i].GetType());
                }
            }

            return methodInfo.MakeGenericMethod(genericParameters.ToArray());
        }

        IEnumerable ReturnErrorAsParameter(string name)
        {
            throw new Exception(name);
        }
    }
}