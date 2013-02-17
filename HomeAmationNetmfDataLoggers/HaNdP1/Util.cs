using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.SPOT;

namespace HaNdP1
{
    public class Util
    {

        public static DateTime GetNetworkTime()
        {
            // http://wiki.tinyclr.com/index.php?title=NTP

            byte[] ip = { 129, 6, 15, 28 };//time-a.nist.gov            

            // byte[] ip = { 96, 44, 142, 5 }; // one of pool.ntp.org

            System.Net.IPAddress timeServer = new IPAddress(ip);
            IPEndPoint ep = new IPEndPoint(timeServer, 123);

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect(ep);

            byte[] ntpData = new byte[48]; // RFC 2030
            ntpData[0] = 0x1B;
            for (int i = 1; i < 48; i++)
                ntpData[i] = 0;

            s.Send(ntpData);
            s.Receive(ntpData);

            byte offsetTransmitTime = 40;
            ulong intpart = 0;
            ulong fractpart = 0;
            for (int i = 0; i <= 3; i++)
                intpart = 256 * intpart + ntpData[offsetTransmitTime + i];

            for (int i = 4; i <= 7; i++)
                fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];

            ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

            s.Close();

            TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
            DateTime dateTime = new DateTime(1900, 1, 1);
            dateTime += timeSpan;

            DateTime networkDateTime = dateTime.AddHours(2);//Change to my time zone
            Debug.Print(networkDateTime.ToString());

            return networkDateTime;
        }


    }
}
