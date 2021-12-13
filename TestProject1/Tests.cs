using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubscriptionService_Nuuday.Client;
using SubscriptionService_Nuuday.Models.DataModels;
using SubscriptionService_Nuuday.Models.Exceptions;
using System.Linq;
using System.Net.Http;

namespace TestProject1
{
    [TestClass]
    public class Tests
    {
        public static ClientSubscriptionService clientSubscriptionService = null;
        public static HttpClient _client;

        [ClassInitialize]
        public static void SetUp(TestContext testContext)
        {
            _client = null;//new HttpClient();

            clientSubscriptionService = new ClientSubscriptionService(_client);
        }

        [TestMethod]
        public void TestAddCustomer()
        {
            var id = clientSubscriptionService.AddCustomer("Bob Newman").Result;
            Customer customer = clientSubscriptionService.GetCustomerByName("Bob Newman");

            Assert.IsNotNull(customer);
            Assert.AreEqual("Bob Newman", customer.userName);
        }

        [TestMethod]
        public void TestRemoveCustomer()
        {
            var id = clientSubscriptionService.AddCustomer("Bobby Newman").Result;
            Customer customer = clientSubscriptionService.GetCustomerById(id).Result;
            bool success = clientSubscriptionService.RemoveCustomer(customer.userId).Result;
            //customer = clientSubscriptionService.GetCustomerById(id).Result;
            var customers = clientSubscriptionService.GetAllCustomers().Result;
            

            Assert.IsNull(customers.FirstOrDefault(c => c.userId == id));
        }

        [TestMethod]
        public void TestCustomerHasSubScriptionType()
        {
            //subscriptionController.AddCustomer("Barbara Styles");
            var userId = clientSubscriptionService.AddCustomer("Barbara Styles").Result;

            var customer = /*subscriptionController*/clientSubscriptionService.GetAllCustomers().Result
                .FirstOrDefault(user => user.userName == "Barbara Styles");

            clientSubscriptionService.AddSubscription(customer.userId, SubscriptionType.Broadband.ToString());
            clientSubscriptionService.AddSubscription(customer.userId, SubscriptionType.TV.ToString());

            //customer 1 already has the TV subscription
            Assert.IsNotNull(clientSubscriptionService.CustomerHasSubscriptionType(customer.userId, "TV"));
            //but not streaming
            var streamingSubscription = clientSubscriptionService.CustomerHasSubscriptionType(customer.userId, "Broadband");
            Assert.IsNotNull(streamingSubscription);
        }

        [ExpectedException(typeof(ClientSubscriptionException), "Customer id 1 already has a subscription of type TV")]
        [TestMethod]
        public void TestDoNotAddSubscriptionIfTheTypeExists()
        {
            //customer 1 already has the TV subscription
            Customer customer = clientSubscriptionService.GetCustomerById("1").Result;
            int initialSubScriptionCount = customer.subscriptions.Count();

            clientSubscriptionService.AddSubscriptionIfNotExisting("1", "TV");

            //wasn't added, since the type exists
            //Assert.AreEqual(initialSubScriptionCount, customer.subscriptions.Count());
        }

        [TestMethod]
        public void TestAddSubscriptionEvenIfTheTypeExists()
        {
            //customer 1 already has the TV subscription
            Customer customer = clientSubscriptionService.GetCustomerById("1").Result;
            int initialSubScriptionCount = customer.subscriptions.Count();

            var success = clientSubscriptionService.AddSubscription("1", "TV").Result;

            customer = clientSubscriptionService.GetCustomerById(customer.userId).Result;

            //wasn't added, since the type exists
            Assert.AreEqual(initialSubScriptionCount + 1, customer.subscriptions.Count());
        }

        [TestMethod]
        public void TestInvalidSubscriptionTypeDoesNotGetAdded()
        {
            var userId = clientSubscriptionService.AddCustomer("Barbara Styles").Result;
            var customers = clientSubscriptionService.GetAllCustomers().Result;
            var customer = clientSubscriptionService.GetCustomerById(userId).Result;
            Assert.IsNotNull(customer);

            int initialSubscriptionCount = customer.subscriptions.Count();

            var success = clientSubscriptionService.AddSubscription(userId, "blabla").Result;

            customer = clientSubscriptionService.GetCustomerById(userId).Result;

            Assert.AreEqual(initialSubscriptionCount, customer.subscriptions.Count());
        }
    }
}