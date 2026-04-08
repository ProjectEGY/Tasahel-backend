# تحديثات تطبيق المندوب - 6 أبريل 2026

## Base URL: `api/Account`
## كل الـ endpoints تحتاج `Authorization: Bearer {JWT_TOKEN}`

---

## 1. إصلاح: حقل `arrivedCost` في كل الطلبات (OrderDto)

**المشكلة:** كان كل طلب بيرجع `arrivedCost = 0` لأن الحقل مكانش موجود في الـ DTO.

**الحل:** تم إضافة `arrivedCost` في `OrderDto` - دلوقتي كل endpoint بيرجع طلبات (CurrentOrders, PendingSettlement, OrderHistory, PendingDeliveries, PendingReturns) هيرجع `arrivedCost` بالقيمة الصحيحة.

**الحقل الجديد في كل order object:**
```json
{
  "id": 1,
  "orderNumber": "ORD-001",
  "cost": 200.0,
  "arrivedCost": 180.0,    // <-- جديد - المبلغ المحصل فعلياً
  "deliveryCost": 30.0,
  "orderCostWithoutDeliveryCost": 170.0,
  "status": 2,
  ...
}
```

---

## 2. جديد: في انتظار التقفيل - توصيل (PendingDeliveries)

### `GET /api/Account/PendingDeliveries?page=1&size=15`

الطلبات اللي المندوب سلمها أو عدّل حالتها بس لسه ما اتقفلتش مع المشرف.

**Headers:**
| Header | Type | Required |
|--------|------|----------|
| Authorization | Bearer Token | نعم |
| Latitude | double | اختياري |
| Longitude | double | اختياري |

**Query Parameters:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| page | 1 | رقم الصفحة |
| size | 15 | عدد الطلبات في الصفحة |

**الحالات المشمولة:**
| الحالة | Status Code | الوصف |
|--------|-------------|-------|
| تم التوصيل | `Delivered = 2` | المندوب سلم الطلب |
| تم التوصيل مع تعديل السعر | `Delivered_With_Edit_Price = 10` | سلم مع تعديل المبلغ |
| تسليم جزئي | `PartialDelivered = 8` | سلم جزء من الطلب |
| مؤجل | `Waiting = 7` | المندوب أجّل الطلب |

**Response:**
```json
{
  "errorCode": 0,
  "errorMessage": null,
  "data": {
    "statistics": {
      "totalOrders": 5,
      "deliveredCount": 2,
      "deliveredWithEditPriceCount": 0,
      "partialDeliveredCount": 1,
      "waitingCount": 2,
      "totalCollected": 440.0,
      "totalDriverCommission": 150.0,
      "totalToCompany": 290.0
    },
    "orders": {
      "items": [
        {
          "id": 1,
          "orderNumber": "ORD-001",
          "agentName": "محمد",
          "date": "الأحد أبريل, 2026",
          "cost": 200.0,
          "arrivedCost": 180.0,
          "deliveryCost": 30.0,
          "orderCostWithoutDeliveryCost": 170.0,
          "senderName": "محمد",
          "senderNumber": "01xxxxxxxxx",
          "reciverCode": "C001",
          "reciverName": "أحمد",
          "reciverNumber": "01xxxxxxxxx",
          "status": 2,
          "address": "القاهرة"
        }
      ],
      "pageNumber": 1,
      "pageSize": 15,
      "totalCount": 5,
      "totalPages": 1,
      "hasPreviousPage": false,
      "hasNextPage": false
    }
  }
}
```

**شرح الإحصائيات:**
| الحقل | الوصف |
|-------|-------|
| `totalOrders` | إجمالي عدد الطلبات في انتظار التقفيل |
| `deliveredCount` | عدد الطلبات تم التوصيل |
| `deliveredWithEditPriceCount` | عدد الطلبات تم التوصيل مع تعديل السعر |
| `partialDeliveredCount` | عدد الطلبات تسليم جزئي |
| `waitingCount` | عدد الطلبات المؤجلة |
| `totalCollected` | إجمالي المبلغ المحصل من العملاء |
| `totalDriverCommission` | إجمالي عمولة المندوب (مجموع deliveryCost) |
| `totalToCompany` | المبلغ المطلوب تسليمه للشركة = المحصل - العمولة |

---

## 3. جديد: مرتجعات في انتظار التقفيل (PendingReturns)

### `GET /api/Account/PendingReturns?page=1&size=15`

الطلبات المرتجعة اللي لسه ما اتقفلتش مع المشرف.

**Headers و Query Parameters:** نفس PendingDeliveries

**الحالات المشمولة:**
| الحالة | Status Code | الوصف |
|--------|-------------|-------|
| مرتجع كامل | `Returned = 5` | الطلب رجع بالكامل |
| مرتجع جزئي | `PartialReturned = 9` | جزء من الطلب رجع |
| مرتجع ودفع شحن | `Returned_And_Paid_DeliveryCost = 11` | مرتجع والعميل دفع الشحن |
| مرتجع وشحن على الراسل | `Returned_And_DeliveryCost_On_Sender = 12` | مرتجع والشحن على حساب الراسل |

