﻿using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Abp.VNext.Hello
{
    public class MessagesConnectionHandler : ConnectionHandler
    {
        private ConnectionList Connections { get; } = new ConnectionList();

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            Microsoft.AspNetCore.Http.Connections.HttpTransportType? transportType = connection.Features.Get<IHttpTransportFeature>()?.TransportType;

            // await Broadcast($"{connection.ConnectionId} connected ({transportType})");

            try
            {
                while (true)
                {
                    var result = await connection.Transport.Input.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (!buffer.IsEmpty)
                        {
                            // We can avoid the copy here but we'll deal with that later
                            var text = Encoding.UTF8.GetString(buffer.ToArray());
                            text = $"{text}";
                            await Broadcast(Encoding.UTF8.GetBytes(text));
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        connection.Transport.Input.AdvanceTo(buffer.End);
                    }
                }
            }
            finally
            {
                Connections.Remove(connection);

                //  await Broadcast($"{connection.ConnectionId} disconnected ({transportType})");
            }
        }

        private Task Broadcast(string text)
        {
            return Broadcast(Encoding.UTF8.GetBytes(text));
        }

        private Task Broadcast(byte[] payload)
        {
            var tasks = new List<Task>(Connections.Count);
            foreach (var c in Connections)
            {
                tasks.Add(c.Transport.Output.WriteAsync(payload).AsTask());
            }
            return Task.WhenAll(tasks);
        }
    }

}
