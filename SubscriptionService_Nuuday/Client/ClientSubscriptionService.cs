using Newtonsoft.Json;
using SubscriptionService_Nuuday.Models.DataModels;
using SubscriptionService_Nuuday.Models.Exceptions;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

namespace SubscriptionService_Nuuday.Client
{
    public class ClientSubscriptionService
    {
        public static HttpClient client = new HttpClient();

        public static string apiPath = "https://localhost:7175/api/subscription";

        public ClientSubscriptionService(HttpClient _client = null)
        {
            client.BaseAddress = new Uri(apiPath);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            client = _client ?? client;
        }

        public async Task<IEnumerable<Customer>> GetAllCustomers()
        {
            List<Customer> customers = null;
            string path = apiPath + "/getallcustomers";
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var customersJsonString = await response.Content.ReadAsStringAsync();
                customers = JsonConvert.DeserializeObject<List<Customer>>(customersJsonString);
            }

            if (customers == null)
            {
                //log warning
                throw new ClientSubscriptionException("No customers could be retrieved.");
            }

            return customers;

        }

        public async Task<Customer> GetCustomerById(string userId)
        {
            Customer customer = null;
            string path = apiPath + "/getcustomer/" + userId;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var customerJsonString = await response.Content.ReadAsStringAsync();
                customer = JsonConvert.DeserializeObject<Customer>(customerJsonString);
            }

            if (customer == null)
            {
                //log warning
                throw new ClientSubscriptionException("Customer id " + userId + " could not be retrieved.");
            }

