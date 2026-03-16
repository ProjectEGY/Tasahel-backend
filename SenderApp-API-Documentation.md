# Sender App - API Documentation
# تطبيق الراسل - توثيق الـ APIs

**Base URL:** `https://your-domain.com/api/SenderApp`
**Response Format:** All endpoints return:
```json
{
  "errorCode": 0,
  "errorMessage": "Success",
  "data": { ... }
}
```

---

## Authentication

### 1. Login - تسجيل الدخول
```
POST /api/SenderApp/Login
```
**Auth:** None

**Body:**
```json
{
  "phone": "01xxxxxxxxx",
  "password": "123456"
}
```

**Response (data):**
```json
{
  "id": "user-id",
  "name": "اسم الراسل",
  "phone": "01xxxxxxxxx",
  "email": "email@example.com",
  "whatsappPhone": "01xxxxxxxxx",
  "address": "العنوان",
  "wallet": 1500.0,
  "branchId": 1,
  "branchName": "الفرع الرئيسي",
  "hideSenderName": false,
  "hideSenderPhone": false,
  "hideSenderCode": false,
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

**Error Codes:**
| Code | المعنى |
|------|--------|
| TheUsernameOrPasswordIsIncorrect | رقم الهاتف أو كلمة السر غير صحيحة |
| InvalidUserType | الحساب ليس حساب راسل |
| UserNotApproved | الحساب في انتظار الموافقة |

---

### 2. Register - تسجيل حساب جديد
```
POST /api/SenderApp/Register
```
**Auth:** None

**Body:**
```json
{
  "name": "اسم الراسل",
  "phone": "01xxxxxxxxx",
  "password": "123456",
  "email": "email@example.com",
  "whatsappPhone": "01xxxxxxxxx",
  "address": "العنوان",
  "branchId": 1
}
```
> `name`, `phone`, `password`, `branchId` = required
> `email`, `whatsappPhone`, `address` = optional

**Response:** Same as Login (includes token)

**Error Codes:**
| Code | المعنى |
|------|--------|
| PhoneAlreadyRegistered | رقم الهاتف مسجل بالفعل |

---

### 3. Forget Password - نسيت كلمة السر
```
PUT /api/SenderApp/ForgetPassword?Phone=01xxxxxxxxx
```
**Auth:** None

**Response (data):** `"تم إعادة تعيين كلمة السر بنجاح"`

> كلمة السر الجديدة الافتراضية: `123456`

---

### 4. Branches - قائمة الفروع
```
GET /api/SenderApp/Branches
```
**Auth:** None

> يستخدم في شاشة التسجيل لاختيار الفرع

**Response (data):**
```json
[
  {
    "id": 1,
    "name": "الفرع الرئيسي",
    "address": "العنوان",
    "phoneNumber": "01xxxxxxxxx",
    "whatsapp": "01xxxxxxxxx",
    "latitude": 30.0,
    "longitude": 31.0
  }
]
```

---

## Profile - البروفايل

> كل الـ endpoints التالية تحتاج **JWT Token** في الـ Header:
> `Authorization: Bearer {token}`

### 5. Get Profile - البروفايل
```
GET /api/SenderApp/Profile
```

**Response:** Same as Login response (without token)

---

### 6. Update Profile - تحديث البروفايل
```
PUT /api/SenderApp/Profile
```

**Body:** (كل الحقول optional - اللي يتبعت بس هو اللي يتحدث)
```json
{
  "name": "الاسم الجديد",
  "email": "new@email.com",
  "whatsappPhone": "01xxxxxxxxx",
  "address": "العنوان الجديد"
}
```

**Response:** Updated profile data

---

### 7. Change Password - تغيير كلمة السر
```
PUT /api/SenderApp/ChangePassword
```

**Body:**
```json
{
  "oldPassword": "123456",
  "newPassword": "654321"
}
```

**Error Codes:**
| Code | المعنى |
|------|--------|
| TheOldPasswordIsInCorrect | كلمة السر القديمة غير صحيحة |

---

## Orders - الشحنات

### 8. Create Order - إنشاء شحنة جديدة
```
POST /api/SenderApp/Orders
```

**Body:**
```json
{
  "clientName": "اسم المستلم",
  "clientPhone": "01xxxxxxxxx",
  "clientCode": "كود المستلم",
  "clientCity": "المدينة",
  "address": "عنوان التوصيل",
  "cost": 250.0,
  "deliveryFees": 50.0,
  "notes": "ملاحظات"
}
```
> `clientName`, `clientPhone` = required

**Response (data):**
```json
{
  "orderCode": "Tas123",
  "receiverName": "اسم المستلم",
  "receiverCode": "كود المستلم",
  "receiverPhone": "01xxxxxxxxx",
  "city": "المدينة",
  "address": "عنوان التوصيل",
  "cost": 250.0,
  "deliveryFees": 50.0,
  "totalCost": 300.0,
  "arrivedCost": 0.0,
  "status": 0,
  "statusArabic": "جديد",
  "notes": "ملاحظات",
  "createdOn": "2026-03-16T12:00:00",
  "lastUpdated": null,
  "deliveryAgentName": null,
  "deliveryAgentPhone": null
}
```

---

### 9. List Orders - قائمة الشحنات
```
GET /api/SenderApp/Orders?search=&status=&from=&to=&pageNumber=1&pageSize=10
```

**Query Parameters:** (كلهم optional)
| Parameter | Type | الوصف |
|-----------|------|-------|
| search | string | بحث بالاسم أو الكود أو رقم الهاتف |
| status | int | فلتر بالحالة (0=Placed, 1=Assigned, 2=Delivered, ...) |
| from | DateTime | من تاريخ |
| to | DateTime | إلى تاريخ |
| pageNumber | int | رقم الصفحة (default: 1) |
| pageSize | int | عدد العناصر (default: 10, max: 50) |

**Order Status Values:**
| Value | English | Arabic |
|-------|---------|--------|
| 0 | Placed | جديد |
| 1 | Assigned | جارى التوصيل |
| 2 | Delivered | تم التوصيل |
| 3 | Waiting | مؤجل |
| 4 | Rejected | مرفوض |
| 5 | Finished | منتهي |
| 6 | Completed | تم تسويته |
| 7 | PartialDelivered | تم التوصيل جزئي |
| 8 | Returned | مرتجع كامل |
| 9 | PartialReturned | مرتجع جزئي |
| 10 | Delivered_With_Edit_Price | تم التوصيل مع تعديل السعر |
| 11 | Returned_And_Paid_DeliveryCost | مرتجع ودفع شحن |
| 12 | Returned_And_DeliveryCost_On_Sender | مرتجع وشحن على الراسل |

**Response (data):**
```json
{
  "items": [ ... ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 50,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

### 10. Order Details - تفاصيل شحنة
```
GET /api/SenderApp/Orders/{code}
```
> Example: `GET /api/SenderApp/Orders/Tas123`

**Response (data):**
```json
{
  "orderCode": "Tas123",
  "receiverName": "اسم المستلم",
  "receiverPhone": "01xxxxxxxxx",
  "cost": 250.0,
  "deliveryFees": 50.0,
  "totalCost": 300.0,
  "status": 2,
  "statusArabic": "تم التوصيل",
  "deliveryAgentName": "اسم المندوب",
  "deliveryAgentPhone": "01xxxxxxxxx",
  "timeline": [
    { "action": "Created", "actionArabic": "تم الإنشاء", "date": "2026-03-14T10:00:00" },
    { "action": "Accepted", "actionArabic": "تمت الموافقة", "date": "2026-03-14T11:00:00" },
    { "action": "Assigned to Driver", "actionArabic": "تم التعيين لمندوب", "date": "2026-03-14T12:00:00" },
    { "action": "Finished", "actionArabic": "تم الانتهاء", "date": "2026-03-15T14:00:00" }
  ],
  "barcodeImageBase64": "iVBORw0KGgo...",
  "senderName": "اسم الراسل",
  "senderPhone": "01xxxxxxxxx",
  "senderCode": "user-id",
  "isPrinted": false
}
```
> `senderName`, `senderPhone`, `senderCode` = null لو الراسل فعّل إعدادات الإخفاء

---

### 11. Edit Order - تعديل شحنة
```
PUT /api/SenderApp/Orders/{code}
```
> يعمل فقط لو الشحنة `status = Placed` و `pending = true` (قبل موافقة الأدمن)

**Body:** (كل الحقول optional)
```json
{
  "clientName": "اسم جديد",
  "clientPhone": "01xxxxxxxxx",
  "clientCode": "كود جديد",
  "clientCity": "مدينة جديدة",
  "address": "عنوان جديد",
  "cost": 300.0,
  "deliveryFees": 60.0,
  "notes": "ملاحظات جديدة"
}
```

**Error Codes:**
| Code | المعنى |
|------|--------|
| OrderCannotBeEdited | لا يمكن التعديل بعد موافقة الأدمن |

---

### 12. Cancel Order - إلغاء شحنة
```
DELETE /api/SenderApp/Orders/{code}
```
> يعمل فقط لو الشحنة `status = Placed` و `pending = true` (قبل موافقة الأدمن)

**Error Codes:**
| Code | المعنى |
|------|--------|
| OrderCannotBeCancelled | لا يمكن الإلغاء بعد موافقة الأدمن |

---

### 13. Order Receipt - بيانات البوليصة
```
GET /api/SenderApp/Orders/{code}/Receipt
```

**Response (data):**
```json
{
  "orderCode": "Tas123",
  "receiverName": "اسم المستلم",
  "receiverPhone": "01xxxxxxxxx",
  "receiverCode": "كود المستلم",
  "city": "المدينة",
  "address": "العنوان",
  "cost": 250.0,
  "deliveryFees": 50.0,
  "totalCost": 300.0,
  "notes": "ملاحظات",
  "status": 0,
  "statusArabic": "جديد",
  "createdOn": "2026-03-16T12:00:00",
  "barcodeImageBase64": "iVBORw0KGgo...",
  "senderName": "اسم الراسل",
  "senderPhone": "01xxxxxxxxx",
  "senderCode": "user-id",
  "branchName": "الفرع الرئيسي",
  "branchAddress": "عنوان الفرع",
  "branchPhone": "01xxxxxxxxx",
  "deliveryAgentName": null,
  "deliveryAgentPhone": null
}
```

---

## Financial - المالية

### 14. Wallet Balance - رصيد المحفظة
```
GET /api/SenderApp/Wallet
```

**Response (data):**
```json
{
  "balance": 1500.0
}
```

---

### 15. Financial Summary - ملخص مالي
```
GET /api/SenderApp/Wallet/Summary?from=2026-01-01&to=2026-03-31
```

**Query Parameters:** (optional)
| Parameter | Type | الوصف |
|-----------|------|-------|
| from | DateTime | من تاريخ |
| to | DateTime | إلى تاريخ |

**Response (data):**
```json
{
  "walletBalance": 1500.0,
  "totalOrdersCost": 50000.0,
  "totalDeliveryFees": 5000.0,
  "totalCollected": 45000.0,
  "totalOrders": 200,
  "deliveredOrders": 150,
  "returnedOrders": 30,
  "pendingOrders": 20
}
```

---

### 16. Wallet Transactions - سجل الحركات المالية
```
GET /api/SenderApp/Wallet/Transactions?pageNumber=1&pageSize=20
```

**Response (data):**
```json
{
  "items": [
    {
      "id": 1,
      "transactionType": 3,
      "amount": 500.0,
      "walletBalanceAfter": 1500.0,
      "note": "تسوية طلبات",
      "orderNumber": null,
      "date": "2026-03-15T10:00:00"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 10,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

**Transaction Types:**
| Value | المعنى |
|-------|--------|
| 0 | AddedByAdmin - إضافة من الأدمن |
| 1 | RemovedByAdmin - خصم من الأدمن |
| 2 | OrderFinished - طلب منتهي |
| 3 | OrderComplete - تسوية طلبات |
| 4 | OrderReturnedComplete - تسوية مرتجعات |
| 5 | ReAddToWallet - إعادة إضافة |

---

## Settlements - التسويات

### 17. Order Settlements - قائمة تسويات الطلبات
```
GET /api/SenderApp/Settlements/Orders?pageNumber=1&pageSize=20
```

**Response (data):**
```json
{
  "items": [
    {
      "id": 1,
      "amount": 5000.0,
      "date": "2026-03-15T10:00:00",
      "orderCount": 10,
      "transactionType": 3,
      "note": null
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 5,
  "totalPages": 1
}
```

---

### 18. Order Settlement Details - تفاصيل تسوية طلبات
```
GET /api/SenderApp/Settlements/Orders/{walletId}
```

**Response (data):**
```json
{
  "settlement": {
    "id": 1,
    "amount": 5000.0,
    "date": "2026-03-15T10:00:00",
    "orderCount": 3,
    "transactionType": 3,
    "note": null
  },
  "orders": [
    {
      "code": "Tas123",
      "clientName": "اسم المستلم",
      "clientPhone": "01xxxxxxxxx",
      "arrivedCost": 300.0,
      "deliveryCost": 50.0,
      "clientCost": 250.0,
      "cost": 250.0,
      "totalCost": 300.0,
      "status": 6,
      "statusArabic": "تم تسويته"
    }
  ]
}
```

---

### 19. Return Settlements - قائمة تسويات المرتجعات
```
GET /api/SenderApp/Settlements/Returns?pageNumber=1&pageSize=20
```

**Response:** Same structure as Order Settlements

---

### 20. Return Settlement Details - تفاصيل تسوية مرتجعات
```
GET /api/SenderApp/Settlements/Returns/{walletId}
```

**Response:** Same structure as Order Settlement Details

---

## Notifications - الإشعارات

### 21. Register Push Token - تسجيل رمز الإشعارات
```
POST /api/SenderApp/Notifications/PushToken?pushToken=firebase_token_here
```

> يتم استدعاؤه عند فتح التطبيق أو عند تحديث الـ Firebase token

---

### 22. List Notifications - قائمة الإشعارات
```
GET /api/SenderApp/Notifications?pageNumber=1&pageSize=20
```

**Response (data):**
```json
{
  "items": [
    {
      "id": 1,
      "title": "تم توصيل شحنتك",
      "body": "الشحنة Tas123 تم توصيلها بنجاح",
      "isSeen": false,
      "date": "2026-03-16T14:00:00"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 5,
  "totalPages": 1
}
```

> الإشعارات تتحول تلقائيا لـ `isSeen: true` بعد ما ترجع في الـ response

---

### 23. Unseen Count - عدد الإشعارات غير المقروءة
```
GET /api/SenderApp/Notifications/UnseenCount
```

**Response (data):**
```json
{
  "count": 3
}
```

---

## Support - الدعم

### 24. Contact Us - بيانات التواصل
```
GET /api/SenderApp/ContactUs
```
**Auth:** None

**Response (data):**
```json
{
  "faceBook": "https://facebook.com/...",
  "twitter": "https://twitter.com/...",
  "instgram": "https://instagram.com/...",
  "branches": [
    {
      "id": 1,
      "name": "الفرع الرئيسي",
      "address": "العنوان",
      "phoneNumber": "01xxxxxxxxx",
      "whatsapp": "01xxxxxxxxx",
      "latitude": 30.0,
      "longitude": 31.0
    }
  ]
}
```

---

## Common Error Codes

| Code | المعنى |
|------|--------|
| Success (0) | نجاح |
| TheModelIsInvalid | البيانات المرسلة غير صحيحة |
| SomeThingWentwrong | حدث خطأ في السيرفر |
| TheUserNotExistOrDeleted | المستخدم غير موجود أو محذوف |
| TheUsernameOrPasswordIsIncorrect | رقم الهاتف أو كلمة السر غير صحيحة |
| TheOldPasswordIsInCorrect | كلمة السر القديمة غير صحيحة |
| TheOrderNotExistOrDeleted | الشحنة غير موجودة |
| PhoneAlreadyRegistered | رقم الهاتف مسجل بالفعل |
| UserNotApproved | الحساب في انتظار الموافقة |
| InvalidUserType | نوع الحساب غير صحيح |
| OrderCannotBeEdited | لا يمكن تعديل الشحنة |
| OrderCannotBeCancelled | لا يمكن إلغاء الشحنة |

---

## Notes for Android Developer

1. **Authentication:** Use JWT Bearer token in header: `Authorization: Bearer {token}`
2. **Token** is returned in Login and Register responses - store it locally
3. **Edit/Cancel orders** only works when order is `Placed` status AND `Pending = true` (before admin approval)
4. **Barcode images** are returned as Base64 PNG strings - decode them to display
5. **Pagination** is consistent across all list endpoints: `pageNumber`, `pageSize`, response includes `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`
6. **Sender hide settings** (`hideSenderName`, `hideSenderPhone`, `hideSenderCode`): when true, the corresponding sender info returns `null` in Receipt and Order Details
7. **Swagger** is available at `/swagger` for interactive API testing
