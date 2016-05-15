$(function () {

    document.getElementById("button").onclick = goWebsite;

    function goWebsite() {
      /*  $.ajax({
            type: "POST",
            data: JSON.stringify({ address: $("input").val() }),
            dataType: "json",
            url: "admin.asmx/startCrawl",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                console.log("add count");
            }
        });*/

        $.ajax({
            type: "POST",
            data: { },
            dataType: "text",
            url: "admin.asmx/HTMLQueueCount",
            success: function (data) {
                document.getElementById("htmlcount").innerHTML = data;
            }
        });

        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/XmlQueueCount",
            success: function (data) {
                document.getElementById("xmlcount").innerHTML = data;
            }
        });

        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/lasttenpages",
            success: function (data) {
                //console.log(data);
                var item = data;
                var item1 = data[0];
                //console.log(item1);
                document.getElementById("recenttenurls").innerHTML = data; //[0].join("<br />")
            }
        });
    }
});
