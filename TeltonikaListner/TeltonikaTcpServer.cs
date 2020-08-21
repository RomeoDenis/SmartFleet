using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using SmartFleet.Core.Contracts.Commands;
using SmartFleet.Core.Helpers;
using SmartFleet.Core.Protocols;
using SmartFleet.Data;

namespace TeltonikaListner
{
    public class TeltonikaTcpServer
    {
        private readonly IBusControl _bus;
        private readonly IRedisCache _redisCache;

        private TcpListener _listener;

        public TeltonikaTcpServer(
            IBusControl bus,
            IRedisCache redisCache)
        {
            _bus = bus;
            _redisCache = redisCache;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, 34400);
            _listener.Start();
            while (true) // Add your exit flag here
            {
                var client = _listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(ThreadProc, client);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        // ReSharper disable once TooManyDeclarations
        private async void  ThreadProc(object state)
        {
            try
            {
                var client = (TcpClient)state;
                byte[] buffer = new byte[client.ReceiveBufferSize];
                NetworkStream stream = ((TcpClient)state).GetStream();
                int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize) - 2;
                string imei = Encoding.ASCII.GetString(buffer, 2, bytesRead);
                if (Commonhelper.IsValidImei(imei))
                    await ParseAvlDataAsync(client, stream, imei).ConfigureAwait(false);
            }
            catch (InvalidCastException e)
            {
                Trace.TraceWarning(e.Message);
            }
            catch (Exception e)
            {
                Trace.TraceWarning(e.Message);
                //throw;
            }
        }

        // ReSharper disable once MethodTooLong
        private async Task ParseAvlDataAsync(TcpClient client, NetworkStream stream, string imei)
        {
            Trace.TraceInformation("IMEI received : " + imei);
            Trace.TraceInformation("--------------------------------------------");
            Byte[] b = { 0x01 };
            await stream.WriteAsync(b, 0, 1).ConfigureAwait(false);
            var modem = _redisCache.Get<CreateBoxCommand>(imei);
            
            while (true)
            {
                stream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];
                await stream.ReadAsync(buffer, 0, client.ReceiveBufferSize).ConfigureAwait(false);
                List<byte> list = new List<byte>();
                foreach (var b1 in buffer.Skip(9).Take(1)) list.Add(b1);
                int dataCount = Convert.ToInt32(list[0]);
                var bytes = Convert.ToByte(dataCount);
                if (client.Connected)
                    await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, bytes }, 0, 4).ConfigureAwait(false);

                var gpsResult =  ParseAvlData(imei, buffer);
               
                 if (!gpsResult.Any() && imei.Any()) continue;
                var events = new TLGpsDataEvents
                {
                    Id = Guid.NewGuid(),
                    Events = gpsResult
                };
                await _bus.Publish(events).ConfigureAwait(false);
                var lastGpsData = gpsResult.Last();
                if (modem == null)
                {
                    modem = new CreateBoxCommand();
                    modem.Imei = imei;
                }
                modem.Longitude = lastGpsData.Long;
                modem.Latitude = lastGpsData.Lat;
                modem.LastValidGpsDataUtc = lastGpsData.DateTimeUtc;
                modem.Speed = lastGpsData.Speed;
                await _bus.Publish(modem).ConfigureAwait(false);
                // break;
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private List<CreateTeltonikaGps> ParseAvlData(string imei, byte[] buffer)
        {
            List<CreateTeltonikaGps> gpsResult = new List<CreateTeltonikaGps>();
            var parser = new DevicesParser();
            gpsResult.AddRange(parser.Decode(new List<byte>(buffer), imei));
           // await GeoReverseCodeGpsData(gpsResult);
            LogAvlData(gpsResult);
            return gpsResult;
        }

        
        private static void LogAvlData(List<CreateTeltonikaGps> gpsResult)
        {
            foreach (var gpsData in gpsResult.OrderBy(x => x.DateTimeUtc))
            {
                Trace.TraceInformation(
                    $"Date:{gpsData.DateTimeUtc} Latitude: {gpsData.Lat} Longitude{gpsData.Long} Speed :{gpsData.Speed} Direction: {gpsData.Direction}");
                Trace.TraceInformation("--------------------------------------------");
                foreach (var io in gpsData.IoElements_1B)
                    Trace.TraceInformation("Propriété IO (1 byte) : " + (TNIoProperty) io.Key + " Valeur:" + io.Value);
                foreach (var io in gpsData.IoElements_2B)
                    Trace.TraceInformation("Propriété IO (2 byte) : " + (TNIoProperty) io.Key + " Valeur:" + io.Value);
                foreach (var io in gpsData.IoElements_4B)
                    Trace.TraceInformation("Propriété IO (4 byte) : " + (TNIoProperty) io.Key + " Valeur:" + io.Value);
                foreach (var io in gpsData.IoElements_8B)
                    Trace.TraceInformation("Propriété IO (8 byte) : " + (TNIoProperty) io.Key + " Valeur:" + io.Value);
                Trace.TraceInformation("--------------------------------------------");
            }
        }
    }
}
