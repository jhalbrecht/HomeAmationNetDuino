using System;
using System.Ext.Xml;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Rodaw.Netmf.Led;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

// Adapted from:
//  http://wiki.tinyclr.com/index.php?title=TCP_/_Web_Server_Tutorial

namespace HaNdP1
{
    public class Program
    {
        static SingleLed singleLed0 = new SingleLed(new OutputPort(Pins.ONBOARD_LED, true));
        static AnalogInput a0 = new AnalogInput((Cpu.AnalogChannel)Cpu.AnalogChannel.ANALOG_0, 3.3, 0.0, 10);
        static double a = 0;
        static double temperature0 = 0;

        public static SummaryTemperatureData std =
            new SummaryTemperatureData(
                "JHA Netduino", // TODO name your data logger here.
                DateTime.Now,
                45.6745f,
                0.0f);      // This code collects Temperature0 on A0. Set Temperature1 to 0.

        public static void Main()
        {
            singleLed0.LedOnState = true;
            singleLed0.BlinkDuration = 250;
            singleLed0.Mode = SingleLedModes.Blink;
            Thread.Sleep(250);
            singleLed0.Mode = SingleLedModes.Blink;

            Thread ledThread = new Thread(LedThread);
            ledThread.Start();

            Thread temperatureThread = new Thread(TemperatureThread);
            temperatureThread.Start();

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
            // TODO enter your ip address here. Don't forget your default gateway.
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

        private static void TemperatureThread()
        {
            while (true)
            {
                var times = 10;
                a = 0;
                // string s = a0.Read().ToString("F2");
                for (int i = 0; i < times; i++)
                {
                    a += a0.Read();
                }
                temperature0 = (a / times) * 100;
                Debug.Print(temperature0.ToString("F2"));
                Thread.Sleep(1000); 
            }
        }

        private static void LedThread()
        {
            while (true)
            {
                singleLed0.Mode = SingleLedModes.Blink;
                Thread.Sleep(1750); 
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

        static string ReturnSummaryDataXml()
        {
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
                // xmlWriter.WriteElementString("CurrentTemperature0", std.CurrentTemperature0.ToString("F2"));
                xmlWriter.WriteElementString("CurrentTemperature0", temperature0.ToString("F2"));
                xmlWriter.WriteElementString("CurrentTemperature1", std.CurrentTemperature1.ToString("F2"));
                xmlWriter.WriteEndElement();
                xmlWriter.Flush();
                xmlWriter.Close();
            }

            byte[] byteArray = ms.ToArray();
            char[] cc = UTF8Encoding.UTF8.GetChars(byteArray);
            string str = new string(cc);

            // TODO add style information? 
            return str;
        }
    }
}
