﻿/*
 * This file is basically AssemblyInfo.cs, but with the option to add global suppressions,
 * or global constants.
*/
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimpleCDN.Configuration;
using SimpleCDN.Extensions.Redis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("SimpleCDN.Tests.Integration")]

[JsonSerializable(typeof(CacheConfiguration))]
[JsonSerializable(typeof(CDNConfiguration))]
[JsonSerializable(typeof(RedisCacheConfiguration))]
[JsonSerializable(typeof(InMemoryCacheConfiguration))]
[JsonSerializable(typeof(HealthReport))]
internal partial class ExtraSourceGenerationContext : JsonSerializerContext;
