# Web-Crawler

##The Product

This assignment was to create a web crawler that will crawl given sites. The 
crawler was written in C# on the Azure platform. The project was hosted on the
Azure Cloud Service with one web role and one worker role. The two have different
functionality. The web role handles the dashboard.html where as the worker role
will just crawl websites.

##The Data

Typically, a search engine crawler will index everything about a web page. For our purposes, let’s simplify the process and only index the title, URL and date (if available). Because we’re limiting the crawler to one website, the crawler should only follow links with the pattern, http://*.cnn.com/* (Links to an external site.)

##The Infrastructure
When the dashboard first loads, the user can trigger the 'GO' button to start the
crawl. Once the crawling begins, the crawler will read the robots.txt file and
grab the disallowed and allowed sites as well as the sitemap for the worker role
to go into. Then the crawler will start to read websites and store URL links found.
If the link has not been visited yet, the link will be put in the queue asynchronously.
The visited site will be stored in the Azure Cloud Table. CPU and memory information
are also stored in the table. The rowkey of the table is the MD5 hashed version of
URL. Therefore, when the user searches for the URL, the web role can search for the
title of the website without actually going through everything in the table.

The dashboard makes multiple ajax calls to the web role. The web role returns the
relevant information as soon as it is ready.

If the user hits the 'STOP' button, the web role will send a message to the queue
telling the worker role to stop the crawl. When the user presses the 'GO' button 
again the crawler will not add the websites back into the queue but resume the
crawl from where it was left off. If the user hits the clear button, the web role 
will delete the tables.

##The User Interface
On the dashboard, there are the HTML queue size, crawled number, links found, current
state of the worker role, memory and CPU usage of the CPU of the worker role. The 
user can search for the website by typing the HTML link and by clicking on any of the
recent URLs. The search will be done automatically.

Something I have learned to optimize the process was to send the URLs found asynchronously.
This was a very important part in reducing the speed, because the worker role does not
have to wait for a response. This increased my process from 10~20 messages per second to
150~250 messages per second.
