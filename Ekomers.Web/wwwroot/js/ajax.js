function GetAjax(getUrl, fieldId) {
    $.ajax(
        {
            // The link we are accessing.
            url: getUrl,

            // The type of request.
            type: "get",

            // The type of data that is getting returned.
            dataType: "html",

            error: function () {
                //ShowStatus("AJAX - error()");
                console.log("AJAX - error()");

                // Load the content in to the page.
                $('#' + fieldId).html("<p>Sayfa bulunamadı!!</p>");
            },

            success: function (strData) {

                $("#divLoading").hide();
                // Load the content in to the page.
                $('#' + fieldId).html(strData);
            }
        });
}


function PostAjax(getUrl, fieldID) {
    $.ajax(
        {
            // The link we are accessing.
            url: getUrl,


            // The type of request.
            type: "post",

            // The type of data that is getting returned.
            dataType: "html",

            error: function () {
                ShowStatus("AJAX - error()");

                // Load the content in to the page.
                $('#' + fieldId).html("<p>Page Not Found!!</p>");
            },

            success: function (strData) {

                $("#divLoading").hide();
                // Load the content in to the page.
                $('#' + fieldId).html(strData);
            }
        });
}

var yukleniyor = "<div><span>&nbsp;&nbsp; Yükleniyor... </span></div>";



function TableToExcel(tableid, tablename) {
    //$.print("#printable"); //Yazdırma işlemini burada tetikliyoruz.
    $(document).ready(function () {
        $(tableid).table2excel({
            exclude: ".noExl",
            name: "Worksheet Name",
            filename: tablename //do not include extension
        });
    });
}

 

function Yazdir(tableid) {
    //$.print("#printable"); //Yazdırma işlemini burada tetikliyoruz.
  
    $(document).ready(function () {
        alert("burası 2");

        $(tableid).print({
            //Use Global styles
            globalStyles: true,
            //Add link with attrbute media=print
            mediaPrint: false,
            //Custom stylesheet
            stylesheet: "http://fonts.googleapis.com/css?family=Inconsolata",
            //Print in a hidden iframe
            iframe: false,
            //Don't print this
            noPrintSelector: ".no-print",
            //Add this at top
            //prepend: "<br /><p>YAZDIRAN: "+ User +" </p>",
            //Add this on bottom
            append: null,
            //Log to console when printing is done via a deffered callback
            deferred: $.Deferred().done(function () { console.log('Printing done', arguments); })
        });
    });
}
function Yazdir2(tableid) {
        
    $(tableid).printThis();
 
}
