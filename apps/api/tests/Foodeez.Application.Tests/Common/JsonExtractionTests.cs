using System.Text.Json;
using FluentAssertions;
using Foodeez.Application.Common;
using Xunit;

namespace Foodeez.Application.Tests.Common;

/// <summary>
/// JsonExtraction is the correct JSON reader in this codebase, and it is about to become the
/// only one: all five AI providers currently carry a private copy that uses
/// IndexOf('{')..LastIndexOf('}'), the exact approach this class's own comment explains is
/// wrong. NutritionEstimation has a third, duplicate implementation of the balanced scanner.
///
/// These pin the behaviours that make it the right one to keep, so Phase 3 can delete the
/// others and show the result is a strict improvement rather than merely a smaller diff.
/// </summary>
public class JsonExtractionTests
{
    [Fact]
    public void ReadObject_PlainObject_IsParsed()
    {
        var result = JsonExtraction.ReadObject("""{"score":80}""");

        result.Should().NotBeNull();
        result!.Value.GetProperty("score").GetInt32().Should().Be(80);
    }

    [Fact]
    public void ReadObject_ProseBeforeTheJson_IsIgnored()
    {
        // Every prompt asks for a few sentences first, so this is the normal case, not an edge.
        var text = "Your day looks balanced overall.\n\n{\"score\":72}";

        JsonExtraction.ReadObject(text)!.Value.GetProperty("score").GetInt32().Should().Be(72);
    }

    [Fact]
    public void ReadObject_ClosingRemarkAfterTheJson_DoesNotSwallowABrace()
    {
        // The naive LastIndexOf('}') takes the brace out of the trailing sentence and fails.
        var text = "{\"score\":65} Hope that helps! Let me know if you want {something else}.";

        var result = JsonExtraction.ReadObject(text);

        result.Should().NotBeNull();
        result!.Value.GetProperty("score").GetInt32().Should().Be(65);
    }

    [Fact]
    public void ReadObject_NestedObjects_ReturnsTheOutermost()
    {
        var text = """{"outer":{"inner":{"value":3}},"score":9}""";

        var result = JsonExtraction.ReadObject(text)!.Value;

        result.GetProperty("score").GetInt32().Should().Be(9);
        result.GetProperty("outer").GetProperty("inner").GetProperty("value").GetInt32().Should().Be(3);
    }

    [Fact]
    public void ReadObject_BracesInsideStringLiterals_DoNotAffectNesting()
    {
        var text = """{"note":"use a {mould} or a }tin{","score":50}""";

        var result = JsonExtraction.ReadObject(text);

        result.Should().NotBeNull();
        result!.Value.GetProperty("score").GetInt32().Should().Be(50);
    }

    [Fact]
    public void ReadObject_EscapedQuoteInsideAString_IsNotTreatedAsTheStringEnd()
    {
        var text = """{"note":"a \" then a } brace","score":41}""";

        JsonExtraction.ReadObject(text)!.Value.GetProperty("score").GetInt32().Should().Be(41);
    }

    [Fact]
    public void ReadObject_TruncatedResponse_ReturnsNullRatherThanAPartialParse()
    {
        // A response cut off by a token limit never closes its object. Reporting absence is
        // what lets the caller say "the provider failed" instead of saving half a result.
        var text = """{"days":[{"date":"2025-01-01","meals":[{"recipeName":"Oat""";

        JsonExtraction.ReadObject(text).Should().BeNull();
    }

    [Fact]
    public void ReadObject_NoJsonAtAll_ReturnsNull()
    {
        JsonExtraction.ReadObject("I'm sorry, I can't help with that.").Should().BeNull();
        JsonExtraction.ReadObject(string.Empty).Should().BeNull();
    }

    [Fact]
    public void ReadObject_MalformedJson_ReturnsNullInsteadOfThrowing()
    {
        JsonExtraction.ReadObject("{not valid json at all}").Should().BeNull();
    }

