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

    public async Task SendToAllSpecificAndroidUserDevices(string userId, string title, string messageBody, bool sendToAll = false, long id = -1, string Image = null, string notificationType = "general")
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
                // المستخدم مسجل بس مفيش device token - نحفظ الإشعار في الداتابيز بس
                await SendAdminNotificationWithoutDeviceToken(userId, title, messageBody, id, Image);
                return;
            }
        }

        if (users == null || users.Count == 0)
        {
            return;
        }

        // نحفظ الإشعار في الداتابيز الأول (مرة واحدة بس مش لكل device)
        await _notification.Add(new Notification()
        {
            UserId = userId,
            Body = messageBody,
            Title = title,
            IsSeen = false,
            ImageUrl = Image,
        });

        // نبعت Push Notification لكل device token
        foreach (var user in users)
        {
            var messageToSend = new Message()
            {
                Token = user.Token,
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = title,
                    Body = messageBody,
                    ImageUrl = Image,
                },
                Data = new Dictionary<string, string>()
                {
                    { "id", id.ToString() },
                    { "type", notificationType }
                }
            };

            try
            {
                string response = await _firebaseMessaging.SendAsync(messageToSend);
            }
            catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
            {
                // التوكن مش صالح - نحذفه عشان ما نبعتلوش تاني
                user.IsDeleted = true;
                await _pushNotification.Update(user);
            }
            catch (FirebaseMessagingException ex)
            {
                // أي خطأ تاني من Firebase - نسجله ونكمل للأجهزة التانية
                System.Diagnostics.Debug.WriteLine($"Firebase Error for user {userId}, token {user.Token}: {ex.MessagingErrorCode} - {ex.Message}");
            }
            catch (System.Exception ex)
            {
                // خطأ عام (مثل Invalid JWT Signature) - credentials غير صالحة
                System.Diagnostics.Debug.WriteLine($"FCM Credential Error: {ex.Message}");
                // نكمل - الإشعار اتحفظ في الداتابيز على الأقل
                // لكن ما نقدرش نبعت push - محتاج تجديد المفاتيح
                break; // مفيش فايدة نكمل لو الـ credentials باظت
            }
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
