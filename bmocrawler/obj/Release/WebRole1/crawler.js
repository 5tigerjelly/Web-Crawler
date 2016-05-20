(function () {

    "use strict";
    var xmlcount = 0;
    var htmlcount = 0;
    var crwaling = true;
    var clearing = false;

    window.onload = function () {
        document.getElementById("go").onclick = go;
        document.getElementById("searchurl").onclick = searchurllink;
        document.getElementById("refresh").onclick = clearsite;
        document.getElementById("stop").onclick = stop;
        document.getElementById("addmorelink").onclick = addmorelink;
        var myVar = setInterval(function () { refreshsite() }, 1000);
        var myVar2 = setInterval(function () { loadGraph() }, 1000);
    };

    function addmorelink() {
        $.ajax({
            type: "POST",
            data: JSON.stringify({ search: $("#morelinkbox").val() }),
            dataType: "json",
            url: "admin2.asmx/addURL",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
            }
        });
    }

    function go() {
        crwaling = true;
        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin2.asmx/restart",
            success: function (data) {
            }
        });
    }

    function stop() {
        crwaling = false;
        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin2.asmx/stopCrawl",
            success: function (data) {
            }
        });
    }

    function searchurllink() {
        if (!clearing) {
            $.ajax({
                type: "POST",
                data: JSON.stringify({ search: $("#searchval").val() }),
                dataType: "json",
                url: "admin2.asmx/searchURL",
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    var lasttendata = JSON.parse(data.d);
                    document.getElementById("searchresult").innerHTML = lasttendata.title;
                    var date = new Date(parseInt(lasttendata.pubdate.substr(6)));
                    document.getElementById("pubdate").innerHTML = date;
                }
            });
        }
    }

    function refreshsite() {
        if (!clearing) {

            $.ajax({
                type: "POST",
                data: {},
                dataType: "text",
                url: "admin2.asmx/HTMLQueueCount",
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
                url: "admin2.asmx/XmlQueueCount",
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
                url: "admin2.asmx/getErrortable",
                contentType: "application/json; charset=utf-8",
                success: function (data) {
                    var lasttendata = JSON.parse(data);
                    var lasttendata2 = JSON.parse(lasttendata.d);

                    var outer2 = document.createElement("tbody");
                    outer2.id = "oldtable";
                    $.each(lasttendata2, function (index, element) {
                        var outer = document.createElement("tr");
                        var innerurl = document.createElement("td");
                        var innertype = document.createElement("td");
                        innerurl.innerHTML = index;
                        innertype.innerHTML = element;
                        outer.appendChild(innerurl);
                        outer.appendChild(innertype);
                        outer2.appendChild(outer);
                    });
                    document.getElementById("errortable").appendChild(outer2);
                    var old_tbody = document.getElementById("oldtable");
                    old_tbody.parentNode.replaceChild(outer2, old_tbody)
                }
            });

            $.ajax({
                type: "POST",
                data: {},
                dataType: "text",
                url: "admin2.asmx/lasttenpages",
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
                        result.onclick = clickedLink;
                        result.innerHTML = element;
                        resultbox.appendChild(result);
                    });
                    if (lasttendata1 != null) {
                        document.getElementById("crwaledcount").innerHTML = lasttendata1.count;
                        var totalcount = parseInt(lasttendata1.totalurl);

                        if (totalcount > 1000000) {
                            totalcount = Math.round(totalcount / 1000);
                            totalcount = totalcount + "k";
                        } else if (totalcount > 1000000000) {
                            totalcount = Math.round(totalcount / 1000000);
                            totalcount = totalcount + "M";
                        } else if (totalcount > 1000000000000) {
                            totalcount = Math.round(totalcount / 1000000000);
                            totalcount = totalcount + "B";
                        }
                        document.getElementById("totalfound").innerHTML = totalcount;
                    } else {
                        document.getElementById("crwaledcount").innerHTML = "0";
                        document.getElementById("totalfound").innerHTML = "0";
                    }
                    
                }
            });
            if (!crwaling) {
                document.getElementById("state").innerHTML = "Idle";
            } else if (xmlcount == 0 && htmlcount == 0) {
                document.getElementById("state").innerHTML = "Idle";
            } else if (xmlcount > 0) {
                document.getElementById("state").innerHTML = "Loading";
            } else if (htmlcount > 0) {
                document.getElementById("state").innerHTML = "Crawling";
            } else {
                document.getElementById("state").innerHTML = "Idle";
            }
        }
    }

    function clearsite() {
        document.getElementById("searchresult").innerHTML = "Currently Clearing";
        clearing = true;
        document.getElementsByTagName("button").disabled = true;
        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin2.asmx/clearIndex",
            success: function (data) {
                document.getElementsByTagName("button").disabled = false;
                clearing = false;
                document.getElementById("searchresult").innerHTML = "Done Clearing";
            }
        });
    }

    function clickedLink() {
        $("#searchval").val(this.innerHTML);
        searchurllink();
    }

    function loadGraph() {
        $.ajax({
            type: "POST",
            data: {},
            dataType: "text",
            url: "admin2.asmx/graphData",
            contentType: "application/json; charset=utf-8",
            success: function (data) {
                
                var lasttendata = JSON.parse(data);
                var lasttendata2 = JSON.parse(lasttendata.d);
                var memdata = lasttendata2.memorylist.split(',');
                var cpudata = lasttendata2.cpulist.split(',');
                document.getElementById("memorycount").innerHTML = memdata[memdata.length - 1] + " MB";
                document.getElementById("cpucount").innerHTML = cpudata[cpudata.length - 1] + "%";

                document.getElementById("memgraph").innerHTML = "";

                var m = [30, 30, 3, 80];
                var w = 650 - m[1] - m[3];
                var h = 300 - m[0] - m[2]; 
                var x = d3.scale.linear().domain([0, memdata.length]).range([0, w]); 
                var y = d3.scale.linear().domain([d3.min(memdata), d3.max(memdata)]).range([h, 0]); 
                var line1 = d3.svg.line()
                    .x(function (d, i) {
                        return x(i);
                    })
                    .y(function (d) {
                        return y(d);
                    })
                    .interpolate("basis");
                var graph = d3.select("#memgraph").append("svg:svg")
                      .attr("width", w + m[1] + m[3])
                      .attr("height", h + m[0] + m[2])
                    .append("svg:g")
                      .attr("transform", "translate(" + m[3] + "," + m[0] + ")");
                var xAxis = d3.svg.axis().scale(x).tickSize(-h).tickSubdivide(true);
                graph.append("svg:g")
                      .attr("class", "x axis")
                      .attr("transform", "translate(0," + h + ")")
                      .call(xAxis);
                var yAxisLeft = d3.svg.axis().scale(y).ticks(4).orient("left");
                graph.append("svg:g")
                      .attr("class", "y axis")
                      .attr("transform", "translate(-25,0)")
                      .call(yAxisLeft);
                graph.append("svg:path").attr("d", line1(memdata));
                secondGraph(cpudata);
            }
        });
    }

    function secondGraph(cpudata) {
        document.getElementById("cpugraph").innerHTML = "";

        var m = [30, 30, 3, 80];
        var w = 650 - m[1] - m[3];
        var h = 300 - m[0] - m[2];
        var x = d3.scale.linear().domain([0, cpudata.length]).range([0, w]);
        var y = d3.scale.linear().domain([0, 100]).range([h, 0]);
        var line1 = d3.svg.line()
            .x(function (d, i) {
                return x(i);
            })
            .y(function (d) {
                return y(d);
            })
            .interpolate("basis");
        var graph = d3.select("#cpugraph").append("svg:svg")
              .attr("width", w + m[1] + m[3])
              .attr("height", h + m[0] + m[2])
            .append("svg:g")
              .attr("transform", "translate(" + m[3] + "," + m[0] + ")");
        var xAxis = d3.svg.axis().scale(x).tickSize(-h).tickSubdivide(true);
        graph.append("svg:g")
              .attr("class", "x axis")
              .attr("transform", "translate(0," + h + ")")
              .call(xAxis);
        var yAxisLeft = d3.svg.axis().scale(y).ticks(4).orient("left");
        graph.append("svg:g")
              .attr("class", "y axis")
              .attr("transform", "translate(-25,0)")
              .call(yAxisLeft);
        graph.append("svg:path").attr("d", line1(cpudata));
    }
})();
