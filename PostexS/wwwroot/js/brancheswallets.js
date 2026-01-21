$(document).ready(function () {
    var totalWalletSummation = 0;
    var totalSettlement = 0;
    var totalFinancialStatus = 0;

    // جلب تسويات كل فرع عند تحميل الصفحة
    $('.branch-card').each(function () {
        var branchId = $(this).data('branch-id');
        var resultDiv = $('#settlement-' + branchId);
        var financialStatus = $('#financial-status-' + branchId);
        var loader = $('#loader-' + branchId);

        // عرض اللودر أثناء التحميل
        loader.show();

        $.post('/Branchs/GetBranchSettlement', { branchId: branchId }, function (data) {
            // إخفاء اللودر وعرض التسويات والوضع المالي
            loader.hide();
            resultDiv.html(`${data} جنيه`);

            // حساب الفرق بين إجمالي المحافظ والتسويات
            var walletSummation = parseFloat($('#wallet-summation-' + branchId).text().replace(' جنيه', ''));
            var financialDifference = (-1 * walletSummation) - parseFloat(data);

            financialStatus.text(`الوضع المالي: ${financialDifference} جنيه`);

            // إضافة القيم إلى الإجماليات
            totalWalletSummation += walletSummation;
            totalSettlement += parseFloat(data);
            totalFinancialStatus += financialDifference;

            // تحديث الإجماليات في القسم الجديد
            $('#total-wallet-summation').text(totalWalletSummation + ' جنيه');
            $('#total-settlement').text(totalSettlement + ' جنيه');
            $('#total-financial-status').text(totalFinancialStatus + ' جنيه');
        }).fail(function () {
            loader.hide();
            resultDiv.html('<span class="text-danger">حدث خطأ أثناء تحميل البيانات.</span>');
            financialStatus.text('');
        });
    });
});
