:: Generally this script isn't necessary, since git records the symlinks.
:: But when creating new links, one could update this script, or just create the sym link and record it in git.

pushd %~dp0

:: Create the directories within which links will be created
md src\MessagePack.UnityClient\Assets\Scripts\MessagePack\LZ4
md src\MessagePack.UnityClient\Assets\Scripts\MessagePack\Unity
md src\MessagePack.UnityClient\Assets\Scripts\MessagePack\UnsafeExtensions

:: Create the links and junctions themselves
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\Attributes.cs" "..\..\..\..\MessagePack.Annotations\Attributes.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\FloatBits.cs" "..\..\..\..\MessagePack\FloatBits.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\IFormatterResolver.cs" "..\..\..\..\MessagePack\IFormatterResolver.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\IMessagePackSerializationCallbackReceiver.cs" "..\..\..\..\MessagePack.Annotations\IMessagePackSerializationCallbackReceiver.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\MessagePackCode.cs" "..\..\..\..\MessagePack\MessagePackCode.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\MessagePackSerializer.cs" "..\..\..\..\MessagePack\MessagePackSerializer.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\MessagePackSerializer.Json.cs" "..\..\..\..\MessagePack\MessagePackSerializer.Json.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\MessagePackSerializer+NonGeneric.cs" "..\..\..\..\MessagePack\MessagePackSerializer+NonGeneric.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\MessagePackReader.cs" "..\..\..\..\MessagePack\MessagePackReader.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\MessagePackReader.Integers.cs" "..\..\..\..\MessagePack\MessagePackReader.Integers.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\MessagePackWriter.cs" "..\..\..\..\MessagePack\MessagePackWriter.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\BufferWriter.cs" "..\..\..\..\MessagePack\BufferWriter.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\SequencePool.cs" "..\..\..\..\MessagePack\SequencePool.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\SequenceReader.cs" "..\..\..\..\MessagePack\SequenceReader.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\SequenceReaderExtensions.cs" "..\..\..\..\MessagePack\SequenceReaderExtensions.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\StreamPolyfillExtensions.cs" "..\..\..\..\MessagePack\StreamPolyfillExtensions.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\ExtensionHeader.cs" "..\..\..\..\MessagePack\ExtensionHeader.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\ExtensionResult.cs" "..\..\..\..\MessagePack\ExtensionResult.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\Utilities.cs" "..\..\..\..\MessagePack\Utilities.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\Nil.cs" "..\..\..\..\MessagePack\Nil.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\StringEncoding.cs" "..\..\..\..\MessagePack\StringEncoding.cs"
mklink /D ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\Formatters" "..\..\..\..\MessagePack\Formatters"
mklink /D ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\Internal" "..\..\..\..\MessagePack\Internal"
mklink /D ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\Resolvers" "..\..\..\..\MessagePack\Resolvers"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\Unity\UnityResolver.cs" "..\..\..\..\..\MessagePack.UnityShims\UnityResolver.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\Unity\Formatters.cs" "..\..\..\..\..\MessagePack.UnityShims\Formatters.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\UnsafeExtensions\UnityBlitResolver.cs" "..\..\..\..\..\MessagePack.UnityShims\Extension\UnityBlitResolver.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\UnsafeExtensions\UnsafeBlitFormatter.cs" "..\..\..\..\..\MessagePack.UnityShims\Extension\UnsafeBlitFormatter.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\Tests\Class1.cs" "..\..\..\..\..\sandbox\SharedData\Class1.cs"
mklink /D ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\LZ4\Codec" "..\..\..\..\..\MessagePack\LZ4\Codec"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\LZ4\LZ4MessagePackSerializer.cs" "..\..\..\..\..\MessagePack\LZ4\LZ4MessagePackSerializer.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\LZ4\LZ4MessagePackSerializer.JSON.cs" "..\..\..\..\..\MessagePack\LZ4\LZ4MessagePackSerializer.JSON.cs"
mklink ".\src\MessagePack.UnityClient\Assets\Scripts\MessagePack\LZ4\LZ4MessagePackSerializer+NonGeneric.cs" "..\..\..\..\..\MessagePack\LZ4\LZ4MessagePackSerializer+NonGeneric.cs"

popd
