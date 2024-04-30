
using MsgPack = global::MessagePack;


partial class MyResolver : MsgPack::IFormatterResolver
{
	public static readonly MyResolver Instance = new MyResolver();

	private static readonly MsgPack::IFormatterResolver[] ResolverList = new MsgPack::IFormatterResolver[]
	{
		global::MessagePack.Resolvers.NativeGuidResolver.Instance,
		new global::ResolverWithCtor(),
	};

	private MyResolver() { }

	public MsgPack::Formatters.IMessagePackFormatter<T> GetFormatter<T>()
	{
		return FormatterCache<T>.Formatter;
	}

	static class FormatterCache<T>
	{
		internal static readonly MsgPack::Formatters.IMessagePackFormatter<T> Formatter;

		static FormatterCache()
		{
			foreach (var resolver in ResolverList)
			{
				var f = resolver.GetFormatter<T>();
				if (f != null)
				{
					Formatter = f;
					return;
				}
			}
		}
	}
}

