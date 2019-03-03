using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MagicPacket
{
    public partial class MainWindow : Window
    {
        private List<PacketInfo> packets;

        private void AddPacket(PacketInfo pi)
        {
            if (packets.Contains(pi)) packets.Remove(pi);
            packets.Add(pi);
            SerializePackets();
            ReadSavedPackets();
        }

        private void RemovePacket(PacketInfo pi)
        {
            packets.Remove(pi);
            SerializePackets();
            ReadSavedPackets();
        }

        private void ReadSavedPackets()
        {
            if (File.Exists("magic.pac"))
            {
                packets = DeserializePackets();

                cbxPackets.ItemsSource = packets;
                cbxPackets.DisplayMemberPath = "Target_IP";
                cbxPackets.SelectedValuePath = "Target_IP";
            }
            else packets = new List<PacketInfo>();
        }

        private void SerializePackets()
        {
            try
            {
                using (FileStream fsWrite = new FileStream("magic.pac", FileMode.Create))
                {
                    BinaryFormatter bfWrite = new BinaryFormatter();
                    bfWrite.Serialize(fsWrite, packets);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Err : " + ex.Message);
            }
        }

        private List<PacketInfo> DeserializePackets()
        {
            try
            {
                List<PacketInfo> packets;

                using (FileStream fsRead = new FileStream("magic.pac", FileMode.Open))
                {
                    BinaryFormatter bfRead = new BinaryFormatter();
                    packets = (List<PacketInfo>)bfRead.Deserialize(fsRead);
                }
                return packets;
            }
            catch (Exception ex)
            {
                throw new Exception("Err : " + ex.Message);
            }
        }



        public MainWindow()
        {
            InitializeComponent(); ReadSavedPackets();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FocusManager.SetFocusedElement(this, txtMAC);
        }

        private void BtnSendMagicPacket_Click(object sender, RoutedEventArgs e)
        {
            PacketInfo pi = new PacketInfo() {
                Target_MAC = txtMAC.Text,               // separated by ':', '-' or ' '
                Target_IP = txtIP.Text,                 // ip address of the target machine
                SubnetMask = txtSubnetMask.Text,        // default may be 255.255.255.0
                Port = Convert.ToInt32(txtPort.Text)    // default is 7
            };

            SendMagicPacket(pi);

            AddPacket(pi);
        }

        /// <summary>
        /// =======================================================================
        /// Magic Packet => 6 times 0xff (255) followed by 16 times the mac address
        /// =======================================================================
        /// Send this packet to broadcast address of network and the target machine should wake up
        /// Target machine must have hardware support and configured properly in bios and OS 
        /// (ethernet driver should be up to date in some cases) 
        /// </summary>
        /// <param name="pi">Packet to be sent</param>
        public void SendMagicPacket(PacketInfo pi)
        {            
            Byte[] packet = new byte[(6 + 16 * 6)];         // 102

            for (int i = 0; i <= 5; i++)                    // ADD: 6 times 0xFF (255)
                packet[i] = 0xff; 
            
            Byte[] macBytes = new byte[6];                  

            for(int i=0; i<6; i++)            
                macBytes[i] = (byte)Convert.ToInt16(pi.Target_MAC.Split(new char[] { '-',':',' ' })[i], 16);   
            
            for(int i=0; i<16; i++)                         // ADD: 16 times MAC:            
                Buffer.BlockCopy(macBytes, 0, packet, 6+6*i, macBytes.Length);

            IPAddress broadcastAddress = IPAddress.Parse(pi.Target_IP).BroadcastAddress(IPAddress.Parse(pi.SubnetMask));
            new UdpClient().Send(packet, packet.Length, broadcastAddress.ToString(), pi.Port);
        }

        private void BtnWebPage_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://mumcu.net");
        }

        private void CbxPackets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                PacketInfo pi = (cbxPackets.Items[cbxPackets.SelectedIndex] as PacketInfo);

                txtMAC.Text = pi.Target_MAC;
                txtIP.Text = pi.Target_IP;
                txtSubnetMask.Text = pi.SubnetMask;
                txtPort.Text = pi.Port.ToString();

                btnDelete.IsEnabled = true;
            }
            catch 
            {

            }
        }

        private void PacketInfo_Changed(object sender, TextChangedEventArgs e)
        {
            try
            {
                btnDelete.IsEnabled = false;
            }
            catch 
            {
                
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            RemovePacket((cbxPackets.Items[cbxPackets.SelectedIndex] as PacketInfo));
        }
    }

    public static class Ext
    {
        /*
         * This method is taken from:
         * https://blogs.msdn.microsoft.com/knom/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks/
         * 
         * */

        public static IPAddress BroadcastAddress(this IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];

            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }

            return new IPAddress(broadcastAddress);
        }
    }
}