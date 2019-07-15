# MessagePack for C# (.NET, .NET Core, Unity, Xamarin)

[![NuGet](https://img.shields.io/nuget/v/MessagePack.svg)](https://www.nuget.org/packages/messagepack)
[![NuGet](https://img.shields.io/nuget/vpre/MessagePack.svg)](https://www.nuget.org/packages/messagepack)
[![Releases](https://img.shields.io/github/release/neuecc/MessagePack-CSharp.svg)](https://github.com/neuecc/MessagePack-CSharp/releases)

[![Join the chat at https://gitter.im/MessagePack-CSharp/Lobby](https://badges.gitter.im/MessagePack-CSharp/Lobby.svg)](https://gitter.im/MessagePack-CSharp/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build Status](https://dev.azure.com/ils0086/MessagePack-CSharp/_apis/build/status/MessagePack-CSharp-CI)](https://dev.azure.com/ils0086/MessagePack-CSharp/_build/latest?definitionId=2)

The extremely fast [MessagePack](http://msgpack.org/) serializer for C#. It is 10x faster than [MsgPack-Cli](https://github.com/msgpack/msgpack-cli) and outperforms other C# serializers. MessagePack for C# also ships with built-in support for LZ4 compression - an extremely fast compression algorithm. Performance is important, particularly in applications like game development, distributed computing, microservice architecture, and caching.

![Perf comparison graph](https://cloud.githubusercontent.com/assets/46207/23835716/89c8ab08-07af-11e7-9183-9e9415bdc87f.png)

MessagePack has compact binary size and full set of general purpose expression. Please see the [comparison with JSON, protobuf, ZeroFormatter section](doc/comparison.md). Learn [why MessagePack C# is fastest](doc/performance.md).

## Getting started

1. [Install MessagePack in your project](doc/installation.md).
1. Check out our [quick start](doc/quickstart.md) tutorial.
1. Check out the rest of [our documentation](doc/index.md).

## Author Info

Yoshifumi Kawai(a.k.a. neuecc) is a software developer in Japan.
He is the Director/CTO at Grani, Inc.
Grani is a mobile game developer company in Japan and well known for using C#.
He is awarding Microsoft MVP for Visual C# since 2011.
He is known as the creator of [UniRx](http://github.com/neuecc/UniRx/)(Reactive Extensions for Unity)

* Blog: [https://medium.com/@neuecc](https://medium.com/@neuecc) (English)
* Blog: [http://neue.cc/](http://neue.cc/) (Japanese)
* Twitter: [https://twitter.com/neuecc](https://twitter.com/neuecc) (Japanese)
