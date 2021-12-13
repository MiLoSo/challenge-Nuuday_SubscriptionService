using Microsoft.Extensions.Caching.Memory;

namespace SubscriptionService_Nuuday.Models.CacheModels
{
    public class MyMemoryCache
    {
        public MemoryCache Cache { get; } = new MemoryCache(
            new MemoryCacheOptions
            {
                //SizeLimit = 1024
            });
    }
}
