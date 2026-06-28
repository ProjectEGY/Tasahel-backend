# Code Review - Tasahel Backend

**تاريخ المراجعة:** 2026-06-28
**النطاق:** تعديلات التسوية (Complete/ReturnedComplete) + صفحة UserOrdersNew

---

## Bugs في الكود الجديد (لازم تتصلح قبل الرفع)

### 1. ApplyDeliveryStateFilterBase ناقصها cases
**ملف:** `PostexS/Controllers/OrdersController.cs` ~line 954
**خطورة:** عالية

`ApplyDeliveryStateFilterBase` ناقصها `"all"` و `"wasaleteditprice"` اللي موجودين في `ApplyClientStateFilterBase`.
لما مندوب يختار "كل الطلبات" → الـ default case بيرجّع كل الطلبات **بما فيها المحذوفة**.

**الإصلاح:** أضف الـ cases الناقصة زي ما هم في `ApplyClientStateFilterBase`.

---

### 2. TempData["Error"] مش بيتعرض في أي View
**ملف:** `PostexS/Controllers/OrdersController.cs` lines 1788, 2048
**خطورة:** عالية

`Complete()` و `ReturnedComplete()` بيعملوا `TempData["Error"]` لما مفيش طلبات صالحة للتسوية.
لكن مفيش ولا view بتقرأ `TempData["Error"]`:
- `Complete.cshtml` و `ReturnedComplete.cshtml` بتقرأ `TempData["CountryErrors"]` بس
- `_Layout.cshtml` بتقرأ `TempData["Success"]` بس

النتيجة: رسالة "لا توجد طلبات صالحة للتسوية" بتتبلع بصمت والمستخدم مش بيشوف أي حاجة.

**الإصلاح:** غيّر `TempData["Error"]` لـ `TempData["CountryErrors"]` كـ `List<string>`.

---

### 3. globalStats.Sum() بدون Any() guard → crash لمستخدم جديد
**ملف:** `PostexS/Controllers/OrdersController.cs` ~line 873
**خطورة:** عالية

في `UserOrdersApi`:
- `filteredStats` بتستخدم `Any()` قبل `Sum()` ✓
- `globalStats` **مش** بتستخدم `Any()` ✗

في EF Core 3.1، `Sum()` على set فاضي بترجع SQL `NULL` → `InvalidOperationException` لما يحاول يحوّلها لـ `double` (non-nullable).

**السيناريو:** مستخدم جديد ملوش أي طلبات → الصفحة بتقع.

**الإصلاح:** أضف نفس الـ `Any()` guard:
```csharp
TotalArrivedCost = globalQuery.Any() ? globalQuery.Sum(x => x.ArrivedCost) : 0
```

---

### 4. filteredStats بتتحسب كل request (أداء)
**ملف:** `PostexS/Controllers/OrdersController.cs` lines 832-843
**خطورة:** متوسطة

`filteredStats` (9 COUNT queries + SUM + ANY) بتشتغل على **كل** AJAX call بغض النظر عن `includeStats`.
بس `globalStats` هي اللي محمية بـ `if (includeStats)`.

يعني كل تغيير صفحة أو بحث → 9+ queries زيادة على الداتابيز بدون فايدة.

**الإصلاح:** حط `filteredStats` جوا `if (includeStats)` block أو أضف parameter منفصل ليها.

---

## Bugs موجودة من الأصل (Pre-existing)

### 5. ReturnedComplete مش بتحدّث admin.Wallet
**ملف:** `PostexS/Controllers/OrdersController.cs` ~line 2104
**خطورة:** عالية (مالية)

`Complete()` بتعمل `admin.Wallet += total` ✓
`ReturnedComplete()` بتعمل `wallet.AddedToAdminWallet = true` **بدون** ما تزود رصيد الأدمن فعلياً ✗

ده باج محاسبي موجود من الأصل — تسوية المرتجعات بتسجّل إنها اتضافت للمحفظة من غير ما تضيفها فعلاً.

**الإصلاح:** أضف `admin.Wallet += total` و `await _users.Update(admin)` زي ما `Complete()` بتعمل.

