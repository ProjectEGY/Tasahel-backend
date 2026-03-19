using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace PostexS.Services
{
    public class FirebaseMessagingService
    {
        /// <summary>
        /// Firebase Messaging instance لتطبيق المندوب (Captain)
        /// </summary>
        public FirebaseMessaging CaptainMessaging { get; }

        /// <summary>
        /// Firebase Messaging instance لتطبيق الراسل (Customer)
        /// </summary>
        public FirebaseMessaging CustomerMessaging { get; }

        public FirebaseMessagingService(string captainJsonPath, string customerJsonPath)
        {
            // Initialize Captain (Driver) Firebase App
            var captainCredential = GoogleCredential.FromFile(captainJsonPath);
            var captainApp = FirebaseApp.Create(new AppOptions
            {
                Credential = captainCredential
            }, "captain");
            CaptainMessaging = FirebaseMessaging.GetMessaging(captainApp);

            // Initialize Customer (Sender) Firebase App
            var customerCredential = GoogleCredential.FromFile(customerJsonPath);
            var customerApp = FirebaseApp.Create(new AppOptions
            {
                Credential = customerCredential
            }, "customer");
            CustomerMessaging = FirebaseMessaging.GetMessaging(customerApp);
        }
    }
}
