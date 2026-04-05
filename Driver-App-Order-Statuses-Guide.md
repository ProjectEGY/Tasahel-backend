<div dir="rtl">

# دليل مطور تطبيق المندوب - حالات الأوردر والتقفيلات

---

## أولا: المشكلة الحالية في تحديث حالة الطلب

التطبيق حاليا بيعرض 4 حالات فقط عند تحديث الشحنة:
- تسليم
- مرتجع
- تسليم جزئي
- مرفوض

**المفروض يكون فيه 7 حالات** يقدر المندوب يختار منها عند تحديث حالة الأوردر (حالة "مرفوض" خاصة بالأدمن والمشرف فقط).

---

## الـ Enum الكامل لحالات الأوردر

```
OrderStatus {
    Placed = 0,                              // جديد (لا يستخدمه المندوب)
    Assigned = 1,                            // جارى التوصيل (لا يستخدمه المندوب)
    Delivered = 2,                           // ✅ تم التوصيل
    Waiting = 3,                             // ✅ مؤجل
    Rejected = 4,                            // ❌ مرفوض (خاص بالأدمن والمشرف فقط)
    Finished = 5,                            // منتهي (لا يستخدمه المندوب)
    Completed = 6,                           // تم تسويته (لا يستخدمه المندوب)
    PartialDelivered = 7,                    // ✅ تسليم جزئي
    Returned = 8,                            // ✅ مرتجع كامل
    PartialReturned = 9,                     // مرتجع جزئي (يتولد تلقائي من السيستم)
    Delivered_With_Edit_Price = 10,           // ✅ تم التوصيل مع تعديل السعر
    Returned_And_Paid_DeliveryCost = 11,     // ✅ مرتجع ودفع شحن
    Returned_And_DeliveryCost_On_Sender = 12 // ✅ مرتجع وشحن على الراسل
}
```

---

## الحالات اللي المندوب يختار منها (7 حالات)

المندوب لما بيفتح أوردر ويدوس "تحديث حالة الطلب"، المفروض يشوف الحالات دي:

### 1. تسليم (Delivered = 2)
- **الوصف:** الشحنة اتسلمت بنجاح والمستلم دفع المبلغ كامل
- **المبلغ المدفوع (Paid):** مش مطلوب - السيستم بيحسبه تلقائي = TotalCost
- **الصورة:** اختيارية

### 2. تسليم مع تعديل السعر (Delivered_With_Edit_Price = 10)
- **الوصف:** الشحنة اتسلمت بس المستلم دفع مبلغ مختلف عن المبلغ الأصلي (أقل أو أكثر)
- **المبلغ المدفوع (Paid):** مطلوب - المندوب يكتب المبلغ الفعلي اللي حصّله
- **الصورة:** اختيارية
- **ملاحظة:** الحالة دي مهمة لما المستلم بيرفض يدفع المبلغ كامل أو بيكون في خصم

### 3. تسليم جزئي (PartialDelivered = 7)
- **الوصف:** المستلم استلم جزء من الشحنة ورجع الباقي
- **المبلغ المدفوع (Paid):** مطلوب - المندوب يكتب المبلغ اللي المستلم دفعه عن الجزء اللي استلمه
- **الصورة:** اختيارية
- **ملاحظة:** السيستم بيعمل أوردر مرتجع جزئي تلقائي بكود R + كود الأوردر الأصلي

### 4. مرتجع كامل (Returned = 8)
- **الوصف:** المستلم رفض الشحنة بالكامل ومدفعش حاجة
- **المبلغ المدفوع (Paid):** مش مطلوب (بيتحسب = 0 تلقائي)
- **الصورة:** اختيارية

### 5. مرتجع ودفع شحن (Returned_And_Paid_DeliveryCost = 11)
- **الوصف:** المستلم رفض الشحنة بس دفع تكلفة الشحن (أو جزء منها)
- **المبلغ المدفوع (Paid):** مطلوب - المبلغ اللي المستلم دفعه كتكلفة شحن
  - لو Paid = 0: بيتعامل معاه زي "مرتجع وشحن على الراسل"
  - لو Paid > 0: المبلغ ده بيتسجل كمبلغ محصل
- **الصورة:** اختيارية

### 6. مرتجع وشحن على الراسل (Returned_And_DeliveryCost_On_Sender = 12)
- **الوصف:** المستلم رفض الشحنة وتكلفة الشحن بتتحسب على الراسل (صاحب الشحنة)
- **المبلغ المدفوع (Paid):** مش مطلوب (بيتحسب = 0 تلقائي)
- **الصورة:** اختيارية

