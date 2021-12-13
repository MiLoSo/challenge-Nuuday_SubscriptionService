using System.ComponentModel.DataAnnotations;

namespace SubscriptionService_Nuuday.Models.DataModels
{
    public class Customer
    {
        public Customer(string id, string name, List<Subscription> subscriptions = null)
        {
            userId = id;
            userName = name;
            this.subscriptions = subscriptions ?? new List<Subscription>();
        }
        [Required]
        public string userName { get; set; }
        [Required]
        public string userId { get; set; }
        public List<Subscription> subscriptions { get; set; }
    }
}
