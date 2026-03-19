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
    private readonly FirebaseMessaging _firebaseMessaging;

    public SendNotification(IGeneric<DeviceTokens> pushNotification, IGeneric<Notification> notification, FirebaseMessaging firebaseMessaging)
    {
        _pushNotification = pushNotification;
        _notification = notification;
        _firebaseMessaging = firebaseMessaging;
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
                await SendAdminNotificationWithoutDeviceToken(userId, title, messageBody, id, Image);
            }
        }

        if (users == null || users.Count == 0)
        {
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
                string response = await _firebaseMessaging.SendAsync(messageToSend);
            }
            catch (FirebaseMessagingException ex)
            {
                // Handle or log the error
            }

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
