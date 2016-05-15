$(function () {

    document.getElementById("button").onclick = goWebsite;

    function goWebsite() {
        $.ajax({
            type: "POST",
            data: JSON.stringify({ address: $("input").val() }),
            dataType: "json",
            url: "admin.asmx/startCrawl",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                console.log("add count");
            }
        });
    }
});