### 7. مؤجل (Waiting = 3)
- **الوصف:** المستلم مش موجود أو طلب تأجيل الاستلام لوقت تاني
- **المبلغ المدفوع (Paid):** مش مطلوب
- **الصورة:** اختيارية
- **ملاحظة:** الأوردر بيفضل عند المندوب وبيقدر يحدث حالته تاني بعدها

> **ملاحظة:** حالة "مرفوض" (Rejected = 4) خاصة بالأدمن والمشرف فقط ولا تظهر للمندوب.

---

## API تحديث حالة الأوردر

### تحديث حالة أوردر واحد

```
PUT /api/Orders/Finshed
Authorization: Bearer {JWT_TOKEN}
Headers:
  - Latitude: double (اختياري - لتحديث موقع المندوب)
  - Longitude: double (اختياري - لتحديث موقع المندوب)

Body (JSON):
{
    "orderId": 12345,           // long - رقم الأوردر
    "status": 2,                // int - رقم الحالة من الـ enum
    "paid": 400.0,              // double - المبلغ المدفوع (مطلوب في بعض الحالات)
    "note": "ملاحظة المندوب",   // string - ملاحظات (اختياري)
    "image": "base64string..."  // string - صورة بصيغة Base64 (اختياري)
}
```

### جدول القيم لكل حالة

| الحالة | Status (int) | الـ Paid مطلوب؟ | قيمة الـ Paid |
|--------|-------------|----------------|--------------|
| تسليم | 2 | لا | يتجاهل (= TotalCost تلقائي) |
| تسليم مع تعديل السعر | 10 | نعم | المبلغ الفعلي المحصل |
| تسليم جزئي | 7 | نعم | المبلغ المحصل عن الجزء المسلم |
| مرتجع كامل | 8 | لا | يتجاهل (= 0 تلقائي) |
| مرتجع ودفع شحن | 11 | نعم | مبلغ الشحن المدفوع |
| مرتجع وشحن على الراسل | 12 | لا | يتجاهل (= 0 تلقائي) |
| مؤجل | 3 | لا | - |

---

## تصميم واجهة المندوب المقترح

### شاشة تحديث حالة الطلب

```
┌─────────────────────────────────────┐
│        تحديث حالة الطلب      ✓      │
│                                     │
│           اختر الحالة               │
│                                     │
│  ✅ تسليم          🔄 تسليم جزئي    │
│                                     │
│  💰 تسليم مع       ↩️ مرتجع كامل   │
│     تعديل السعر                     │
│                                     │
│  🚚 مرتجع          📦 مرتجع وشحن   │
│     ودفع شحن          على الراسل    │
│                                     │
│  ⏳ مؤجل                            │
│                                     │
│  ─────────────────────────────────  │
│  المبلغ المدفوع: [________] ج.م     │
│  (يظهر فقط في: تسليم مع تعديل      │
│   السعر، تسليم جزئي، مرتجع ودفع    │
│   شحن)                              │
│                                     │
│  ملاحظات: [____________________]    │
│                                     │
│  📷 إرفاق صورة (اختياري)            │
│                                     │
│ ┌─────────────┐ ┌─────────────────┐ │
│ │  ❌ إلغاء   │ │  ✅ تأكيد       │ │
│ └─────────────┘ └─────────────────┘ │
└─────────────────────────────────────┘
```

### متى يظهر حقل "المبلغ المدفوع"؟

حقل المبلغ المدفوع يظهر **فقط** عند اختيار:
- تسليم مع تعديل السعر (Delivered_With_Edit_Price = 10)
- تسليم جزئي (PartialDelivered = 7)
- مرتجع ودفع شحن (Returned_And_Paid_DeliveryCost = 11)

ويكون **مخفي** في باقي الحالات لأن المبلغ بيتحسب تلقائي.

---

## ثانيا: سجل التقفيلات

التقفيلة = المشرف المميز بياخد من المندوب مبالغ الطلبات اللي حصّلها ويديله نسبته (عمولته).

### 1. قائمة التقفيلات

