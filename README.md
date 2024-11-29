# SimpleCDN

SimpleCDN is, well, a simple CDN server. Currently it is only tested for single-instance use, but I don't see any reasons why a load balancer wouldn't work. Be aware this CDN currently doesn't support any
distributed caching, meaning both servers will build their own cache.

## How to run this?
### Using Docker

**Tags:**
- `latest`: the latest stable release
- `vX.X.X`: pin to a specific version. Recommended for production scenarios. Supported versions can be found in the [tags listing](https://github.com/JonathanBout/SimpleCDN/tags).
- `dev`: the latest build of the `main` branch. Not recommended for production as it may contain bugs.

#### `docker run`
```
docker run -p "<your_port>:8080" -v "<your_cdn_data>:/data:ro" ghcr.io/jonathanbout/simplecdn
```
This will pull and run the latest stable build of SimpleCDN.

#### `docker compose`
```yml
services:
  server:
    image: ghcr.io/jonathanbout/simplecdn
    volumes:
    - <your_cdn_data>:/data:ro # :ro to make the bind mount read-only
    ports:
    - <your_port>:8080
```

### Using dotnet
```
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
