using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace Server
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public delegate void delegate1(string str);//定义委托
        public delegate int delegate2(string endPoint);

        private void btnListen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //当点击开始监听的时候，在服务器端创建一个负责监听IP地址跟端口号的Socket
                Socket socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse("192.168.0.102");//IPAddress.Any;
                //创建端口号对象
                IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(txtPort.Text));
                //监听
                socketWatch.Bind(point);
                ShowMsg("监听成功");
                socketWatch.Listen(10);

                Thread th = new Thread(Listen);
                th.IsBackground = true;
                th.Start(socketWatch);
            }
            catch { }
        }

        Socket socketSend;
        /// <summary>
        /// 等待客户端的连接，并且创建与之通信的Socket
        /// </summary>
        /// <param name="o"></param>
        void Listen(object o)
        {
            Socket socketWatch = o as Socket;
            //等待客户端的连接，并且创建一个负责通信的Socket
            while (true)
            {
                try
                {
                    //负责跟客户端通信的Socket
                    socketSend = socketWatch.Accept();
                    //将远程连接的客户端的IP地址和Socket存入集合中
                    dicSocket.Add(socketSend.RemoteEndPoint.ToString(), socketSend);
                    //将远程连接的客户端的IP地址和端口号存储下拉框中
                    //cboUsers.Items.Add(socketSend.RemoteEndPoint.ToString());
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new delegate2(cboUsers.Items.Add), socketSend.RemoteEndPoint.ToString());
                    //192.168.0.100:连接成功
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new delegate1(ShowMsg), socketSend.RemoteEndPoint.ToString() + ":" + "连接成功");
                    //开启一个新的线程，不停地接收客户端发送过来的消息
                    Thread th = new Thread(Receive);
                    th.IsBackground = true;
                    th.Start(socketSend);
                }
                catch { }
            }
        }

        //将远程连接的客户端的IP地址和Socket存入集合中
        Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();

        /// <summary>
        /// 服务端不停地接收客户端发送过来的消息
        /// </summary>
        /// <param name="o"></param>
        void Receive(object o)
        {
            Socket socketSend = o as Socket;
            while (true)
            {
                try
                {
                    //客户端连接成功后，服务器应该接收服务器应该接收客户端发来的消息
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    //实际接收到的有效字节数
                    int r = socketSend.Receive(buffer);
                    if (r == 0)
                    {
                        break;
                    }
                    string str = Encoding.UTF8.GetString(buffer, 0, r);
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new delegate1(ShowMsg), socketSend.RemoteEndPoint.ToString() + ":" + str);
                }
                catch { }
            }
        }

        private void ShowMsg(string str)
        {
            txtContent.AppendText(str + "\r\n");
        }

        /// <summary>
        /// 服务器给客户端发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendMsg_Click(object sender, RoutedEventArgs e)
        {
            string str = txtSendMsg.Text.Trim();
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            List<byte> list = new List<byte>();
            list.Add(0);
            list.AddRange(buffer);
            //将泛型集合转换为数组
            byte[] newBuffer = list.ToArray();
            //获得用户在下拉框中选中的IP地址
            string ip = cboUsers.SelectedItem.ToString();
            dicSocket[ip].Send(newBuffer);
        }

        /// <summary>
        /// 选择要发送的文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = @"C:\Users\Administrator\Desktop";
            ofd.Title = "请选择要发送的文件";
            ofd.Filter = "所有文件|*.*";
            ofd.ShowDialog();

            txtFilePath.Text = ofd.FileName;
        }

        private void btnSendFile_Click(object sender, RoutedEventArgs e)
        {
            //获得要发送文件的路径
            string path = txtFilePath.Text;
            using (FileStream fsRead = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[1024 * 1024 * 5];
                int r = fsRead.Read(buffer, 0, buffer.Length);
                List<byte> list = new List<byte>();
                list.Add(1);
                list.AddRange(buffer);
                byte[] newBuffer = list.ToArray();
                dicSocket[cboUsers.SelectedItem.ToString()].Send(newBuffer, 0, r + 1, SocketFlags.None);
            }
        }

        /// <summary>
        /// 发送震动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnShock_Click(object sender, RoutedEventArgs e)
        {
            byte[] buffer = new byte[1];
            buffer[0] = 2;
            dicSocket[cboUsers.SelectedItem.ToString()].Send(buffer);
        }
    }
}
