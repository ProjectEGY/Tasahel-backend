
(function ($) {
    "use strict";
  
    /*==================================================================
    [ Validate ]*/
    var input = $('.validate-input .input100');

    $('.validate-form').on('submit',function(event){
        var check = true;

        for(var i=0; i<input.length; i++) {
            if(validate(input[i]) == false){
                showValidate(input[i]);
                check=false;
            }
        }

        if (check == false) {
            return check;
        }
        else {
            var progress = $(".my-progress-bar").circularProgress({
                line_width: 6,
                color: "#000",
                starting_position: 0, // 12.00 o' clock position, 25 stands for 3.00 o'clock (clock-wise)
                percent: 0, // percent starts from
                percentage: true,
                text: "إنتظر من فضلك جارى التسجيل"
            })
            event.preventDefault();
            $('#cover-spin').show();
            $(".my-progress-bar").show();
            $(".validate-form").attr("disabled", true);
            progress.circularProgress('animate', 98, 45000);
            var formData = new FormData($(".validate-form")[0]);
            $.ajax({
                url: '/Partners/Join',
                type: "POST",
                data: formData,
                processData: false,
                contentType: false,
                success: function (results) {
                    if (results.Success == false) {
                        toastr.error(results.Message, { timeOut: 9000 });
                        $('#cover-spin').hide();
                        $(".my-progress-bar").hide();
                        $(".validate-form").attr("disabled", false);
                    }
                    else {
                        Swal.fire({
                            title: 'تم الارسال !',
                            text: 'تم استقبال طلبك وسيقوم احد ممثلينا بالرد عليكم فى أقرب وقت ممكن',
                            icon: 'success',
                            confirmButtonText: 'حسناً',
                        }).then((result) => {
                            window.location.href = "/";
                        })
                        $('#cover-spin').hide();
                        $(".my-progress-bar").hide();
                    }
                },

                failure: function (results) {
                    alert("failed");
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    alert("Something went wrong");
                },
            });
        }
    });


    $('.validate-form .input100').each(function(){
        $(this).focus(function(){
           hideValidate(this);
        });
    });

    function validate (input) {
        if ($(input).attr('type') == 'email' || $(input).attr('name') == 'Email') {
            if($(input).val().trim().match(/^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,5}|[0-9]{1,3})(\]?)$/) == null) {
                return false;
            }
        }
        else {
            if($(input).val().trim() == ''){
                return false;
            }
        }
    }

    function showValidate(input) {
        var thisAlert = $(input).parent();

        $(thisAlert).addClass('alert-validate');
    }

    function hideValidate(input) {
        var thisAlert = $(input).parent();

        $(thisAlert).removeClass('alert-validate');
    }
    
    

})(jQuery);
