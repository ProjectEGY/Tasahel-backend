# خطة التحديثات - Tasahel Express Backend

**التاريخ:** 2026-07-01  
**العميل:** Tasahel Express Admin  
**المطور:** Ashraf Kotb  

---

## ملخص المحادثة مع العميل

العميل طلب ميزتين أساسيتين:
1. **تقفيل الطلبات بالاكسيل شيت** - بدل ما يقفل أوردر أوردر يدوي (بياخد 5-6 ساعات) يرفع شيت مرة واحدة يخلص في دقيقة
2. **استلام مخزن / الطلبات المعلقة** - اكاونتات الداتا إنتري لما ترفع شحنات تروح "معلقة" بدل ما تتقبل أوتوماتيك

---

## الميزة الأولى: تقفيل الطلبات بالاكسيل شيت (Bulk Settlement via Excel)

### المشكلة

الوكلاء بيبعتوا تقفيلات كبيرة، والإدمن بيدخل يقفل أوردر أوردر يدوي من صفحة بيان تحميل المندوب.
الشغل اللي بياخد 5-6 ساعات المفروض يخلص في دقيقة.

### ملف الاكسيل (Template)

الملف اللي العميل بعته: `فورمه شيت التقفيل.xlsx`

| العمود | الوصف | ملاحظات |
|--------|-------|---------|
| كود الشحنه | كود الأوردر في السيستم | مطلوب - يُستخدم للبحث عن الطلب |
| سعر الطلب | المبلغ اللي وصل فعلياً (ArrivedCost) | مطلوب - يُقارن بسعر السيستم |
| حاله الشحنه | الحالة النهائية للشحنة | مطلوب - من الحالات المدعومة |

### الحالات المدعومة

| اسم الحالة بالعربي (في الاكسيل) | OrderStatus Enum | القيمة |
|----------------------------------|------------------|--------|
| تم التوصيل | `Delivered` | 2 |
| تسليم جزئي | `PartialDelivered` | 7 |
| تم التوصيل مع تعديل السعر | `Delivered_With_Edit_Price` | 10 |
| مرتجع كامل | `Returned` | 8 |
| مرتجع ودفع شحن | `Returned_And_Paid_DeliveryCost` | 11 |
| مرتجع وشحن على الراسل | `Returned_And_DeliveryCost_On_Sender` | 12 |

### ملاحظات العميل من المحادثة

- الطلبات ممكن تكون **لكل المناديب** (مش مندوب واحد بس)
- المفروض تشتغل **كأنك بتقفل لمندوب** (نفس flow التقفيل اليدوي)
- لو فيه **غلط في السعر** السيستم ينبه (مثلاً السعر في الشيت غير اللي في السيستم)
- التنبيه يقول **السطر الكام** فيه المشكلة
- الحالات = **كل الحالات** الموجودة في السيستم

### خطة التنفيذ التفصيلية

#### 1. Backend - Controller Action

**الملف:** `PostexS/Controllers/UsersController.cs` أو `OrdersController.cs`  
**الاسم:** `BulkFinishFromExcel`  
**الصلاحية:** `Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")`

**المدخلات:**
- ملف اكسيل (IFormFile)

**الخطوات:**

```
1. استقبال الملف والتحقق من الامتداد (.xlsx / .xls)
2. قراءة الملف بـ ClosedXML (مكتبة موجودة أصلاً في المشروع)
3. لكل سطر في الاكسيل:
   أ. البحث عن الطلب بالكود (Order.Code)
   ب. لو الكود مش موجود → إضافة للأخطاء
   ج. لو الطلب مقفل أصلاً (Finished == true) → إضافة للتحذيرات + تخطي
   د. لو الطلب محذوف (IsDeleted) → إضافة للأخطاء
   هـ. مقارنة السعر: لو سعر الاكسيل ≠ TotalCost → إضافة للتحذيرات (مع رقم السطر)
   و. تحويل اسم الحالة العربي → OrderStatus enum
   ز. لو الحالة مش معروفة → إضافة للأخطاء
4. تجميع الطلبات الصالحة حسب المندوب (DeliveryId)
5. لكل مندوب:
   أ. إنشاء Wallet جديد (InitializeWallet) 
   ب. تطبيق نفس لوجيك FinshedOrders:
      - تحديث حالة الطلب
      - تحديث ArrivedCost / DeliveryCost
      - ربط الطلب بالـ Wallet
      - تحديث Finished / ReturnedFinished
      - تحديث OrderOperationHistory
      - إنشاء PartialReturned order لو الحالة تسليم جزئي
   ج. حساب الإجمالي وتحديث Wallet.Amount
   د. تحديث محفظة المندوب (user.Wallet)
6. إرجاع التقرير النهائي
```

