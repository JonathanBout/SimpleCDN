# SimpleCDN
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/JonathanBout/SimpleCDN/dotnet.yml?style=flat-square&logo=.net&label=tests&labelColor=%23512BD4&link=https%3A%2F%2Fgithub.com%2FJonathanBout%2FSimpleCDN%2Factions%2Fworkflows%2Fdotnet.yml)


SimpleCDN is, well, a simple CDN server. Currently it is only tested for single-instance use, but I don't see any reasons why a load balancer wouldn't work. With Redis you can make sure they have one single shared cache.

## How to use this in an existing project?
### NuGet Packages
SimpleCDN is available on NuGet:
- ![SimpleCDN on NuGet](https://img.shields.io/nuget/v/SimpleCDN?style=flat-square&logo=nuget&label=SimpleCDN&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FSimpleCDN)
- ![SimpleCDN.Extensions.Redis on NuGet](https://img.shields.io/nuget/v/SimpleCDN?style=flat-square&logo=redis&label=SimpleCDN.Extensions.Redis&link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FSimpleCDN.Extensions.Redis)

## How to use SimpleCDN as a standalone application?
### Using Docker

**Tags:**
- `latest`: the latest stable release
- `main`: the latest build of the main branch, usually on the last commit. Not recommended for production as it may contain bugs or break.
- `vX.X.X`: pin to a specific version. Recommended for production scenarios. Supported versions can be found in the [tags listing](https://github.com/JonathanBout/SimpleCDN/tags).

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
    environment:
    - ASPNETCORE_URLS=http://+:8080

# === use below only if you want to use redis ===
    - Cache__Redis__ConnectionString=redis:6379
  redis:
    image: redis
```

> [!WARNING]  
> **Redis support is available, but it ocasionally fails**, especially in high-load scenario's. By implementing a custom connection manager,
> it's brought down to a minimum but failures still happen. The application will simply load the data from disk instead of using the cache.

### Using dotnet
#### dotnet run
```
# PublishAOT is not supported with dotnet run so we need to disable it
dotnet run --property:PublishAot=false -- --data-root <your_cdn_data>
```

### Variables:

> [!NOTE]  
> Command line arguments have precedence over appsettings.json and appsettings.json has precedence environment variables.

| command line argument | environment variable | appsettings.json | allowed values | default value | description |
|--|--|--|--|--|--|
| `--data-root` | `CDN_DATA_ROOT` | `CDN:DataRoot` | a local path | `/data` | The data root, where the files to be served are stored. |
| `--max-cache` | `CDN_CACHE_LIMIT` | `CDN:MaxMemoryCacheSize` | any number within the size of your devices memory | `500` | The maximum size of the cache, in kB |
| `--CDN:Footer` | `CDN__Footer` | `CDN:Footer` | Any HTML | `Powered by SimpleCDN` (with a link to this GitHub repo) | The text to place at the bottom of generated index files |
| `--CDN:PageTitle` | `CDN__PageTitle` | `CDN:PageTitle` | Any <title> compatible string | `SimpleCDN` | The text to display in the browser's title bar |

more options are available, for a full overview look at the models in the [SimpleCDN/Configuration](https://github.com/JonathanBout/SimpleCDN/tree/main/SimpleCDN/Configuration) folder.
- For command line arguments, use `--<section name>:<property name> "<property value>"` (section separator is `:`)
- For environment variables, use `<section name>__<property name>=<property value>` (section separator is `__`, double underscore)
- In appsettings.json, use the following structure:
  ```json
  {
    "<section name>": {
      "<property name>": "<property value>"
    }
  }
  ```
Where section name is one of the following:
- `CDN` - common CDN options corresponding to [CDNConfiguration.cs](https://github.com/JonathanBout/SimpleCDN/tree/main/SimpleCDN/Configuration/CDNConfiguration.cs)
- `Cache` - caching options corresponding to [CacheConfiguration.cs](https://github.com/JonathanBout/SimpleCDN/tree/main/SimpleCDN/Configuration/CacheConfiguration.cs)
  - `Cache` > `Redis` - Redis-specific options, corresponding to [RedisCacheConfiguration.cs](https://github.com/JonathanBout/SimpleCDN/tree/main/SimpleCDN/Configuration/RedisCacheConfiguration.cs)
  - `Cache` > `InMemory` - Redis-specific options, corresponding to [InMemoryCacheConfiguration.cs](https://github.com/JonathanBout/SimpleCDN/tree/main/SimpleCDN/Configuration/InMemoryCacheConfiguration.cs)
  When any options in the Redis section are defined, SimpleCDN will assume you want to use Redis. To overwrite this, use the `Cache` > `Type` property to use `InMemory` or no (`Disabled`) cache. 
 
## Development

Contributions are always welcome! Feel free to create an issue if you encounter problems. If you know a fix, a Pull Request is even better! 

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
