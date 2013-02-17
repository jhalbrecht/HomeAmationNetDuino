using System;
using System.Ext.Xml;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
// using Rodaw.Netmf.Led;
// using Rodaw.Netmf;
using Rodaw.Netmf.Led;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

// Adapted from:
//  http://wiki.tinyclr.com/index.php?title=TCP_/_Web_Server_Tutorial

namespace HaNdP1
{
    public class Program
    {
        static SingleLed singleLed0 = new SingleLed(new OutputPort(Pins.ONBOARD_LED, true));

        public static SummaryTemperatureData std =
            new SummaryTemperatureData(
                "JHA Netduino",
                DateTime.Now,
                45.6745f,
                87.6589f);

        public static void Main()
        {
            singleLed0.LedOnState = true;
            singleLed0.BlinkDuration = 250;
            singleLed0.Mode = SingleLedModes.Blink;
            Thread.Sleep(250);
            singleLed0.Mode = SingleLedModes.Blink;

            Thread ledThread = new Thread(LedThread);
            ledThread.Start(); 

            SetTheClock();



            #region Check we have a valid NIC
            // First, make sure we actually have a network interface to work with!
            if (Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().Length < 1)
            {
                Debug.Print("No Active network interfaces. Bombing out.");
                Thread.CurrentThread.Abort();
            }
            #endregion

            // OK, retrieve the network interface
            Microsoft.SPOT.Net.NetworkInformation.NetworkInterface NI = Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0];

            #region DHCP Code
            ////// If DHCP is not enabled, then enable it and get an IP address, else renew the lease. Most iof us have a DHCP server
            ////// on a network, even at home (in the form of an internet modem or wifi router). If you want to use a static IP
            ////// then comment out the following code in the "DHCP" region and uncomment the code in the "fixed IP" region.
            //if (NI.IsDhcpEnabled == false)
            //{
            //    Debug.Print("Enabling DHCP.");
            //    NI.EnableDhcp();
            //    Debug.Print("DCHP - IP Address = " + NI.IPAddress + " ... Net Mask = " + NI.SubnetMask + " ... Gateway = " + NI.GatewayAddress);
            //}
            //else
            //{
            //    Debug.Print("Renewing DHCP lease.");
            //    NI.RenewDhcpLease();
            //    Debug.Print("DCHP - IP Address = " + NI.IPAddress + " ... Net Mask = " + NI.SubnetMask + " ... Gateway = " + NI.GatewayAddress);
            //}
            #endregion

            #region Static IP code
            // Uncomment the following line if you want to use a static IP address, and comment out the DHCP code region above
            NI.EnableStaticIP("192.168.1.210", "255.255.255.0", "192.168.1.1");
            #endregion

            #region Create and Bind the listening socket
            // Create the socket            
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

           

            // Bind the listening socket to the portum  
            IPAddress hostIP = IPAddress.Parse(NI.IPAddress);
            IPEndPoint ep = new IPEndPoint(hostIP, 80);
            listenSocket.Bind(ep);
            #endregion

            // Start listening
            listenSocket.Listen(1);

            // Main thread loop
            while (true)
            {
                try
                {
                    Debug.Print("listening...");
                    Socket newSock = listenSocket.Accept();
                    Debug.Print("Accepted a connection from " + newSock.RemoteEndPoint.ToString());
                    // byte[] messageBytes = Encoding.UTF8.GetBytes(ButtonPage(NI.IPAddress));
                    byte[] messageBytes = Encoding.UTF8.GetBytes(ReturnSummaryDataXml());
                    newSock.Send(messageBytes);
                    Thread.Sleep(1000);
                    newSock.Close();
                }
                catch (Exception e)
                {
                    Debug.Print(e.Message);
                }
            }

        }

        private static void LedThread()
        {
            while (true)
            {
                singleLed0.Mode = SingleLedModes.Blink;
                Thread.Sleep(750); 
            }
        }

        private static void SetTheClock()
        {
            // http://www.tinyclr.com/codeshare/entry/404
            var adjustTz = new System.TimeSpan(0, 9, 0, 0);
                // I'm in Arizona where they don't adjust DST, when I go back to Spokane this will need more elegance
            // Utility.SetLocalTime(Util.GetNetworkTime() - adjustTz); //Set the RTC
            Utility.SetLocalTime(Rodaw.Netmf.Util.GetNetworkTime() - adjustTz); //Set the RTC
            Debug.Print("DateTime.Now... " + DateTime.Now.ToString());
        }

        // Read the states of the Cobra buttons and build a web page showing their states
        static string ButtonPage(string sourceIP)
        {
            // Determine the states of the three cobra buttons
            //string ubs; if (upButton.Read() == false) ubs = "Pressed"; else ubs = "Released";
            //string sbs; if (selectButton.Read() == false) sbs = "Pressed"; else sbs = "Released";
            //string dbs; if (downButton.Read() == false) dbs = "Pressed"; else dbs = "Released";

            var ubs = "Pressed";
            var sbs = "Pressed";
            var dbs = "Pressed";
            var foo = sourceIP; 


            // Build the web page
            string s = "<html>\n";                                      // First the page type
            s += "<head><title>HomeAmation Test Page</title>";            // now the page header
            s += "<META http-equiv=\"REFRESH\" content=\"1;URL=" + sourceIP + "\">";    // Auto-refresh
            s += "</head>\n<body>\n";                                   // start the body        
            s += "<p>Up Button State = <i>" + ubs + "</i></p>";         // Up button, state in italics
            s += "<p>Select Button State = <i>" + sbs + "</i></p>";     // Select button, state in italics
            s += "<p>Down Button State = <i>" + dbs + "</i></p>";       // Down button, state in italics
            s += "<p>Source IP is: " + sourceIP + "</p>";
            s += "<p>At the tone the time will be: " + DateTime.Now.ToLocalTime() + "</p>";
            s += "</body>";                                             // close the body section
            s += "</html>";                                             // close the page type
            return s;
        }

        static string ReturnSummaryDataXml()
        {
            // http://www.scribd.com/doc/105868082/115/Creating-XML

            MemoryStream ms = new MemoryStream();

            using (XmlWriter xmlWriter = XmlWriter.Create(ms))
            {
                xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                xmlWriter.WriteStartElement("SummaryTemperatureData");
                xmlWriter.WriteAttributeString("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                xmlWriter.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                // <xs:element name="startdate" type="xs:dateTime"/>
                //xmlWriter.WriteElementString("CurrentMeasuredTime", std.CurrentMeasuredTime.ToString()); // TODO hmmm why is this showing a preposterous date? 
                xmlWriter.WriteElementString("DataLoggerDeviceName", std.DataLoggerDeviceName);
                xmlWriter.WriteElementString("CurrentMeasuredTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                xmlWriter.WriteElementString("CurrentTemperature0", std.CurrentTemperature0.ToString("F2"));
                xmlWriter.WriteElementString("CurrentTemperature1", std.CurrentTemperature1.ToString("F2"));
                xmlWriter.WriteEndElement();
                xmlWriter.Flush();
                xmlWriter.Close();
            }

            // StringBuilder sb = new StringBuilder(); 
            byte[] byteArray = ms.ToArray();
            char[] cc = UTF8Encoding.UTF8.GetChars(byteArray);
            string str = new string(cc);

            // TODO apparentloy no XmlWriter.WriterStartDocument is there another way to do this? 
            // a more better, or proper way? 
            // TODO add style information? 

            // sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>"); // jha
            // sb.Append(str);
            // return sb.ToString(); 
            return str;
            //return cc; 
        }

    }
}
