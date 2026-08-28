using System;
using System.Globalization;

namespace MessagePack;

/// <summary>
/// Member-name-to-key conversion for <see cref="MessagePackObjectAttribute"/> string-key maps.
/// Each policy converts names exactly like its <c>System.Text.Json.JsonNamingPolicy</c> counterpart;
/// an explicit <c>[Key("name")]</c> on a member always wins over the policy.
/// </summary>
#if MESSAGEPACK_SOURCE_GENERATOR
// the linked copy inside the analyzer assembly must not leak into consumers that reference
// the generator project with ReferenceOutputAssembly=true (the analyzer tests do): a public
// twin would make every MessagePack.KeyNamingPolicy use ambiguous (CS0433)
internal enum KeyNamingPolicy
#else
public enum KeyNamingPolicy
#endif
{
    /// <summary>No conversion, the member name as-is — the <c>[MessagePackObject(true)]</c> behavior.</summary>
    None = 0,

    /// <summary>"TempCelsius" → "tempCelsius" (JsonNamingPolicy.CamelCase).</summary>
    CamelCase,

    /// <summary>"tempCelsius" → "TempCelsius", "XMLReader" → "XmlReader" (JsonNamingPolicy.PascalCase).</summary>
    PascalCase,

    /// <summary>"TempCelsius" → "temp_celsius" (JsonNamingPolicy.SnakeCaseLower).</summary>
    SnakeCaseLower,

    /// <summary>"TempCelsius" → "TEMP_CELSIUS" (JsonNamingPolicy.SnakeCaseUpper).</summary>
    SnakeCaseUpper,

    /// <summary>"TempCelsius" → "temp-celsius" (JsonNamingPolicy.KebabCaseLower).</summary>
    KebabCaseLower,

    /// <summary>"TempCelsius" → "TEMP-CELSIUS" (JsonNamingPolicy.KebabCaseUpper).</summary>
    KebabCaseUpper,
}

// 1:1 port of System.Text.Json's JsonCamelCaseNamingPolicy.FixCasing and
// JsonSeparatorNamingPolicy.ConvertNameCore (dotnet/runtime, MIT). Only the buffer management
// is simplified (plain array growth instead of stackalloc/ArrayPool: conversion runs once per
// type, at generation time or reflection table build); the character state machine is kept
// verbatim because the contract is byte-identical output to JsonNamingPolicy, and
// KeyNamingPolicyTests pins that against the real System.Text.Json as the oracle.
// Compiled into BOTH MessagePack (runtime reflection tier) and MessagePack.SourceGenerator
// (compile-time keys, linked source), so it must stay netstandard2.0-clean: no Span, no
// modern BCL surface.
internal static class KeyNamingPolicyConverter
{
    public static string ConvertName(KeyNamingPolicy policy, string name) => policy switch
    {
        KeyNamingPolicy.None => name,
        KeyNamingPolicy.CamelCase => ConvertCamelCase(name),
        KeyNamingPolicy.PascalCase => ConvertSeparator(name, separator: null, WordCasing.PascalCase),
        KeyNamingPolicy.SnakeCaseLower => ConvertSeparator(name, '_', WordCasing.LowerCase),
        KeyNamingPolicy.SnakeCaseUpper => ConvertSeparator(name, '_', WordCasing.UpperCase),
        KeyNamingPolicy.KebabCaseLower => ConvertSeparator(name, '-', WordCasing.LowerCase),
        KeyNamingPolicy.KebabCaseUpper => ConvertSeparator(name, '-', WordCasing.UpperCase),
        _ => throw new ArgumentOutOfRangeException(nameof(policy)),
    };

    static string ConvertCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
        {
            return name;
        }

        char[] chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
            {
                break;
            }

            bool hasNext = i + 1 < chars.Length;

            // stop when the next char is already lowercase
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
            {
                // if the next char is a space, lowercase the current char before exiting
                if (chars[i + 1] == ' ')
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                }

