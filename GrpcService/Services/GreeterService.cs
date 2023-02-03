using Grpc.Core;
using StackExchange.Redis;

namespace GrpcService.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    private readonly IConnectionMultiplexer _redis;

    public GreeterService(ILogger<GreeterService> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Hello request: {hellorequest}", request.ToString());

        var database = _redis.GetDatabase();
        await database.StringAppendAsync("NAMES", $"{request.Name}\n");

        return new HelloReply
        {
            Message = "Hello " + request.Name
        };
    }
}