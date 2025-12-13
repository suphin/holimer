// Class definition
var KTSelect2 = function () {
    // Private functions
    var demos = function () {
        // basic
        $('#kt_select2_1, #kt_select2_1_validate').select2({
            placeholder: "Seçiniz!"
        });

        // nested
        $('#kt_select2_2, #kt_select2_2_validate').select2({
            placeholder: "Seçiniz!"
        });

        // multi select
        $('#kt_select2_3, #kt_select2_3_validate').select2({
            placeholder: "Seçiniz!"
        });

        // basic
        $('#kt_select2_4').select2({
            placeholder: "Seçiniz!"
        });
        $('#kt_select2_5').select2({
            placeholder: "Seçiniz!"
        });
        $('.kt_select2').select2({
            width: '100%' 
        });
    }



    // Public functions
    return {
        init: function () {
            demos();
        }
    };
}();

// Initialization
jQuery(document).ready(function () {
    KTSelect2.init();
});  