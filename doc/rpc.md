# RPC

MessagePack advocated [MessagePack RPC](https://github.com/msgpack-rpc/msgpack-rpc), but formulation is stopped and it is not widely used.

## MagicOnion

I've created gRPC based MessagePack HTTP/2 RPC streaming framework called [MagicOnion](https://github.com/neuecc/MagicOnion). gRPC usually communicates with Protocol Buffers using IDL. But MagicOnion uses MessagePack for C# and does not needs IDL. If communicates C# to C#, schemaless(C# classes as schema) is better than IDL.

## StreamJsonRpc

The StreamJsonRpc library is based on [JSON-RPC](https://www.jsonrpc.org/) and includes [a pluggable formatter architecture](https://github.com/microsoft/vs-streamjsonrpc/blob/master/doc/extensibility.md#alternative-formatters) and includes [a sample MessagePack plugin](https://github.com/microsoft/vs-streamjsonrpc/blob/master/src/StreamJsonRpc.Tests/MessagePackFormatter.cs).
