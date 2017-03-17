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
            var localConfig = GetLocalConfigFromArgs(ref args);

            // This whole secion will be back to one line once a new version of BenchmarkDotNet with an optinal config
            // parameter of the BenchmarkSwitcher is released.

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
            config = ManualConfig.Union(config, localConfig);

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

        private static IConfig GetLocalConfigFromArgs(ref string[] args)
        {
            string[] exportBigQueryArgs = {"--export-bigquery", "-ebq"};
            if (args.Intersect(exportBigQueryArgs, StringComparer.OrdinalIgnoreCase).Any())
            {
                var commitId = GetArgValue(ref args, "--commit=", "");
                var googleProjectId = GetArgValue(ref args, "--project=", "nodatime-benchmarks-test");
                googleProjectId = GetArgValue(ref args, "--projectId=", googleProjectId);
                var datasetId = GetArgValue(ref args, "--dataset=", "NodaTimeBenchmarks");
                datasetId = GetArgValue(ref args, "--datasetId=", datasetId);
                args = args.Except(exportBigQueryArgs, StringComparer.OrdinalIgnoreCase).ToArray();
                return ManualConfig.CreateEmpty()
                    .With(new BigQueryExporter(commitId, googleProjectId, datasetId));
            }
            else
            {
                return DefaultConfig.Instance;
            }
        }

        private static string GetArgValue(ref string[] args, string argPrefix, string defaultValue)
        {
            string fullArg =
                args.FirstOrDefault((arg) => arg.StartsWith(argPrefix, StringComparison.OrdinalIgnoreCase));
            if (fullArg != null)
            {
                args = args.Except(new[] {fullArg}).ToArray();
                return fullArg.Substring(argPrefix.Length).Trim().Trim('"');
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
