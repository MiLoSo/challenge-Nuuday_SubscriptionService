namespace SubscriptionService_Nuuday.Models.DataModels
{
    public class Subscription
    {
        public Subscription(SubscriptionType subscriptionType, string subscriptionId, string userId)
        {
            this.type = subscriptionType;
            this.subscriptionId = subscriptionId;
            this.userId = userId;
        }

        public string subscriptionId { get; set; }
        public SubscriptionType type { get; set; }
        public string userId { get; set; }
    }
}
