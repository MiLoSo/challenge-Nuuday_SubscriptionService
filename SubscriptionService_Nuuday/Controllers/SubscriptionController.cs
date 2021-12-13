using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SubscriptionService_Nuuday.Models.DataModels;

namespace SubscriptionService_Nuuday.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : Controller
    {
        private readonly IMemoryCache _memoryCache;
        MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
              .SetSlidingExpiration(TimeSpan.FromMinutes(100));

        public SubscriptionController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;

            //Set up some initial test data
            var newCustomer =  new Customer("1", "Alicia Keys", new List<Subscription>{
                new Subscription(SubscriptionType.Broadband, "1-1", "1"), new Subscription(SubscriptionType.TV, "0001-02", "0001") });

            _memoryCache.TryGetValue("customerKeys", out List<string> customerKeys);
            customerKeys = customerKeys ?? new List<string>();
            foreach (var customer in internalCustomerList)
            {
                _memoryCache.TryGetValue(customer.userId, out Customer exists);
                if (exists == null)
                {
                    _memoryCache.Set(customer.userId, customer, cacheEntryOptions);
                    customerKeys.Add(customer.userId);
                }  else
                {
                    break; //seems this has been done 
                }
            }
            _memoryCache.Set("customerKeys", customerKeys, cacheEntryOptions);
        
        }

        List<Customer> internalCustomerList = new List<Customer>()
        {
            new Customer("1", "Alicia Keys", new List<Subscription>{
                new Subscription(SubscriptionType.Broadband, "1-1", "1"), new Subscription(SubscriptionType.TV, "0001-02", "0001") }),
            new Customer("2", "Kent Fyre", new List<Subscription>{
                new Subscription(SubscriptionType.TV, "2-2", "2") }),
            new Customer("3", "Noel Reinegard"),
        };

        [HttpPost("addSubscription")]
        public async Task<bool> AddSubscription(string userId, string subScriptionType) //maybe other data
        {
            //add the subscription to the user... return updated list, or let client reload?
            var user = GetAllUsers().Result.FirstOrDefault(user => user.userId == userId);

            if (user == null)
            {
                //log warning
                return false;
            }

            Enum.TryParse(subScriptionType, out SubscriptionType type);
            if (type == SubscriptionType.None)
            {
                //log that subscription type was not valid
                return false;
            }

            var rng = new Random();
            var newSubscriptionId = user.userId + "-" + type;
            user.subscriptions.Add(new Subscription(type, newSubscriptionId, user.userId));
            SaveUser(user);
            return true;
        }

        //overwrite old user
        private void SaveUser(Customer user)
        {
            _memoryCache.Set(user.userId, user);
            _memoryCache.TryGetValue("customerKeys", out List<string> customerKeys);
            customerKeys = customerKeys ?? new List<string>();
            if (!customerKeys.Contains(user.userId))
            {
                customerKeys.Add(user.userId);
                _memoryCache.Set("customerKeys", customerKeys, cacheEntryOptions);
            }
        }

        [HttpPost("cancelsubscription")]
        public async Task<bool> CancelSubscription(string userId, string subscriptionId)
        {
            //delete the subscription on the user... return updated list, or let client reload?
            bool success = false;

            _memoryCache.TryGetValue(userId, out Customer user);
            
            if (user != null)
            {
                foreach (Subscription subscription in user.subscriptions)
                {
                    if (subscription.subscriptionId == subscriptionId)
                    {
                        user.subscriptions.Remove(subscription);
                        SaveUser(user);
                        success = true;
                        return success;
                    }
                }
            }
            return success;
        }

        [HttpGet("getallcustomers")]
        public async Task<IEnumerable<Customer>> GetAllUsers()
        {
            _memoryCache.TryGetValue("customerKeys", out List<string> customerKeys);
            List<Customer> customers = new List<Customer>();
            foreach (string customerKey in customerKeys)
            {
                _memoryCache.TryGetValue(customerKey, out Customer customer);
                if (customer != null)
                    customers.Add(customer);
            }
            return customers;

        }

        [HttpGet("getcustomer/{userId}")]
        public async Task<Customer> GetCustomer(string userId = "1")
        {
            _memoryCache.TryGetValue(userId, out Customer customer);
            return customer;
        }

        [HttpPost("deletecustomer")]
        public async Task<bool> DeleteCustomer(string userId = "3")
        {
            //string userId = value.userId;
            Customer customer = GetCustomer(userId).Result;
            if (customer == null)
            {
                //log error
                return false;
            }
            if (customer.subscriptions.Count() != 0)
            {
                //log error, subscriptions still present, can't delete customer
                return false;
            }

            _memoryCache.Remove(userId);

            //remove from list as well
            _memoryCache.TryGetValue("customerKeys", out List<string> customerKeys);
            if (customerKeys.Contains(userId))
            {
                customerKeys.Remove(userId);
                _memoryCache.Set("customerKeys", customerKeys);
            }

            return true;
        }

        [HttpPost("addcustomer")]
        public async Task<string> AddCustomer(string userName)
        {
            string userId = GetUnusedId();
            Customer newCustomer = new Customer(userId, userName);
            SaveUser(newCustomer);

            return userId;
        }

        private string GetUnusedId()
        {
            int highestIndex = 0;
            var customers = GetAllUsers().Result.ToList();
            foreach (Customer customer in customers)
            {
                int.TryParse(customer.userId, out var index);
                if (index > highestIndex)
                    highestIndex = index;
            }
            return "" + (highestIndex + 1);
        }
    }
}