    [Fact]
    public void ReadString_MissingOrWrongType_ReturnsEmpty()
    {
        var element = Parse("""{"a":"text","b":5,"c":null}""");

        JsonExtraction.ReadString(element, "a").Should().Be("text");
        JsonExtraction.ReadString(element, "b").Should().BeEmpty();
        JsonExtraction.ReadString(element, "c").Should().BeEmpty();
        JsonExtraction.ReadString(element, "missing").Should().BeEmpty();
    }

    [Fact]
    public void ReadInt_ReadsNumbersAndQuotedNumbers()
    {
        var element = Parse("""{"a":7,"b":"7"}""");

        JsonExtraction.ReadInt(element, "a").Should().Be(7);
        JsonExtraction.ReadInt(element, "b").Should().Be(7, because: "models do quote numbers");
    }

    [Fact]
    public void ReadInt_MissingOrUnusable_ReturnsTheFallback()
    {
        var element = Parse("""{"c":null,"d":"lots","e":true,"f":[]}""");

        JsonExtraction.ReadInt(element, "missing", 3).Should().Be(3);
        JsonExtraction.ReadInt(element, "c", 3).Should().Be(3);
        JsonExtraction.ReadInt(element, "d", 3).Should().Be(3);
        JsonExtraction.ReadInt(element, "e", 3).Should().Be(3);
        JsonExtraction.ReadInt(element, "f", 3).Should().Be(3);
    }

    [Fact]
    public void ReadFloat_ReadsNumbersAndQuotedNumbers()
    {
        var element = Parse("""{"a":7.5,"b":"7.5"}""");

        JsonExtraction.ReadFloat(element, "a").Should().Be(7.5f);
        JsonExtraction.ReadFloat(element, "b").Should().Be(7.5f);
    }

    [Fact]
    public void ReadFloat_MissingOrUnusable_ReturnsTheFallback()
    {
        var element = Parse("""{"c":null,"d":"12 g","e":false}""");

        JsonExtraction.ReadFloat(element, "missing", 1.5f).Should().Be(1.5f);
        JsonExtraction.ReadFloat(element, "c", 1.5f).Should().Be(1.5f);
        JsonExtraction.ReadFloat(element, "e", 1.5f).Should().Be(1.5f);

        // A number with a unit attached is not read. Left as a fallback deliberately: guessing
        // which part is the number risks turning "12 g" into a silently wrong 12 of something.
        JsonExtraction.ReadFloat(element, "d", 1.5f).Should().Be(1.5f);
    }

    [Fact]
    public void ReadNumbers_NeverThrow_WhateverTheModelEmitted()
    {
        // TryGetInt32/TryGetSingle throw on a non-number rather than returning false, and
        // nothing above the parsers catches it - so one quoted or null figure used to discard
        // an entire meal plan, analysis or parsed photo.
        var element = Parse("""{"n":null,"s":"text","b":true,"a":[],"o":{}}""");

        foreach (var property in new[] { "n", "s", "b", "a", "o", "missing" })
        {
            var readInt = () => JsonExtraction.ReadInt(element, property);
            var readFloat = () => JsonExtraction.ReadFloat(element, property);

            readInt.Should().NotThrow(because: $"'{property}' must not take down the whole response");
            readFloat.Should().NotThrow(because: $"'{property}' must not take down the whole response");
        }
    }

    [Fact]
    public void ReadStrings_ReturnsOnlyNonEmptyStrings()
    {
        var element = Parse("""{"tags":["one","",null,"two",3]}""");

        JsonExtraction.ReadStrings(element, "tags").Should().Equal("one", "two");
    }

    [Fact]
    public void ReadStrings_MissingOrNotAnArray_ReturnsEmptyList()
    {
        var element = Parse("""{"tags":"not an array"}""");

        JsonExtraction.ReadStrings(element, "tags").Should().BeEmpty();
        JsonExtraction.ReadStrings(element, "missing").Should().BeEmpty();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
