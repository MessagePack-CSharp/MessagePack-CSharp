# Analyzer

The MessagePackAnalyzer helps to:

1. Automate defining your serializable objects.
1. Produces compiler warnings due to incorrect attribute use, accessibility, and more.

![analyzergif](https://cloud.githubusercontent.com/assets/46207/23837445/ce734eae-07cb-11e7-9758-d69f0f095bc1.gif)

If you want to allow a specific type (for example, when registering a custom type), put `MessagePackAnalyzer.json` at the project root and make the Build Action to `AdditionalFiles`.

![image](https://cloud.githubusercontent.com/assets/46207/23837427/8a8d507c-07cb-11e7-9277-5a566eb0bfde.png)

This is a sample of the contents of `MessagePackAnalyzer.json`:

```json
[ "MyNamespace.FooClass", "MyNameSpace.BarStruct" ]
```
