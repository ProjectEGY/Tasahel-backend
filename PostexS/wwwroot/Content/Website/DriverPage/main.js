
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
                text: "إنتظر من فضلك جارى التسجيل ، برجاء عدم غلق الصفحه"
            })
            $('#cover-spin').show();
            $(".my-progress-bar").show();
            $(".validate-form").attr("disabled", true);
            progress.circularProgress('animate', 98, 71000);
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
