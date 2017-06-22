
************* Welcome to the  Microsoft.Diagnostics.Tracing.TraceEvent library!  ***************

This library is designed to make controlling and parsing Event Tracing for Windows (ETW) events easy.  
In particular if you are generating events with System.Diagnostics.Tracing.EventSource, this library 
makes it easy to process that data.  

******** PROGRAMMERS GUIDE ********

If you are new to TraceEvent, see the _TraceEventProgammersGuide.docx that was installed as part of 
your solution when this NuGet package was installed.

************ FEEDBACK *************

If you have problems, wish to report a bug, or have a suggestion please log your comments on the
.NET Runtime Framework Blog http://blogs.msdn.com/b/dotnet/ under the TraceEvent announcement. 

********** RELEASE NOTES ***********

If you are interested what particular features/bug fixes are in this particular version please 
see the TraceEvent.RelaseNotes.txt file that is part of this package.   It also contains 
information about breaking changes (If you use the bcl.codeplex version in the past).  

************* SAMPLES *************

There is a companion NUGET package called Microsoft.Diagnostics.Tracing.TraceEvent.Samples.   These
are simple but well commented examples of how to use this library.    To get the samples, it is best 
to simply create a new Console application, and then reference the Samples package from that App.  
The package's README.TXT file tell you how to run the samples.

************** BLOGS **************

See http://blogs.msdn.com/b/vancem/archive/tags/traceevent/ for useful blog entries on using this
package.   

*********** QUICK STARTS ***********

The quick-starts below will get you going in a minimum of typing, but please see the WELL COMMENTED 
samples in the Samples NUGET package that describe important background and other common scenarios.  

**************************************************************************************************
******* Quick Start: Turning on the 'MyEventSource' EventSource and log to MyEventsFile.etl:

    using (var session = new TraceEventSession("SimpleMontitorSession", "MyEventsFile.etl"))     // Sessions collect and control event providers.  Here we send data to a file
    {
        var eventSourceGuid = TraceEventProviders.GetEventSourceGuidFromName("MyEventSource");   // Get the unique ID for the eventSouce. 
        session.EnableProvider(eventSourceGuid);                                                 // Turn it on. 
        Thread.Sleep(10000);                                                                     // Collect for 10 seconds then stop.                  
    }

**************************************************************************************************
******** Quick Start: Reading MyEventsFile.etl file and printing the events.    

    using (var source = new ETWTraceEventSource("MyEtlFile.etl"))            // Open the file
    {
        var parser = new DynamicTraceEventParser(source);                    // DynamicTraceEventParser knows about EventSourceEvents
        parser.All += delegate(TraceEvent data)                              // Set up a callback for every event that prints the event
        {
            Console.WriteLine("GOT EVENT: " + data.ToString());              // Print the event.  
        };
        source.Process();                                                    // Read the file, processing the callbacks.  
    }                                                                        // Close the file.  


*************************************************************************************************************
******** Quick Start: Turning on the 'MyEventSource', get callbacks in real time (no files involved).  

    using (var session = new TraceEventSession("MyRealTimeSession"))         // Create a session to listen for events
    {
        session.Source.Dynamic.All += delegate(TraceEvent data)              // Set Source (stream of events) from session.  
        {                                                                    // Get dynamic parser (knows about EventSources) 
                                                                             // Subscribe to all EventSource events
            Console.WriteLine("GOT Event " + data);                          // Print each message as it comes in 
        };

        var eventSourceGuid = TraceEventProviders.GetEventSourceGuidFromName("MyEventSource"); // Get the unique ID for the eventSouce. 
        session.EnableProvider(eventSourceGuid);                                               // Enable MyEventSource.
        session.Source.Process();                                                              // Wait for incoming events (forever).  
    }
