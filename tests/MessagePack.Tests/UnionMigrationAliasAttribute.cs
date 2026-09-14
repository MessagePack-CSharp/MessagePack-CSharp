// The CS0104-dodging migration alias: a project that must keep compiling against both
// generations can declare its own [Union] NAMED like v3's (so annotation sites survive a
// find-and-replace-free migration) but deriving from v4's [UnionTag]. ReflectionUnionFormatter's
// name-first duck read meets the v3 name WITHOUT a Key property and must fall through to the
// base chain's v4 read instead of throwing. Exercised by ReflectionUnionTests.
namespace MessagePack;

internal sealed class UnionAttribute : UnionTagAttribute
{
    public UnionAttribute(int key, Type subType)
        : base(subType, key)
    {
    }
}