#### 2. نموذج النتيجة (Response Model)

```csharp
public class BulkSettlementResult
{
    public int TotalRows { get; set; }           // إجمالي السطور في الاكسيل
    public int SuccessCount { get; set; }         // عدد الطلبات اللي اتقفلت بنجاح
    public int ErrorCount { get; set; }           // عدد الأخطاء
    public int WarningCount { get; set; }         // عدد التحذيرات
    public int SkippedCount { get; set; }         // عدد المتخطى (مقفل سابقاً)
    public List<BulkSettlementError> Errors { get; set; }
    public List<BulkSettlementWarning> Warnings { get; set; }
    public List<WalletSummary> WalletsSummary { get; set; }  // ملخص لكل مندوب
}

public class BulkSettlementError
{
    public int RowNumber { get; set; }            // رقم السطر في الاكسيل
    public string OrderCode { get; set; }         // كود الشحنة
    public string Message { get; set; }           // وصف الخطأ
}

public class BulkSettlementWarning
{
    public int RowNumber { get; set; }
    public string OrderCode { get; set; }
    public string Message { get; set; }           // مثلاً: "السعر في الشيت 150 والسيستم 200"
}

public class WalletSummary
{
    public long WalletId { get; set; }
    public string DriverName { get; set; }
    public int OrdersCount { get; set; }
    public double TotalAmount { get; set; }
}
```

#### 3. مرحلتين للتنفيذ (Validate → Confirm)

**المرحلة 1 - التحقق (Validate):**
- رفع الملف → قراءته → عرض تقرير التحقق (أخطاء + تحذيرات + ملخص)
- لو فيه تحذيرات سعر → يظهرها للمستخدم ويسأله "هل تريد المتابعة؟"

**المرحلة 2 - التأكيد (Confirm):**
- بعد مراجعة التقرير → المستخدم يضغط "تأكيد التقفيل"
- يتم تنفيذ التقفيل الفعلي داخل TransactionScope

#### 4. View - صفحة الرفع

**الملف:** `PostexS/Views/Orders/BulkSettlement.cshtml`

**العناصر:**
- زرار رفع ملف اكسيل
- زرار تحميل Template فاضي
- جدول نتائج التحقق (أخطاء بالأحمر / تحذيرات بالأصفر / نجاح بالأخضر)
- ملخص: عدد الناجح / الأخطاء / التحذيرات
- زرار "تأكيد التقفيل" (يظهر بعد نجاح التحقق)
- جدول ملخص المحافظ المُنشأة (لكل مندوب)

#### 5. إضافة رابط في القائمة الجانبية

**الملف:** `PostexS/Views/Partials/AdminSideMenu.cshtml`

إضافة رابط "تقفيل بالاكسيل" تحت قسم التقفيلات

### ربط الميزة بالكود الحالي

| الكود الحالي | الاستخدام في الميزة الجديدة |
|-------------|----------------------------|
| `FinshedOrders()` في `UsersController.cs:472` | نفس اللوجيك هيتطبق لكن بشكل bulk |
| `InitializeWallet()` في `UsersController.cs:1222` | إنشاء Wallet لكل مندوب |
| `ValidateOrdersBelongToSameDriver()` في `UsersController.cs:1237` | مش هنستخدمها - لأن الشحنات لكل المناديب |
| `PendingDeliveriesSettlement()` في `OrdersController.cs:4207` | مرجع لفلترة الطلبات الجاهزة للتقفيل |
| `OrderStatus` enum في `Models/Enums/OrderStatus.cs` | mapping من الاسم العربي للـ enum |
| `ClosedXML` (مكتبة موجودة) | قراءة ملف الاكسيل |

### حالات خاصة يجب معالجتها

1. **تسليم جزئي (PartialDelivered):**
   - بيتعمل أوردر عكسي (PartialReturned) بكود `R + الكود الأصلي`
   - ArrivedCost = القيمة من الشيت
   - ReturnedCost = TotalCost - ArrivedCost