> **تحذير:** لازم تتأكد الأول إن ده فعلاً السلوك المطلوب — ممكن يكون مقصود إن المرتجعات متتضافش للمحفظة. راجع مع الفريق.

---

### 6. EditComplete بدون أي حماية idempotency
**ملف:** `PostexS/Controllers/OrdersController.cs` ~line 2396
**خطورة:** متوسطة

`Complete` و `ReturnedComplete` اتضاف لهم guard (`OrderCompleted != OK`) + `TransactionScope`.
لكن `EditComplete` لسه بدون أي حماية:
- مفيش check على `OrderCompleted`
- مفيش `TransactionScope`

ممكن يعيد تسوية نفس الطلبات ويضاعف الرصيد.

---

## مشاكل ثانوية

### 7. Catch block بيبلع الأخطاء بصمت
**ملف:** `PostexS/Controllers/OrdersController.cs` lines 1842-1845, 2116-2119
**خطورة:** متوسطة

```csharp
catch (Exception ex)
{
    // مفيش logging ولا رسالة خطأ
}
```

لو حصل أي خطأ (مثلاً طلبات من عملاء مختلفين)، الـ transaction بتعمل rollback بس المستخدم بيتحوّل للصفحة من غير أي رسالة.

---

### 8. TOCTOU Race Condition - ReadCommitted مش كافي
**ملف:** `PostexS/Controllers/OrdersController.cs` lines 1758, 1778
**خطورة:** منخفضة-متوسطة

اتنين بيسوّوا نفس الطلبات في نفس الوقت:
1. كلهم بيقرأوا `OrderCompleted != OK` ✓
2. كلهم بيعدّوا الطلبات ✓
3. كلهم بيعملوا Wallet ✓

`ReadCommitted` مش بيمنع ده. الحل: `RowVersion` concurrency token أو `Serializable` isolation أو unique constraint.

---

### 9. N+1 Query - تحميل الطلبات واحد واحد
**ملف:** `PostexS/Controllers/OrdersController.cs` lines 1773-1783, 2028-2043
**خطورة:** منخفضة

الـ validation loop بتحمّل كل طلب لوحده:
```csharp
var order = await _orders.GetObj(x => x.Id == OrderId[i]); // × 200 مرة
```

ممكن تكون query واحدة: `WHERE Id IN (...)`.

---

### 10. JS double-submit - حماية browser-side بس
**ملف:** `Complete.cshtml:247`, `ReturnedComplete.cshtml:278`
**خطورة:** منخفضة

`this.disabled=true` بيحمي من double-click في نفس الـ tab بس.
مش بيحمي من tabs تانية أو devtools أو network retry.
الـ server-side guard بيخفف الخطر جزئياً (بس بسبب TOCTOU في #8 الحماية مش كاملة).

---

## ملخص سريع

| # | النوع | الوصف | خطورة | حالة |
|---|-------|-------|-------|------|
| 1 | BUG | filter ناقص → طلبات محذوفة بتظهر للمندوب | عالية | لازم يتصلح |
| 2 | BUG | رسالة الخطأ مش بتتعرض للمستخدم | عالية | لازم يتصلح |
| 3 | BUG | crash لمستخدم جديد بدون طلبات | عالية | لازم يتصلح |
| 4 | PERF | 9 queries زيادة كل AJAX request | متوسطة | لازم يتصلح |
| 5 | PRE-EXISTING | admin.Wallet مش بيتحدث في المرتجعات | عالية | محتاج مراجعة |
| 6 | PRE-EXISTING | EditComplete بدون حماية idempotency | متوسطة | محتاج مراجعة |
| 7 | QUALITY | Catch block بيبلع الأخطاء | متوسطة | يفضّل يتصلح |
| 8 | RACE | TOCTOU في concurrent settlement | منخفضة-متوسطة | تحسين مستقبلي |
| 9 | PERF | N+1 تحميل طلبات | منخفضة | تحسين مستقبلي |
| 10 | UX | JS-only double-submit guard | منخفضة | مقبول حالياً |