**Response:**
```json
{
  "errorCode": 0,
  "errorMessage": null,
  "data": {
    "statistics": {
      "totalOrders": 3,
      "returnedCount": 1,
      "partialReturnedCount": 1,
      "returnedPaidDeliveryCount": 1,
      "returnedOnSenderCount": 0,
      "totalCollected": 50.0,
      "totalDriverCommission": 90.0,
      "totalToCompany": -40.0
    },
    "orders": {
      "items": [ ... ],
      "pageNumber": 1,
      "pageSize": 15,
      "totalCount": 3,
      "totalPages": 1,
      "hasPreviousPage": false,
      "hasNextPage": false
    }
  }
}
```

---

## 4. جديد: سجل التقفيلات (Settlements)

### `GET /api/Account/Settlements?pageNumber=1&pageSize=20`

قائمة التقفيلات اللي اتعملت للمندوب (التقفيلات المكتملة).

**Query Parameters:**
| Parameter | Default | Max | Description |
|-----------|---------|-----|-------------|
| pageNumber | 1 | - | رقم الصفحة |
| pageSize | 20 | 50 | عدد التقفيلات في الصفحة |

**Response:**
```json
{
  "errorCode": 0,
  "errorMessage": null,
  "data": {
    "items": [
      {
        "id": 10,
        "amount": 500.0,
        "date": "2026-04-05T14:30:00",
        "orderCount": 8,
        "transactionType": 2,
        "note": "تقفيلة يوم السبت"
      },
      {
        "id": 7,
        "amount": 320.0,
        "date": "2026-04-03T10:00:00",
        "orderCount": 5,
        "transactionType": 2,
        "note": null
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 2,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

**شرح حقول التقفيلة:**
| الحقل | الوصف |
|-------|-------|
| `id` | معرف التقفيلة (هتستخدمه في endpoint التفاصيل) |
| `amount` | إجمالي مبلغ التقفيلة |
| `date` | تاريخ التقفيلة |
| `orderCount` | عدد الطلبات في التقفيلة |
| `transactionType` | نوع المعاملة (2 = OrderFinished) |
| `note` | ملاحظات |

---

## 5. جديد: تفاصيل تقفيلة (Settlement Details)

### `GET /api/Account/Settlements/{walletId}`

تفاصيل تقفيلة معينة مع كل الطلبات اللي جواها.

> **ملاحظة أمان:** المندوب يقدر يشوف تقفيلاته بس. لو حاول يدخل على تقفيلة مندوب تاني هيرجع 404.

**Response:**
```json
{
  "errorCode": 0,
  "errorMessage": null,
  "data": {
    "settlement": {
      "id": 10,
      "amount": 500.0,
      "date": "2026-04-05T14:30:00",
      "orderCount": 3,
      "transactionType": 2,
      "note": "تقفيلة يوم السبت"
    },
    "statistics": {
      "totalCollected": 500.0,
      "totalDriverCommission": 90.0,
      "totalToCompany": 410.0,
      "deliveredCount": 2,
      "deliveredWithEditPriceCount": 0,
      "partialDeliveredCount": 0,
      "returnedCount": 1,
      "returnedPaidDeliveryCount": 0,
      "returnedOnSenderCount": 0,
      "partialReturnedCount": 0
    },
    "orders": [
      {
        "code": "ORD-001",
        "clientName": "أحمد محمد",
        "clientPhone": "01xxxxxxxxx",
        "senderName": "شركة ABC",
        "arrivedCost": 200.0,
        "deliveryCost": 30.0,
        "cost": 170.0,
        "totalCost": 200.0,
        "status": 2,
        "statusArabic": "تم التوصيل"
      },
      {
        "code": "ORD-002",
        "clientName": "سارة أحمد",
        "clientPhone": "01xxxxxxxxx",
        "senderName": "شركة ABC",
        "arrivedCost": 0.0,
        "deliveryCost": 30.0,
        "cost": 150.0,
        "totalCost": 180.0,
        "status": 5,
        "statusArabic": "مرتجع كامل"
      }
    ]
  }
}
```

**شرح حقول الطلب في التقفيلة:**
| الحقل | الوصف |
|-------|-------|
| `code` | كود الطلب |
| `clientName` | اسم المستلم |
| `clientPhone` | رقم المستلم |
| `senderName` | اسم الراسل (التاجر) |
| `arrivedCost` | المبلغ المحصل فعلياً |
| `deliveryCost` | عمولة المندوب |
| `cost` | سعر البضاعة بدون الشحن |
| `totalCost` | التكلفة الإجمالية |
| `status` | كود الحالة (رقم) |
| `statusArabic` | الحالة بالعربي |

---

## ملخص الـ Endpoints الجديدة

| # | Method | Endpoint | الوصف |
|---|--------|----------|-------|
| 1 | GET | `/api/Account/PendingDeliveries` | طلبات توصيل في انتظار التقفيل + إحصائيات |
| 2 | GET | `/api/Account/PendingReturns` | مرتجعات في انتظار التقفيل + إحصائيات |
| 3 | GET | `/api/Account/Settlements` | سجل التقفيلات المكتملة |
| 4 | GET | `/api/Account/Settlements/{walletId}` | تفاصيل تقفيلة بالطلبات والإحصائيات |

## التعديل على الـ Endpoints القديمة

| Endpoint | التعديل |
|----------|---------|
| كل endpoint بيرجع `OrderDto` | تم إضافة حقل `arrivedCost` (المبلغ المحصل) |

> **ملاحظة:** الـ endpoint القديم `PendingSettlement` لسه شغال ومتغيرش - الـ endpoints الجديدة بديل أفضل ومنفصل (توصيل / مرتجعات).
