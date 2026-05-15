// ----------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Common.Extensions;
using Xunit;

namespace Shared.Common.Tests.Extensions;

/// <summary>
/// Набор тестов для класса расширения строк <see cref="StringExtensions"/>.
/// Проверяет корректность работы всех методов расширения строк,
/// включая граничные случаи и различные форматы входных данных.
/// </summary>
public sealed class StringExtensionsTests
{
    #region ToKebabCase Tests

    /// <summary>
    /// Проверяет конвертацию строки в kebab-case формат.
    /// </summary>
    [Theory]
    [InlineData("HelloWorld", "hello-world")]
    [InlineData("helloWorld", "hello-world")]
    [InlineData("Hello World", "hello-world")]
    [InlineData("XMLParser", "xml-parser")]
    [InlineData("IOStream", "io-stream")]
    [InlineData("already-kebab-case", "already-kebab-case")]
    [InlineData("SingleWord", "single-word")]
    [InlineData("ABC", "abc")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("Multiple   Spaces   Between", "multiple-spaces-between")]
    [InlineData("WithNumbers123Test", "with-numbers123-test")]
    [InlineData("HTMLDocumentReader", "html-document-reader")]
    [InlineData("getHTTPResponse", "get-http-response")]
    public void ToKebabCase_ValidInput_ConvertsToKebabCase(string input, string expected)
    {
        // Act
        var result = input.ToKebabCase();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ToSnakeCase Tests

    /// <summary>
    /// Проверяет конвертацию строки в snake_case формат.
    /// </summary>
    [Theory]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("helloWorld", "hello_world")]
    [InlineData("Hello World", "hello_world")]
    [InlineData("XMLParser", "xml_parser")]
    [InlineData("IOStream", "io_stream")]
    [InlineData("already_snake_case", "already_snake_case")]
    [InlineData("SingleWord", "single_word")]
    [InlineData("ABC", "abc")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("Multiple   Spaces   Between", "multiple_spaces_between")]
    [InlineData("WithNumbers123Test", "with_numbers123_test")]
    [InlineData("HTMLDocumentReader", "html_document_reader")]
    [InlineData("getHTTPResponse", "get_http_response")]
    public void ToSnakeCase_ValidInput_ConvertsToSnakeCase(string input, string expected)
    {
        // Act
        var result = input.ToSnakeCase();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ToCamelCase Tests

    /// <summary>
    /// Проверяет конвертацию строки в camelCase формат.
    /// </summary>
    [Theory]
    [InlineData("HelloWorld", "helloWorld")]
    [InlineData("helloWorld", "helloWorld")]
    [InlineData("Hello World", "hello World")]
    [InlineData("XMLParser", "xmlParser")]
    [InlineData("IOStream", "ioStream")]
    [InlineData("already_camel_case", "already_camel_case")]
    [InlineData("already-camel-case", "already-camel-case")]
    [InlineData("SingleWord", "singleWord")]
    [InlineData("ABC", "abc")]
    [InlineData("a", "a")]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("Multiple   Spaces   Between", "multiple   Spaces   Between")]
    [InlineData("WithNumbers123Test", "withNumbers123Test")]
    [InlineData("HTMLDocumentReader", "htmlDocumentReader")]
    [InlineData("getHTTPResponse", "getHTTPResponse")]
    public void ToCamelCase_ValidInput_ConvertsToCamelCase(string input, string expected)
    {
        // Act
        var result = input.ToCamelCase();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ToUpperFirstChar Tests

    /// <summary>
    /// Проверяет преобразование строки к виду с заглавной первой буквой.
    /// </summary>
    [Theory]
    [InlineData("hello", "Hello")]
    [InlineData("Hello", "Hello")]
    [InlineData("HELLO", "HELLO")]
    [InlineData("h", "H")]
    [InlineData("H", "H")]
    [InlineData("", "")]
    [InlineData("hello world", "Hello world")]
    [InlineData("123test", "123test")]
    [InlineData("!special", "!special")]
    public void ToUpperFirstChar_ValidInput_ConvertsFirstCharToUpper(string input, string expected)
    {
        // Act
        var result = input.ToUpperFirstChar();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region RemoveWhiteSpaces Tests

    /// <summary>
    /// Проверяет удаление всех пробельных символов из строки.
    /// </summary>
    [Theory]
    [InlineData("hello world", "helloworld")]
    [InlineData("  multiple   spaces  ", "multiplespaces")]
    [InlineData("no_spaces", "no_spaces")]
    [InlineData("", "")]
    [InlineData(null, null)]
    [InlineData("   ", "")]
    [InlineData("a b c d e f", "abcdef")]
    [InlineData("tab\there", "tabhere")]
    [InlineData("new\nline", "newline")]
    [InlineData("mixed \t\n whitespace", "mixedwhitespace")]
    [InlineData("123 456 789", "123456789")]
    [InlineData("Special! @# Characters", "Special!@#Characters")]
    public void RemoveWhiteSpaces_ValidInput_RemovesAllWhitespaceCharacters(string? input, string? expected)
    {
        // Act
        var result = input.RemoveWhiteSpaces();

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
