// Copyright 2009 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNetBigQuery;

namespace NodaTime.Benchmarks
{
    /// <summary>
    /// Entry point for benchmarking.
    /// </summary>
    internal class Program
    {
        // Run it with args = { "*" } for choosing all of target benchmarks
        // ReSharper disable once UnusedMember.Local
        private static void Main(string[] args)
        {
            var bigQueryConfig = new BigQueryConfig("someCommit", "nodatime-benchmarks-test", "NodaTimeBenchmarks");
            // Reflection to call internal methods.
            MethodInfo readArgumentList =
                typeof(TypeParser).GetMethod("ReadArgumentList", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo matchingTypesWithMethods =
                typeof(TypeParser).GetMethod(
                    "MatchingTypesWithMethods", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo logTotalTime =
                typeof(BenchmarkRunnerCore).GetMethod("LogTotalTime", BindingFlags.NonPublic | BindingFlags.Static);

            Assembly assembly = typeof(Program).GetTypeInfo().Assembly;
            ILogger logger = new ConsoleLogger();
            Type[] types =
                assembly
                    .GetTypes()
                    .Where(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .Any(m => m.GetCustomAttributes(true).OfType<BenchmarkAttribute>().Any()))
                    .Where(t => !t.GetTypeInfo().IsGenericType)
                    .OrderBy(t => t.Namespace)
                    .ThenBy(t => t.Name)
                    .ToArray();
            var typeParser = new TypeParser(types, logger);

            args = (string[]) readArgumentList.Invoke(typeParser, new object[]{args ?? new string[0]});
            var globalChronometer = Chronometer.Start();

            var config = ManualConfig.Union(DefaultConfig.Instance, ManualConfig.Parse(args));
            config = ManualConfig.Union(config, bigQueryConfig);

            var methods =
                (IEnumerable<TypeParser.TypeWithMethods>) matchingTypesWithMethods.Invoke(
                    typeParser, new object[] { args});
            foreach (var typeWithMethods in methods)
            {
                logger.WriteLineHeader("Target type: " + typeWithMethods.Type.Name);
                if (typeWithMethods.AllMethodsInType)
                    BenchmarkRunner.Run(typeWithMethods.Type, config);
                else
                    BenchmarkRunner.Run(typeWithMethods.Type, typeWithMethods.Methods, config);
                logger.WriteLine();
            }

            var clockSpan = globalChronometer.Stop();
            logTotalTime.Invoke(null, new object[] {logger, clockSpan.GetTimeSpan(), "Global total time"});
        }
    }
}
