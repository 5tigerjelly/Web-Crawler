(function () {

    "use strict";
    var xmlcount = 0;
    var htmlcount = 0;
    var crwaling = true;

    window.onload = function () {
        document.getElementById("go").onclick = go;
        document.getElementById("searchurl").onclick = searchurllink;
        document.getElementById("refresh").onclick = clearsite;
        document.getElementById("stop").onclick = stop;
        var myVar = setInterval(function () { refreshsite() }, 3000);
    };

    function go() {
        crwaling = true;
        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/restart",
            success: function (data) {
                console.log(data);
            }
        });
    }

    function stop() {
        crwaling = false;
        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/stopCrawl",
            success: function (data) {
                console.log(data);
            }
        });
    }

    function searchurllink() {
        $.ajax({
            type: "POST",
            data: JSON.stringify({ search: $("#searchval").val() }),
            dataType: "json",
            url: "admin.asmx/searchURL",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                console.log(data.d);
                document.getElementById("searchresult").innerHTML = data.d;
            }
        });
    }

    function refreshsite() {
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
            data: {},
            dataType: "text",
            url: "admin.asmx/HTMLQueueCount",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                var lasttendata = JSON.parse(data);
                htmlcount = lasttendata.d;
                document.getElementById("htmlcount").innerHTML = lasttendata.d;
            }
        });

        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/XmlQueueCount",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                var lasttendata = JSON.parse(data);
                xmlcount = lasttendata.d;
            }
        });

        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/lasttenpages",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                var resultbox = document.getElementById("listoften");
                $('#listoften').empty();
                var lasttendata = JSON.parse(data);
                var lasttendata1 = JSON.parse(lasttendata.d);
                var tenlist = lasttendata1.lastitems.split(',');
                
                $.each(tenlist, function (index, element) {
                    var result = document.createElement("li");
                    result.class = "tenlistli";
                    result.innerHTML = element;
                    //result.onclick = clickTitle;
                    resultbox.appendChild(result);
                });
                document.getElementById("crwaledcount").innerHTML = lasttendata1.count;
                document.getElementById("totalfound").innerHTML = lasttendata1.totalurl;
            }
        });
        console.log("xml" + xmlcount);
        console.log("html" + htmlcount);
        if (!crwaling) {
            document.getElementById("state").innerHTML = "Idle";
        } else if (xmlcount == 0 && htmlcount == 0) {
            //idle
            document.getElementById("state").innerHTML = "Idle";
        } else if (xmlcount > 0) {
            //loading
            document.getElementById("state").innerHTML = "Loading";
        } else if (htmlcount > 0) {
            //crwaling
            document.getElementById("state").innerHTML = "Crawling";
        } else {
            //stopped
            document.getElementById("state").innerHTML = "Idle";
        }

        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/getPerformance",
            success: function (data) {
                document.getElementById("memorycount").innerHTML = data + " MB"; //[0].join("<br />")
            }
        });

        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/getCPUPerformance",
            success: function (data) {
                document.getElementById("cpucount").innerHTML = data + "%"; //[0].join("<br />")
            }
        });
    }

    function clearsite() {
        document.getElementsByTagName("button").disabled = true;
        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/clearIndex",
            success: function (data) {
                document.getElementsByTagName("button").disabled = false;
            }
        });
    }
})();
