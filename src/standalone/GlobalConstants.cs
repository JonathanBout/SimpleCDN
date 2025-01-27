/*
 * This file is basically AssemblyInfo.cs, but with the option to add global suppressions,
 * or globally accessed classes, constants, etc.
*/
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimpleCDN.Configuration;
using SimpleCDN.Extensions.Redis;
using SimpleCDN.Standalone;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("SimpleCDN.Tests.Integration")]

[JsonSourceGenerationOptions(Converters = [typeof(JsonStringEnumConverter<HealthStatus>)])]
[JsonSerializable(typeof(CacheConfiguration))]
[JsonSerializable(typeof(CDNConfiguration))]
[JsonSerializable(typeof(RedisCacheConfiguration))]
[JsonSerializable(typeof(InMemoryCacheConfiguration))]
[JsonSerializable(typeof(CustomHealthReport))]
internal partial class ExtraSourceGenerationContext : JsonSerializerContext;
