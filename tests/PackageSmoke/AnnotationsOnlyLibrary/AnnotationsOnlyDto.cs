using MessagePack;

namespace AnnotationsOnlyLibrary
{
    // compiles against the shim's forwarders; the generator that the shim's dependency brings in produces its formatter
    [MessagePackObject]
    public class AnnotationsOnlyDto
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public string? Name { get; set; }
    }
}
