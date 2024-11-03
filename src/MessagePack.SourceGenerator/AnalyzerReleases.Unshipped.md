; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MsgPack001 | Reliability | Disabled | MsgPack001SpecifyOptionsAnalyzer
MsgPack002 | Reliability | Disabled | MsgPack002UseConstantOptionsAnalyzer
MsgPack003 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack004 | Usage | Error | Member needs Key or IgnoreMember attribute
MsgPack005 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack006 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack007 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack008 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack009 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack010 | Usage | Warning | Formatter is not accessible to the source generated resolver
MsgPack011 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack012 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack013 | Usage | Warning | Formatter has no accessible instance for the source generated resolver
MsgPack014 | Usage | Warning | Formatters of reference types should implement `IMessagePackFormatter<T?>`
MsgPack015 | Usage | Warning | MessagePackObjectAttribute.AllowPrivate should be set
MsgPack016 | Usage | Error | KeyAttribute-derived attributes are not supported by AOT formatters
MsgPack017 | Usage | Warning | Property with init accessor and initializer
MsgPack018 | Usage | Error | Unique names required in force map mode
