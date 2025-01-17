# SimpleCDN
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/JonathanBout/SimpleCDN/dotnet.yml?style=flat-square&logo=.net&label=tests&labelColor=%23512BD4&link=https%3A%2F%2Fgithub.com%2FJonathanBout%2FSimpleCDN%2Factions%2Fworkflows%2Fdotnet.yml)


SimpleCDN is, well, a simple CDN server. Built with relatively high r/w latency in mind (think NAS mount), it provides efficiënt ways to cache files, either using the built-in in-memory cache, or the Redis extension.

> [!WARNING]  
> **While Redis support is available, it is not very stable**, especially in high-load scenario's (tens of requests per second). By implementing a custom connection manager,
> it's brought down to a minimum but failures still happen. In such cases, SimpleCDN will load the data from disk instead of using the cache.

## How to use this in an existing project?
### NuGet Packages
SimpleCDN is available on NuGet:
- [![SimpleCDN on NuGet](https://img.shields.io/nuget/v/SimpleCDN?style=flat-square&logo=nuget&label=SimpleCDN&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FSimpleCDN)](https://NuGet.org/packages/SimpleCDN)
- [![SimpleCDN.Extensions.Redis on NuGet](https://img.shields.io/nuget/v/SimpleCDN?style=flat-square&logo=redis&label=SimpleCDN.Extensions.Redis&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FSimpleCDN.Extensions.Redis)](https://nuget.org/packages/SimpleCDN.Extensions.Redis)

## How to use SimpleCDN Standalone
### Using Docker

**Tags:**
- `latest`: the latest stable release, useful for quickly testing SimpleCDN.
- `main`: the latest build of the main branch, usually on the last commit. Not recommended for anything as it may contain bugs or break.
- `X.X[.X]`: pin to a specific minor or patch version. This provides higher precision and is recommended for production scenarios, especially with multi-instance environments. Supported versions can be found in the [tags listing](https://github.com/JonathanBout/SimpleCDN/tags). Note that the docker tag does not have the `v` prefix, so Git tag `v0.7.1` is Docker tag `0.7` or `0.7.1`.

#### with `docker run`
```
docker run -p "<your_port>:8080" -v "<your_cdn_data>:/data:ro" ghcr.io/jonathanbout/simplecdn
```
This will pull and run the latest stable build of SimpleCDN.

#### with `docker compose`
```yml
services:
  server:
    image: ghcr.io/jonathanbout/simplecdn
    volumes:
    - <your_cdn_data>:/data:ro # :ro to make the bind mount read-only
    ports:
    - <your_port>:8080

# === use below only if you want to use redis ===
    environment:
    - Cache__Redis__ConnectionString=redis:6379
  redis:
    image: redis
```

### Using dotnet
#### dotnet run
```
# PublishAOT is not supported with dotnet run so we need to disable it
dotnet run --property:PublishAot=false -- --CDN:DataRoot <your_cdn_data>
```

### Variables:

Generic:
| key | value type | default value | description |
|--|--|--|--|
| `CDN:DataRoot` | a local path | `/data` when using the Docker image, otherwise required. | The data root, where the files to be served are stored. |
| `CDN:MaxCachedItemSize` | A size in kB within your devices memory. | `8_000` | The maximum size of a file to be cached. If the size exceeds this value, the file is streamed directly from the disk. |
| `CDN:AllowDotFileAccess` | `true` or `false` | `false` | Whether to allow access to dotfiles and directories. |
| `CDN:ShowDotFiles` | `true` or `false` | `false` | Whether to show dotfiles in generated index files. When `AllowDotFileAccess` is `false`, `ShowDotFiles` is ignored. |
| `CDN:BlockRobots` | `true` or `false` | `true` | Whether to request robots to not index CDN files. Its still up to the robots to adhere to this rule. |
| `CDN:Footer` | Any HTML | `Powered by SimpleCDN` (with a link to this GitHub repo) | The text to place at the bottom of generated index files |
| `CDN:PageTitle` | Any <title> compatible string | `SimpleCDN` | The text to display in the browser's title bar |

Caching:
| key | value type | default value | description |
|--|--|--|--|
| `Cache:MaxAge` | A time in minutes | `60` | How long an item may be stale (read nor written) before being removed. |
| `Cache:Type` | `InMemory`, `Redis` or `Disabled` | `InMemory`, or if Redis has been configured, `Redis` | What cache provider to use, if any |
| **In-Memory Options** |
| `Cache:InMemory:MaxSize` | A size in kB | `500_000` | How big the cache may grow. When an entry is added, the oldest entries will be removed until this limit is met. |
| `Cache:InMemory:PurgeInterval` | A time in minutes | `5` | How often the purge loop should wake up, to remove stale items older than `Cache:MaxAge` |
| **Redis Options** |
| `Cache:Redis:ConnectionString` | A redis connection string` | None. Required when using Redis | How to connect to your Redis instance |
| `Cache:Redis:ClientName` | A string, without spaces | `SimpleCDN` | How this client should be identified to Redis. |
| `Cache:Redis:KeyPrefix` | A string | `SimpleCDN` | A string to prepend to Redis entry keys. |

#### Overriding the defaults:
- With an environment variable, e.g. `CDN__DataRoot=/mnt/data`
- With an appsettings.json file, e.g.
```json
{
  "CDN": {
    "ShowDotFiles": false
  }
}
```
- with a command line argument, e.g. `--CDN:MaxCachedItemSize 10000`

> [!NOTE]  
> Command line arguments have precedence over appsettings.json and appsettings.json has precedence environment variables.
 
## Development

Contributions are always welcome! Feel free to create an issue if you encounter problems. If you know a fix, a Pull Request is even better!

If you want to build a custom caching provider, take a look at the
[extensions/README.md file](https://github.com/jonathanbout/simplecdn/tree/main/extensions/README.md).

### Building the docker image
Building a docker image can be done easily with `docker build`:
```
docker build . -f SimpleCDN/Dockerfile -t simplecdn:local
```
Be aware the build context has to be the root of the repo, whilst the dockerfile is in the SimpleCDN folder.

### Running tests
Executing the Unit tests can be done with just a single command:
```
dotnet test SimpleCDN.sln
```
This will run the NUnit tests in the SimpleCDN.Tests project.
