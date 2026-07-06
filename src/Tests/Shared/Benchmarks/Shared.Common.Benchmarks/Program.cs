// ----------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Shared.Common.Benchmarks;

internal static class Program
{
    private static void Main()
    {
        BenchmarkRunner.Run<ExpressionCombinationBenchmark>(
            DefaultConfig.Instance.WithOptions(ConfigOptions.JoinSummary));
    }
}