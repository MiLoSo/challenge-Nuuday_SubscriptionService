namespace SubscriptionService_Nuuday.Models.Exceptions
{
    public class ClientSubscriptionException : Exception
    {
        public ClientSubscriptionException(string message)
        {
            this.message = message;
        }
        public string message { get; set; }
    }
}