            return customer;
        }

        public Customer GetCustomerByName(string userName)
        {
            var customers = GetAllCustomers().Result.ToList();
            Customer customer = customers.FirstOrDefault(user => user.userName == userName);

            if (customer == null)
            {
                throw new ClientSubscriptionException("Customer " + userName + " could not be retrieved.");
            }

            return customer;
        }

        public Subscription CustomerHasSubscriptionType(string userId, string subscriptionType)
        {
            Customer customer = GetCustomerById(userId).Result;

            Enum.TryParse(subscriptionType, out SubscriptionType type);

            Subscription subscription = customer.subscriptions.FirstOrDefault(subscription => subscription.type == type);

            //if the user had no such subscription type, log an error
            if (subscription == null)
            {
                return null;
                //throw new ClientSubscriptionException("Subscription of type \"" + type.ToString() + "\" did not exist on customer id " + userId);
            }

            return subscription;
        }

        //only add if not already present - possibly return error/warning if present
        public void AddSubscriptionIfNotExisting(string userId, string subscriptionType)
        {
            if (CustomerHasSubscriptionType(userId, subscriptionType) != null)
            {
                throw new ClientSubscriptionException("Customer id " + userId + " already has a subscription of type " + subscriptionType);
            }

            AddSubscription(userId, subscriptionType);
        }

        //duplicate subscription types allowed
        public async Task<bool> AddSubscription(string userId, string subscriptionType)
        {
            Uri path = new Uri(apiPath + "/addsubscription");
            var uriBuilder = new UriBuilder(path);
            var parameters = HttpUtility.ParseQueryString(uriBuilder.Query);
            parameters["userId"] = userId;
            parameters["subscriptionType"] = subscriptionType;
            uriBuilder.Query = parameters.ToString();

            bool success = false;

            var response = await client.PostAsync(uriBuilder.ToString(), null).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var jsonstring = await response.Content.ReadAsStringAsync();
                success = JsonConvert.DeserializeObject<bool>(jsonstring);

            }
            else
            {
                throw new ClientSubscriptionException("Could not add subscription of type" + subscriptionType + " to customer " + userId);
            }
            return success;
        }

        public async void CancelSubscription(string userId, string subscriptionId)
        {
            Customer customer = GetCustomerById(userId).Result;
            Subscription subscription = customer.subscriptions.FirstOrDefault(s => s.subscriptionId == subscriptionId);

            if (subscription == null)
                throw new ClientSubscriptionException("Could not find subscription " + subscriptionId + " on customer " + userId);

            Uri path = new Uri(apiPath + "/cancelsubscription");
            var uriBuilder = new UriBuilder(path);
            var parameters = HttpUtility.ParseQueryString(uriBuilder.Query);
            parameters["userId"] = userId;
            parameters["subscriptionId"] = subscriptionId;
            uriBuilder.Query = parameters.ToString();

            var response = await client.PostAsync(uriBuilder.ToString(), null).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new ClientSubscriptionException("Could not remove subscription " + subscriptionId + " on customer " + userId);
            }
        }

        /// <summary>
        /// Will attempt to pick the first subscription on the user which matches the subscription type
        /// </summary>
        public async void CancelSubscriptionByType(string userId, string subscriptionType)
        {
            Subscription subscription = CustomerHasSubscriptionType(userId, subscriptionType);
            if (subscription == null)
                return;

            Uri path = new Uri(apiPath + "/cancelsubscription");
            var uriBuilder = new UriBuilder(path);
            var parameters = HttpUtility.ParseQueryString(uriBuilder.Query);
            parameters["userId"] = userId;
            parameters["subscriptionId"] = subscription.subscriptionId;
            uriBuilder.Query = parameters.ToString();

            var response = await client.PostAsync(uriBuilder.ToString(), null).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new ClientSubscriptionException("Could not find subscription of type" + subscriptionType + " on customer " + userId);
            }
        }

        /// <summary>
        /// Remove all subscriptions of a type from all users
        /// </summary>
        public void RemoveAllSubscriptionsOfType(string subscriptionType)
        {
            var usersWithSubscriptionType = GetUsersWithSubscriptionType(subscriptionType);

            if (usersWithSubscriptionType.Count() == 0)
                return;

            Enum.TryParse(subscriptionType, out SubscriptionType type);
            foreach (Customer customer in usersWithSubscriptionType)
            {
                List<Subscription> subScriptionsToCancel = customer.subscriptions.Where(s => s.type == type).ToList();

                foreach (Subscription subscription in subScriptionsToCancel)
                    CancelSubscription(customer.userId, subscription.subscriptionId);
            }
        }

        //Assuming the users can be returned with their subscription data
        //in some cases that data may be large enough that separate calls for more data would be necessary
        public IEnumerable<Customer> GetUsersWithSubscriptionType(string subscriptionType)
        {
            Enum.TryParse(subscriptionType, out SubscriptionType type);
            return GetAllCustomers().Result.Where(user => user.subscriptions
                    .Any(subscription => subscription.type == type));
        }

        public async Task<string> AddCustomer(string userName)
        {
            string newUserId = "";

            Uri path = new Uri(apiPath + "/addcustomer");
            var uriBuilder = new UriBuilder(path);
            var parameters = HttpUtility.ParseQueryString(uriBuilder.Query);
            parameters["userName"] = userName;
            uriBuilder.Query = parameters.ToString();

            var response = await client.PostAsync(uriBuilder.ToString(), null).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                newUserId = JsonConvert.DeserializeObject<string>(jsonResponse);
            }
            else
            {
                throw new ClientSubscriptionException("Could not add new user with name " + userName);
            }
            return newUserId;
        }

        public async Task<bool> RemoveCustomer(string userId)
        {
            Customer customer = GetCustomerById(userId).Result;

            //This code is rather simple, but in another scenario,
            //we may need to go through a large process to shut down subscriptions correctly 
            foreach (Subscription subscription in customer.subscriptions)
            {
                CancelSubscription(userId, subscription.subscriptionId);
            }

            bool success = false;

            Uri path = new Uri(apiPath + "/deletecustomer");
            var uriBuilder = new UriBuilder(path);
            var parameters = HttpUtility.ParseQueryString(uriBuilder.Query);
            parameters["userId"] = userId;
            uriBuilder.Query = parameters.ToString();

            var response = await client.PostAsync(uriBuilder.ToString(), null).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                success = JsonConvert.DeserializeObject<bool>(jsonResponse);
            }
            else
            {
                throw new ClientSubscriptionException("Could not remove user " + userId);
            }
            return success;
        }

    }
}