2. **مرتجع كامل (Returned):**
   - DeliveryCost = 0, ArrivedCost = 0
   - ReturnedCost = TotalCost
   - الطلب **لا يُربط** بالـ Wallet (goto Skip في الكود الحالي)

3. **مرتجع ودفع شحن / مرتجع وشحن على الراسل:**
   - يُربط بـ `ReturnedWalletId` بدل `WalletId`
   - يُعلم بـ `ReturnedFinished = true` بدل `Finished = true`

4. **تعديل السعر (Delivered_With_Edit_Price):**
   - ArrivedCost = القيمة من الشيت (اللي هي مختلفة عن TotalCost)

5. **مؤجل (Waiting):**
   - DeliveryId = null (يتشال من المندوب)
   - DeliveryCost = 0

---

## الميزة الثانية: استلام مخزن / الطلبات المعلقة (Warehouse Receipt)

### المشكلة

اكاونتات الداتا إنتري والأوبريشن لما بيرفعوا شحنات (سواء يدوي أو بالاكسيل) بتنزل في السيستم **مقبولة مباشرة**.
الإدارة مش بتعرف إيه اللي جه فعلياً في المخزن وإيه اللي لسه.
بالذات في الرواسل أو الشركات اللي بتبعت pickups كبيرة.

### المطلوب

1. **خاصية في إعدادات اليوزر:** checkbox "الطلبات تروح للمعلق" (`OrdersGoToPending`)
2. **صفحة استلام مخزن:** الأدمن يشوف الطلبات المعلقة ويقبلها أو يرفضها

### الحالة الحالية في الكود

- خاصية `Pending` **موجودة أصلاً** على `Order` model (`PostexS/Models/Domain/Order.cs:24`)
- الـ SenderApp **بيستخدمها فعلاً** - لما الراسل يرفع أوردر بتنزل `Pending = true`
- الأوردرات المعلقة **بتتفلتر** من معظم الصفحات (`!x.Pending`)
- تعديل الأوردر المعلق **متاح** قبل القبول فقط
- **اللي ناقص:** الاكاونتات الداخلية (DataEntry/Operations) مش بتفعل الـ Pending

### خطة التنفيذ التفصيلية

#### 1. تعديل Model - ApplicationUser

**الملف:** `PostexS/Models/Domain/ApplicationUser.cs`

```csharp
// إضافة خاصية جديدة
public bool OrdersGoToPending { get; set; } = false;
```

#### 2. Migration

```
Add-Migration AddOrdersGoToPendingToUser
Update-Database
```

#### 3. تعديل لوجيك إنشاء الأوردرات

**الأماكن اللي محتاجة تعديل:**

| الملف | الميثود | الوصف |
|-------|---------|-------|
| `UsersController.cs` | `createOrders()` POST (سطر ~2083) | رفع الأوردرات بالاكسيل |
| `OrdersController.cs` | `Create()` POST | إنشاء أوردر يدوي |
| `BranchsController.cs` | أي مكان بيتعمل فيه أوردر | التحقق من الفلاج |

**اللوجيك:**
```csharp
// قبل حفظ الأوردر
var creatingUser = await _user.GetObj(x => x.Id == userId);
if (creatingUser.OrdersGoToPending)
{
    order.Pending = true;
}
```

#### 4. صفحة استلام مخزن (Warehouse Receipt)

**Controller Action:** `OrdersController.cs`

```csharp
[Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
public IActionResult WarehouseReceipt()
{
    // عرض كل الطلبات المعلقة (Pending = true)
    // مع فلتر بالراسل (Client) واليوزر اللي رفعها
    // مع إمكانية القبول أو الرفض
}

[HttpPost]
public async Task<IActionResult> ApproveOrders(List<long> orderIds)
{
    // قبول: Pending = false
}

[HttpPost]
public async Task<IActionResult> RejectOrders(List<long> orderIds)
{
    // رفض: IsDeleted = true أو حالة خاصة
}
```

#### 5. View - صفحة استلام مخزن

**الملف:** `PostexS/Views/Orders/WarehouseReceipt.cshtml`

