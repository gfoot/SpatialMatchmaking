SpatialMatchmaking
==================

This package contains a sample server, a Unity client implementation, and a demo of the usage of the Unity package. 
The server also includes a web-based Javascript client for test purposes.

Software Requirements
---------------------

To build the servlet you need a JDK and Maven.  The Java library dependencies will be downloaded automatically by 
Maven.  I use jdk1.7.0.25 from Oracle and Maven 3.1.1 from Apache.

The Unity package and project were built with Unity 4.2.2f1, and should work on later versions but may fail to 
import cleanly on earlier versions.

Servlet
-------

The sample server is a Java Servlet intended to run in a Servlet Container such as Apache Tomcat.

It is packaged as a Maven project, which may be built and deployed from the command line or imported into an 
IDE such as IntelliJ IDEA.

The Maven project uses the tomcat and jetty plugins.  The tomcat plugin primarily allows deployment to an Apache 
Tomcat server.  The jetty plugin provides a local servlet container, allowing you to more easily run a local 
instance of the servlet and test against that.

To use the tomcat maven plugin you may need to edit the relevant configuration section of pom.xml.  In particular 
it is currently set to deploy to host 'fi-cloud', which you can locally alias to an IP address via /etc/hosts 
(%WINDOWS%\system32\drivers\etc\hosts).  You may need to edit the other configuration settings, and also be aware 
that login/password details for managing the Tomcat server are stored for maven in a local configuration file 
(~/.m2/settings.xml), not in the git repository.  Google "tomcat-maven-plugin settings.xml" for more information.

The main maven goals that are useful here are:

<dl>
<dt>mvn tomcat:redeploy</dt>
<dd>Update the server with a new build of the servlet; this also restarts the perpetual running instance of the servlet</dd>
<dt>mvn jetty:run</dt>
<dd>Run the servlet locally using Jetty in the foreground</dd>
<dt>mvn jetty:start</dt>
<dd>Run the servlet locally using Jetty in the background</dd>
<dt>mvn jetty:stop</dt>
<dd>Kill any running Jetty instance, either in the foreground or the background</dd>
</dl>

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

Unity client package
--------------------

After importing the client package into your project, when you want to matchmake apply the MatchClient component to
a GameObject, and initialize any public members you need to customize.  You can remove the MatchClient component when 
matching is complete.

Note that in particular you will need to provide implementations of ILocationInterface and INetworkInterface - either
using the provided examples (UnityInputLocationInterface and UnityNetworkInterface), or custom implementations if you 
require different mechanisms for obtaining location data and initializing your networking library.

For more hints, see the example usage in the Unity Demo App for an example, in the 'Go' function of the 
SpatialMatchmakingDemo class.

Unity Demo App
--------------

The Unity Demo App must be executed standalone, in order to run multiple instances, though one instance running 
in the editor can also connect to other standalone instances.

It should work on all platfroms, and has been tested on Windows Desktop and Android.  There are some caveats on mobile,
however, due to Unity's poor WWW interface.

Before building the player, select the "Main Camera" game object and, in the Inspector, edit the "Base Url" setting of
the "SpatialMatchmakingDemo" component.  This specifies which instance of the servlet to use - for local Jetty use it 
should probably be http://localhost:8888, or with the default deployment settings you can use http://fi-cloud:8080/matcher, 
substituting for fi-cloud if you don't have a name alias set up in /etc/hosts.

The client simulates connectivity issues according to three bits which you can toggle before pressing the 'Go' button. 
The initial setting of these bits is random, and the background colour also indicates their state.  Clients can only 
communicate with each other if they share at least one set bit in common.  This can be used loosely to simulate NAT 
traversal problems and other connectivity issues.

The client also provides an option to use a fake location interface, which allows you to specify a lat/long position
instead of using Unity's location API, as that only works on mobile devices.  So, when testing on desktop, enable the 
fake location interface and optionally use the GUI fields to set the lat/long values it should report.

The client is set to seek matches within a 500m radius; this is tweakable through the MaxMatchRadius field in the 
Inspector.

