﻿(function () {

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
            data: {},
            dataType: "text",
            url: "admin.asmx/searchURL",
            success: function (data) {
                document.getElementById("searchresult").innerHTML = data;
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
            type: "GET",
            data: {},
            dataType: "text",
            url: "admin.asmx/HTMLQueueCount",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                var lasttendata = JSON.parse(data);
                var tempval = $(xml).find("int");
                var parsedata = $.parseXML(data);

                console.log(tempval);
                htmlcount = parseInt(data);
                console.log(htmlcount);
                document.getElementById("htmlcount").innerHTML = data;
            }
        });

        $.ajax({
            type: "GET",
            data: {},
            dataType: "xml",
            url: "admin.asmx/XmlQueueCount",
            success: function (data) {
                xmlcount = parseInt(data);
            }
        });

        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin.asmx/lasttenpages",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                var lasttendata = JSON.parse(data);
                var lasttendata1 = JSON.parse(lasttendata.d);
                document.getElementById("recenttenurls").innerHTML = lasttendata1.lastitems;
                document.getElementById("crwaledcount").innerHTML = lasttendata1.count;
                document.getElementById("totalfound").innerHTML = lasttendata1.totalurl;
            }
        });
        //console.log("xml" + xmlcount);
        //console.log("html" + htmlcount);
        if (!crwaling) {
            statespan.innerHTML = "1";
        } else if (xmlcount == 0 && htmlcount == 0) {
            //idle
            document.getElementById("state").innerHTML = "2";
        } else if (xmlcount > 0) {
            //loading
            document.getElementById("state").innerHTML = "3";
        } else if (htmlcount > 0) {
            //crwaling
            document.getElementById("state").innerHTML = "4";
        } else {
            //stopped
            document.getElementById("state").innerHTML = "5";
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
