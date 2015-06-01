﻿// SocketEx.cs implementation by J. Arturo <webmaster at komodosoft dot net>
//  
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SharpCifs.Util.Sharpen
{
    public class SocketEx : Socket
    {
        private int _soTimeOut = -1;       

        public int SoTimeOut
        {
            get
            {
                return _soTimeOut;
            }

            set
            {
                if (value > 0)
                {
                    _soTimeOut = value;
                }
                else
                {
                    _soTimeOut = -1;
                }

            }
        }

        public SocketEx(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
            : base(addressFamily, socketType, protocolType)
        {

        }

        public void Connect(IPEndPoint endPoint, int timeOut)
        {
            AutoResetEvent autoReset = new AutoResetEvent(false);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endPoint
            };

            args.Completed += delegate {
                autoReset.Set();
            };

            ConnectAsync(args);

            if (!autoReset.WaitOne(timeOut))
            {
                CancelConnectAsync(args);
                throw new TimeoutException("Can't connect to end point.");
            }
            if (args.SocketError != SocketError.Success)
            {
                throw new Exception("Can't connect to end point.");
            }
        }

        public void Bind2(EndPoint ep)
        {
            if (ep == null)
                Bind(new IPEndPoint(IPAddress.Any, 0));
            else
                Bind(ep);
        }


        public int Receive(byte[] buffer, int offset, int count)
        {
            AutoResetEvent autoReset = new AutoResetEvent(false);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();

            args.UserToken = this;
            args.SetBuffer(buffer, offset, count);

            args.Completed += delegate 
            {
                autoReset.Set();
            };

            if (ReceiveAsync(args))
            {
                if (!autoReset.WaitOne(_soTimeOut))
                {                                      
                    throw new TimeoutException("No data received.");
                }
            }

            return args.BytesTransferred;
        }

        public void Send(byte[] buffer, int offset, int length, EndPoint destination = null)
        {
            AutoResetEvent autoReset = new AutoResetEvent(false);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();

            args.UserToken = this;
            args.SetBuffer(buffer, offset, length);

            args.Completed += delegate 
            {
                autoReset.Set();
            };

            args.RemoteEndPoint = destination ?? RemoteEndPoint;


            SendToAsync(args);
            if (!autoReset.WaitOne(_soTimeOut))
            {
                throw new TimeoutException("No data sent.");
            }
        }

        public InputStream GetInputStream()
        {
            return new NetworkStream(this);
        }

        public OutputStream GetOutputStream()
        {
            return new NetworkStream(this);
        }

    }
}
