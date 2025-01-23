/*
 * This file is basically AssemblyInfo.cs, but with the option to add global suppressions,
 * or global constants.
*/
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SimpleCDN.Tests.Integration")]

#if DEBUG // only generate serializers for debug views in debug mode
[JsonSerializable(typeof(CacheConfiguration))]
[JsonSerializable(typeof(CDNConfiguration))]
[JsonSerializable(typeof(RedisCacheConfiguration))]
[JsonSerializable(typeof(InMemoryCacheConfiguration))]
internal partial class ExtraSourceGenerationContext : JsonSerializerContext;
#endif
