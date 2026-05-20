; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 2.1.80

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MsgPack001 | Reliability | Disabled | MsgPack001SpecifyOptionsAnalyzer
MsgPack002 | Reliability | Disabled | MsgPack002UseConstantOptionsAnalyzer
MsgPack003 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack004 | Usage | Error | Member needs Key or IgnoreMember attribute
MsgPack005 | Usage | Error | MsgPack00xMessagePackAnalyzer

## Release 2.3.73-alpha

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MsgPack006 | Usage | Error | MsgPack00xMessagePackAnalyzer

## Release 2.6.95-alpha

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MsgPack007 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack008 | Usage | Error | MsgPack00xMessagePackAnalyzer

## Release 3.0.54-alpha

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MsgPack009 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack010 | Usage | Warning | Formatter is not accessible to the source generated resolver
MsgPack011 | Usage | Error | MsgPack00xMessagePackAnalyzer
MsgPack012 | Usage | Error | MsgPack00xMessagePackAnalyzer

## Release 3.0.129-beta

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MsgPack013 | Usage | Warning | Formatter has no accessible instance for the source generated resolver

## Release 3.0.208-rc.1

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MsgPack014 | Usage | Warning | Formatters of reference types should implement `IMessagePackFormatter<T?>`
MsgPack015 | Usage | Warning | MessagePackObjectAttribute.AllowPrivate should be set
MsgPack016 | Usage | Error | KeyAttribute-derived attributes are not supported by AOT formatters
MsgPack017 | Usage | Warning | Property with init accessor and initializer
MsgPack018 | Usage | Error | Unique names required in force map mode