**العناصر:**
- فلتر بالراسل / التاريخ / اليوزر اللي رفع
- جدول الطلبات المعلقة (كود - راسل - عنوان - سعر - تاريخ الرفع - مَن رفعها)
- Checkbox لاختيار طلبات متعددة
- زرار "قبول المحدد" (Accept Selected)
- زرار "قبول الكل" (Accept All)
- زرار "رفض المحدد" (Reject Selected)
- إحصائيات: عدد المعلق / إجمالي السعر

#### 6. تعديل صفحة تعديل اليوزر

**الملف:** `PostexS/Views/Users/Edit.cshtml`

إضافة checkbox:
```html
<div class="form-group">
    <label>
        <input type="checkbox" asp-for="OrdersGoToPending" />
        الطلبات تروح للمعلق (تحتاج موافقة قبل الظهور في النظام)
    </label>
</div>
```

#### 7. إضافة رابط في القائمة الجانبية

**الملف:** `PostexS/Views/Partials/AdminSideMenu.cshtml`

إضافة رابط "استلام مخزن" مع badge بعدد الطلبات المعلقة

### ربط الميزة بالكود الحالي

| الكود الحالي | الاستخدام |
|-------------|-----------|
| `Order.Pending` في `Order.cs:24` | موجودة - هنستخدمها زي ما هي |
| فلترة `!x.Pending` في الصفحات | موجودة - الطلبات المعلقة مش هتظهر في الصفحات العادية |
| SenderApp Pending workflow | مرجع - نفس المبدأ هيتطبق |
| `Edit.cshtml` للمستخدمين | هنضيف فيها checkbox الـ OrdersGoToPending |

---

## ترتيب الأولويات

| الأولوية | الميزة | السبب |
|----------|--------|-------|
| 1 (عاجل) | تقفيل بالاكسيل | العميل قال "بتعطل الدنيا جداً" - 5-6 ساعات → دقيقة |
| 2 | استلام مخزن | مهمة لكن أقل إلحاحاً |

---

## ملاحظات فنية عامة

### المكتبات المستخدمة
- **ClosedXML** - لقراءة/كتابة ملفات الاكسيل (موجودة أصلاً في المشروع)
- **TransactionScope** - لضمان atomicity التقفيل الجماعي (مستخدم أصلاً في `FinshedOrders`)

### الأمان
- كل الـ Actions محمية بـ `Authorize` مع الأدوار المناسبة
- التقفيل بالاكسيل يكون `HighAdmin` فقط (زي `FinshedOrders` الحالي)
- استلام المخزن يكون `Admin,HighAdmin,Accountant,TrustAdmin`

### الأداء
- التقفيل الجماعي لازم يكون داخل `TransactionScope` عشان لو فيه مشكلة يعمل rollback
- ممكن نستخدم `BulkUpdate` لو عدد الأوردرات كبير جداً
- يُفضل عمل validation كامل الأول قبل أي تعديل في الداتابيز

### الإشعارات
- بعد التقفيل الجماعي → إرسال إشعارات WhatsApp + Push Notifications (زي `FinshedOrders` الحالي)
- إشعارات للراسلين بالتسوية
- إشعارات للمندوب بالتقفيلة

---

## الملفات اللي هتتعدل / تتضاف

### ملفات جديدة
| الملف | الوصف |
|-------|-------|
| `Views/Orders/BulkSettlement.cshtml` | صفحة تقفيل بالاكسيل |
| `Views/Orders/WarehouseReceipt.cshtml` | صفحة استلام مخزن |
| `Models/ViewModels/BulkSettlementVM.cs` | ViewModel للتقفيل الجماعي |

### ملفات هتتعدل
| الملف | التعديل |
|-------|---------|
| `Models/Domain/ApplicationUser.cs` | إضافة `OrdersGoToPending` |
| `Controllers/UsersController.cs` أو `OrdersController.cs` | إضافة `BulkFinishFromExcel` action |
| `Controllers/OrdersController.cs` | إضافة `WarehouseReceipt` + `ApproveOrders` + `RejectOrders` |
| `Controllers/UsersController.cs` | تعديل `createOrders` لتفعيل Pending حسب الفلاج |
| `Views/Users/Edit.cshtml` | إضافة checkbox `OrdersGoToPending` |
| `Views/Partials/AdminSideMenu.cshtml` | إضافة روابط الصفحات الجديدة |
| `Migrations/` | migration جديد لـ `OrdersGoToPending` |
