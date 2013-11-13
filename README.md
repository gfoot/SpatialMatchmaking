SpatialMatchmaking
==================

This package contains a sample server and Unity client implementation.  The server also includes a web-based 
Javascript client for test purposes.

Servlet
-------

The sample server is a Java Servlet intended to run in a Servlet Container such as Apache Tomcat.

It is packaged as a Maven project, which may be built and deployed from the command line or imported into an 
IDE such as IntelliJ IDEA.

The Maven project uses the tomcat and jetty plugins.  The tomcat plugin primarily allows deployment to an Apache 
Tomcat server.  The jetty plugin provides a local servlet container, allowing you to more easily run a local 
instance of the servlet and test against that.

To use the tomcat maven plugin you may need to edit the relevant configuration section of pom.xml.  In particular 
it  is currently set to deploy to host 'fi-cloud', which you can locally alias to an IP address via /etc/hosts 
(%WINDOWS%\system32\drivers\etc\hosts).  You may need to edit the other configuration settings, and also be aware 
that login/password details for managing the Tomcat server are stored for maven in a local configuration file 
(~/.m2/settings.xml), not in the git repository.  Google "tomcat-maven-plugin settings.xml" for more information.

When it is correctly configured, the only command you generally need is "mvn tomcat:redeploy", which builds 
everything, then undeploys any existing instance on the server, and deploys a new instance.  The servlet runs 
perpetually, and rerunning this command is also a convenient way to restart the servlet in case it has stopped
working properly.

For local testing via jetty, you just need to run "mvn jetty:run", or "mvn jetty:start" to start it in the 
background.  Either way, you can also use "mvn jetty:stop" to stop the process, or Ctrl-C if you ran it in the 
foreground.

Test web interface
------------------

When the servlet is running you can use the multi-client test web interface by pointing a web browser at 
http://localhost:8888/jstest/index.html if you're using Jetty, or http://fi-cloud:8080/matcher/jstest/index.html 
if you're using the standard fi-cloud deployment settings.

The clients pretty much manage themselves once started.  You get an opportunity to control the client-to-client 
connection process, which allows you to instead reject matches and check that the clients get rematched against
other peers.

The status line at the top of the display shows some statistics about the server - the number of active clients, 
the number of unmatched clients, the number of matches, and the number of clients that are pending deletion.

... more to follow ...
