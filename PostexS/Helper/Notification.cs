using FirebaseAdmin.Messaging;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Notification = PostexS.Models.Domain.Notification;

public class SendNotification
{
    private readonly IGeneric<DeviceTokens> _pushNotification;
    private readonly IGeneric<Notification> _notification;

    public SendNotification(IGeneric<DeviceTokens> pushNotification, IGeneric<Notification> notification)
    {
        _pushNotification = pushNotification;
        _notification = notification;
    }

    public async Task SendToAllSpecificAndroidUserDevices(string userId, string title, string messageBody, bool sendToAll = false, long id = -1, string Image = null)
    {
        List<DeviceTokens> users = new List<DeviceTokens>();

        if (sendToAll)
        {
            if (await _pushNotification.IsExist(x => !x.IsDeleted))
            {
                users = _pushNotification.Get(x => !x.IsDeleted).ToList();
            }
        }
        else if (!string.IsNullOrEmpty(userId))
        {
            if (await _pushNotification.IsExist(x => x.UserId == userId && !x.IsDeleted))
            {
                users = _pushNotification.Get(x => x.UserId == userId && !x.IsDeleted).ToList();
            }
            else
            {
                // Handle cases where the user has no device tokens (like admins)
                // Send notification to admins without device tokens
                // Optionally, log that the user has no tokens, but we still need to notify them
                await SendAdminNotificationWithoutDeviceToken(userId, title, messageBody, id, Image);
            }
        }

        if (users == null || users.Count == 0)
        {
            // Optionally, log that no users were found
            return;
        }

        foreach (var user in users)
        {
            var messageToSend = new Message()
            {
                Token = user.Token,
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = title,
                    Body = messageBody,
                },
                Data = new Dictionary<string, string>()
                {
                    { "id", id.ToString() }
                }
            };

            try
            {
                // Send message via Firebase
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(messageToSend);
                // Optionally, log the response
            }
            catch (FirebaseMessagingException ex)
            {
                // Handle or log the error
                // Example: Console.WriteLine($"Error sending message: {ex.Message}");
            }

            // Add notification to your database
            await _notification.Add(new Notification()
            {
                UserId = userId,
                Body = messageBody,
                Title = title,
                IsSeen = false,
                ImageUrl = Image,
            });
        }
    }
    public async Task SendAdminNotificationWithoutDeviceToken(string userId, string title, string messageBody, long id, string Image = null)
    {
        // Handle admins who don't have a device token, e.g., send email or web notification
        // Example: Add to database for web-based notification or send email to admin
        await _notification.Add(new Notification()
        {
            UserId = userId,
            Body = messageBody,
            Title = title,
            IsSeen = false,
            ImageUrl = Image,
        });

        // Optionally, send an email or other type of notification
        // await SendEmailNotificationToAdmin(userId, title, messageBody);
    }

}