                break;
            }

            chars[i] = char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }

    enum SeparatorState
    {
        NotStarted,
        UppercaseLetter,
        LowercaseLetterOrDigit,
        SpaceSeparator,
    }

    enum WordCasing
    {
        LowerCase,
        UpperCase,
        PascalCase, // separator-less: word boundaries keep/uppercase their first letter, the rest lowercases
    }

    static string ConvertSeparator(string name, char? separator, WordCasing wordCasing)
    {
        var destination = new char[name.Length + (name.Length / 2) + 4];
        var state = SeparatorState.NotStarted;
        int charsWritten = 0;

        for (int i = 0; i < name.Length; i++)
        {
            // NB like the original, surrogate pair letters are not handled (dotnet/runtime#90352)
            char current = name[i];
            var category = char.GetUnicodeCategory(current);

            switch (category)
            {
                case UnicodeCategory.UppercaseLetter:
                    bool isWordBoundary = false;
                    switch (state)
                    {
                        case SeparatorState.NotStarted:
                            isWordBoundary = true;
                            break;

                        case SeparatorState.LowercaseLetterOrDigit:
                        case SeparatorState.SpaceSeparator:
                            // an uppercase letter after lowercase letters or spaces starts a new word
                            isWordBoundary = true;
                            if (separator.HasValue)
                            {
                                Write(ref destination, ref charsWritten, separator.Value);
                            }
                            break;

                        case SeparatorState.UppercaseLetter:
                            // uppercase runs group together except a final letter followed by
                            // lowercase: 'XMLReader' → 'xml_reader', but 'SHA512Hash' → 'sha512-hash'
                            if (i + 1 < name.Length && char.IsLower(name[i + 1]))
                            {
                                isWordBoundary = true;
                                if (separator.HasValue)
                                {
                                    Write(ref destination, ref charsWritten, separator.Value);
                                }
                            }
                            break;
                    }

                    current = wordCasing switch
                    {
                        WordCasing.LowerCase => char.ToLowerInvariant(current),
                        WordCasing.PascalCase => isWordBoundary ? current : char.ToLowerInvariant(current),
                        _ => current,
                    };

                    Write(ref destination, ref charsWritten, current);
                    state = SeparatorState.UppercaseLetter;
                    break;

                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    bool isWordStart = state is SeparatorState.SpaceSeparator or SeparatorState.NotStarted;

                    if (state == SeparatorState.SpaceSeparator)
                    {
                        // normalize preceding spaces to one separator
                        if (separator.HasValue)
                        {
                            Write(ref destination, ref charsWritten, separator.Value);
                        }
                    }

                    if (category == UnicodeCategory.LowercaseLetter)
                    {
                        current = wordCasing switch
                        {
                            WordCasing.UpperCase => char.ToUpperInvariant(current),
                            WordCasing.PascalCase => isWordStart ? char.ToUpperInvariant(current) : current,
                            _ => current,
                        };
                    }

                    Write(ref destination, ref charsWritten, current);
                    state = SeparatorState.LowercaseLetterOrDigit;
                    break;

                case UnicodeCategory.SpaceSeparator:
                    // spaces are trimmed at the edges and normalized to one separator between letters
                    if (state != SeparatorState.NotStarted)
                    {
                        state = SeparatorState.SpaceSeparator;
                    }
                    break;

                default:
                    // non-alphanumerics (the separator itself, surrogates, ...) pass through
                    // as-is and reset the state: 'ABC???def' → 'abc???def' in snake_case
                    Write(ref destination, ref charsWritten, current);
                    state = SeparatorState.NotStarted;
                    break;
            }
        }

        return new string(destination, 0, charsWritten);

        static void Write(ref char[] destination, ref int charsWritten, char value)
        {
            if (charsWritten == destination.Length)
            {
                Array.Resize(ref destination, destination.Length * 2);
            }

            destination[charsWritten++] = value;
        }
    }
}