```
GET /api/Account/Settlements?pageNumber=1&pageSize=20
Authorization: Bearer {JWT_TOKEN}

Response:
{
    "errorCode": 0,
    "errorMessage": "Success",
    "data": {
        "items": [
            {
                "id": 42,
                "amount": 3500.0,
                "totalDriverCommission": 450.0,
                "totalCollected": 3950.0,
                "totalToCompany": 3500.0,
                "date": "2026-04-04T20:00:00",
                "orderCount": 12,
                "settledBy": "أحمد المشرف",
                "settledAt": "2026-04-04 10:00 PM",
                "note": null,
                "deliveredCount": 7,
                "deliveredWithEditPriceCount": 1,
                "partialDeliveredCount": 1,
                "returnedCount": 2,
                "returnedPaidDeliveryCount": 1,
                "returnedOnSenderCount": 0,
                "rejectedCount": 0,
                "ordersSummary": [
                    {
                        "code": "Tas237538",
                        "clientName": "محمد أحمد",
                        "arrivedCost": 400.0,
                        "driverCommission": 30.0,
                        "status": 2,
                        "statusArabic": "تم التوصيل"
                    },
                    {
                        "code": "Tas239373",
                        "clientName": "علي عبد العال",
                        "arrivedCost": 0.0,
                        "driverCommission": 30.0,
                        "status": 8,
                        "statusArabic": "مرتجع كامل"
                    }
                ]
            }
        ],
        "pageNumber": 1,
        "pageSize": 20,
        "totalCount": 5,
        "totalPages": 1,
        "hasPreviousPage": false,
        "hasNextPage": false
    }
}
```

**شرح الحقول:**

| الحقل | الوصف |
|-------|-------|
| `id` | رقم التقفيلة |
| `amount` | إجمالي مبلغ التقفيلة |
| `totalDriverCommission` | إجمالي نسبة (عمولة) المندوب في التقفيلة |
| `totalCollected` | إجمالي المبالغ المحصلة من العملاء |
| `totalToCompany` | المبلغ اللي المندوب سلمه للشركة (المحصل - العمولة) |
| `date` | تاريخ التقفيلة |
| `orderCount` | عدد الطلبات في التقفيلة |
| `settledBy` | اسم المشرف اللي عمل التقفيلة |
| `settledAt` | ميعاد التقفيلة بتوقيت مصر |
| `note` | ملاحظات (اختياري) |
| `deliveredCount` | عدد الطلبات اللي اتسلمت |
| `deliveredWithEditPriceCount` | عدد الطلبات اللي اتسلمت مع تعديل سعر |
| `partialDeliveredCount` | عدد الطلبات اللي اتسلمت جزئي |
| `returnedCount` | عدد الطلبات المرتجعة كامل |
| `returnedPaidDeliveryCount` | عدد الطلبات المرتجعة ودفع شحن |
| `returnedOnSenderCount` | عدد الطلبات المرتجعة وشحن على الراسل |
| `rejectedCount` | عدد الطلبات المرفوضة |
| `ordersSummary` | ملخص كل طلب في التقفيلة (الكود، اسم المستلم، المحصل، عمولة المندوب، الحالة) |

---

### 2. تفاصيل تقفيلة معينة

```
GET /api/Account/Settlements/{walletId}
Authorization: Bearer {JWT_TOKEN}

Response:
{
    "errorCode": 0,
    "errorMessage": "Success",
    "data": {
        "settlement": {
            "id": 42,
            "amount": 3500.0,
            "totalDriverCommission": 450.0,
            "totalCollected": 3950.0,
            "totalToCompany": 3500.0,
            "date": "2026-04-04T20:00:00",
            "orderCount": 3,
            "settledBy": "أحمد المشرف",
            "settledAt": "2026-04-04 10:00 PM",
            "note": null,
            "deliveredCount": 2,
            "deliveredWithEditPriceCount": 0,
            "partialDeliveredCount": 0,
            "returnedCount": 1,
            "returnedPaidDeliveryCount": 0,
            "returnedOnSenderCount": 0,
            "rejectedCount": 0,
            "ordersSummary": null
        },
        "orders": [
            {
                "code": "Tas237538",
                "clientName": "محمد أحمد",
                "clientPhone": "01012345678",
                "senderName": "شركة ABC",
                "arrivedCost": 400.0,
                "deliveryCost": 30.0,
                "driverCommission": 30.0,
                "cost": 370.0,
                "totalCost": 400.0,
                "status": 2,
                "statusArabic": "تم التوصيل"
            },
            {
                "code": "Tas239373",
                "clientName": "علي عبد العال",
                "clientPhone": "01098765432",
                "senderName": "شركة ABC",
                "arrivedCost": 410.0,
                "deliveryCost": 30.0,
                "driverCommission": 30.0,
                "cost": 380.0,
                "totalCost": 410.0,
                "status": 2,
                "statusArabic": "تم التوصيل"
            },
            {
                "code": "Tas240100",
                "clientName": "سعيد محمود",
                "clientPhone": "01155566677",
                "senderName": "شركة XYZ",
                "arrivedCost": 0.0,
                "deliveryCost": 0.0,
                "driverCommission": 0.0,
                "cost": 250.0,
                "totalCost": 250.0,
                "status": 8,
                "statusArabic": "مرتجع كامل"
            }
        ]
    }
}
```

**شرح حقول الطلب في التفاصيل:**

