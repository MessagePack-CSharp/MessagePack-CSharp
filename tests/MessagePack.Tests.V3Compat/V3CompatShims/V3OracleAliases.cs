// The ported v3 tests hand-author msgpack payloads with MessagePackWriter and pick results
// apart with MessagePackReader. v4's types of those names have a different API, but the repo
// already carries the REAL v3.1.8 runtime as an oracle assembly (MessagePackV3, extern alias V3) —
// so alias the v3 types back to their unqualified names. This is not a workaround but a
// feature: payloads WRITTEN by genuine v3 get handed to v4 for deserialization (and bytes
// produced by v4 get validated by the genuine v3 reader), which is exactly what a
// compatibility suite should exercise.
extern alias V3;

global using ExtensionHeader = V3::MessagePack.ExtensionHeader;
global using ExtensionResult = V3::MessagePack.ExtensionResult;
global using MessagePackReader = V3::MessagePack.MessagePackReader;
global using MessagePackWriter = V3::MessagePack.MessagePackWriter;
global using ReservedExtensionTypeCodes = V3::MessagePack.ReservedExtensionTypeCodes;
global using SequencePool = V3::MessagePack.SequencePool;

// Inside namespace MessagePack.Tests a v4 type of the same name shadows a compilation-unit
// alias, so these two only reach the files outside that namespace. v4 declares
// MessagePack.MessagePackReader and MessagePack.MessagePackWriter, so every file inside it
// that uses them repeats the two aliases within its namespace block.
//
// MessagePackType is NOT aliased: v4 has its own (same values). Tests that read v3's
// reader.NextMessagePackType name V3::MessagePack.MessagePackType explicitly.
