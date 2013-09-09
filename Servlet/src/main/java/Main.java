import com.sun.jersey.api.container.grizzly2.GrizzlyWebContainerFactory;
import org.glassfish.grizzly.http.server.HttpServer;
import org.glassfish.grizzly.http.server.StaticHttpHandler;

import java.io.IOException;
import java.util.HashMap;
import java.util.Map;

public class Main {

    public static void main(String[] args) throws IOException {

        final String baseUri = "http://localhost:9998/";
        final Map<String, String> initParams =
                new HashMap<String, String>();

        initParams.put("javax.ws.rs.Application", "com.studiogobo.fi.Matcher.Servlet.Application");
        initParams.put("com.sun.jersey.config.property.packages", "com.studiogobo.fi.Matcher.Servlet");

        System.out.println("Starting grizzly...");
        HttpServer httpServer = GrizzlyWebContainerFactory.create(baseUri, initParams);
        httpServer.getServerConfiguration().addHttpHandler(
                new StaticHttpHandler(Main.class.getClassLoader().getResource("").getPath()),
                "/static");

        System.out.println(String.format(
                "Jersey app started with WADL available at %sapplication.wadl\n" +
                        "Hit enter to stop it...", baseUri));

        System.in.read();

        httpServer.stop();
        System.exit(0);
    }

}