| الحقل | الوصف |
|-------|-------|
| `code` | كود الطلب |
| `clientName` | اسم المستلم |
| `clientPhone` | رقم هاتف المستلم |
| `senderName` | اسم الراسل (صاحب الشحنة) |
| `arrivedCost` | المبلغ المحصل من المستلم |
| `deliveryCost` | تكلفة التوصيل |
| `driverCommission` | نسبة (عمولة) المندوب في الطلب |
| `cost` | تكلفة المنتج |
| `totalCost` | الإجمالي |
| `status` | رقم الحالة (enum value) |
| `statusArabic` | الحالة بالعربي |

---

## ثالثا: طلبات في انتظار التقفيل

الطلبات اللي المندوب غيّر حالتها (سلّم أو رجّع) بس لسه ما اتعملهاش تقفيلة مع المشرف.

```
GET /api/Account/PendingSettlement?page=1&size=15
Authorization: Bearer {JWT_TOKEN}
Headers:
  - Latitude: double (اختياري)
  - Longitude: double (اختياري)

Response:
{
    "errorCode": 0,
    "errorMessage": "Success",
    "data": {
        "statistics": {
            "totalOrders": 25,
            "deliveredCount": 15,
            "deliveredWithEditPriceCount": 2,
            "partialDeliveredCount": 1,
            "returnedCount": 4,
            "returnedPaidDeliveryCount": 1,
            "returnedOnSenderCount": 1,
            "rejectedCount": 1,
            "totalCollected": 8500.0,
            "totalDriverCommission": 750.0,
            "totalToCompany": 7750.0
        },
        "orders": {
            "items": [
                {
                    "id": 12345,
                    "code": "Tas237538",
                    "clientName": "محمد أحمد",
                    "clientPhone": "01012345678",
                    "address": "المعادي - شارع 9",
                    "addressCity": "القاهرة",
                    "cost": 370.0,
                    "deliveryFees": 30.0,
                    "totalCost": 400.0,
                    "arrivedCost": 400.0,
                    "deliveryCost": 30.0,
                    "status": 2,
                    "finished": false,
                    "notes": "ملاحظة"
                }
            ],
            "pageNumber": 1,
            "pageSize": 15,
            "totalCount": 25,
            "totalPages": 2,
            "hasPreviousPage": false,
            "hasNextPage": true
        }
    }
}
```

**شرح حقول الإحصائيات:**

| الحقل | الوصف |
|-------|-------|
| `totalOrders` | إجمالي عدد الطلبات اللي مستنية تقفيل |
| `deliveredCount` | عدد الطلبات المسلمة |
| `deliveredWithEditPriceCount` | عدد الطلبات المسلمة مع تعديل سعر |
| `partialDeliveredCount` | عدد الطلبات المسلمة جزئي |
| `returnedCount` | عدد الطلبات المرتجعة |
| `returnedPaidDeliveryCount` | عدد الطلبات المرتجعة ودفع شحن |
| `returnedOnSenderCount` | عدد الطلبات المرتجعة وشحن على الراسل |
| `rejectedCount` | عدد الطلبات المرفوضة |
| `totalCollected` | إجمالي المبالغ المحصلة |
| `totalDriverCommission` | إجمالي نسبة المندوب |
| `totalToCompany` | المبلغ المطلوب تسليمه للشركة |

---

## ملاحظات مهمة للمطور

1. **الأوردر لازم يكون في حالة Assigned (1) أو Waiting (3) عشان المندوب يقدر يحدث حالته.** لو الأوردر في أي حالة تانية، الـ API هيرجع Success بس مش هيحصل تحديث فعلي.

2. **حقل الـ `paid` مهم جدا** - لازم يتبعت بالقيمة الصح في الحالات اللي بتحتاجه، لو مبعتش قيمة هيبقى 0 وده ممكن يأثر على الحسابات.

3. **الصورة `image`** بتتبعت كـ Base64 string، مش كـ file upload.

4. **الـ API بيبعت إشعار تلقائي** للراسل (صاحب الشحنة) عبر Push Notification + رسالة واتساب عند تحديث الحالة.

5. **في حالة التسليم الجزئي (PartialDelivered)** السيستم بيعمل أوردر مرتجع جزئي جديد تلقائي بكود `R` + كود الأوردر الأصلي.

6. **الفرق بين التقفيلة وطلبات انتظار التقفيل:**
   - **طلبات انتظار التقفيل** (`PendingSettlement`): طلبات المندوب غيّر حالتها بس لسه المشرف ما قفلش معاه
   - **التقفيلات** (`Settlements`): طلبات المشرف قفل فيها مع المندوب وأخد المبالغ وأعطاه نسبته

</div>
