using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using System.Net;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Connections.TCP;
using System.Collections.Generic;
using Elements;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using UnitsNet;
using UnitsNet.Serialization.JsonNet;
using Newtonsoft.Json;
using System.Windows.Forms;

namespace FormWithUnity
{
    class NCServer
    {
        // for testing purpose
        private int position = -1;

        // for the communication 
        private bool serverEnabled = false;
        private int PortServer= 10000;
        private ConnectionInfo serverConnectionInfo = null;
        private Communication message;
        private CommunicateElement messageObject;
        private SendReceiveOptions customSendReceiveOptions;
        private List<IPEndPoint> connectedClients = new List<IPEndPoint>();
        private bool connected = false;
        

        // The delegate procedure we are assigning to our object
        public delegate void EventHandler(object myObject, Event myArgs);
        public event EventHandler OnClickMade;

        public NCServer()
        {
            // warm things up to have a sweet communication =)
            customSendReceiveOptions = new SendReceiveOptions<ProtobufSerializer>();
            serverConnectionInfo = new ConnectionInfo(new IPEndPoint(IPAddress.Any, PortServer));

            //Start listening for incoming TCP connections
            if (!serverEnabled)
                EnableServer_Toggle();
           

            //Configure NetworkComms .Net to handle and incoming packet of type 'ChatMessage'
            //e.g. If we receive a packet of type 'ChatMessage' execute the method 'HandleIncomingChatMessage'
            NetworkComms.AppendGlobalIncomingPacketHandler<Communication>("Communication", HandleIncomingChatMessage, customSendReceiveOptions);

            // if a client disconnects
            NetworkComms.AppendGlobalConnectionCloseHandler(ClientDisconnected);
            
            // if a client connects
            NetworkComms.AppendGlobalConnectionEstablishHandler(ClientConnected);
            
            // if an update is received
            NetworkComms.AppendGlobalIncomingPacketHandler<CommunicateUpdate>("Update", (header, connection, message) =>
            {
                string content = message.Message;
                // deserialize the content or w/e

                OnClickMade(this, new Event(content));
                //RichTextBox1.Text = content; // can't update from this thread
            });

            // on first connection
            NetworkComms.AppendGlobalIncomingPacketHandler<Communication>("InitialClientConnect", (header, connection, message) =>
            {
                if (!connected)
                {
                    lock (connectedClients) { 
                        //If a client sends a InitialClientConnect packet we add them to the connectedClients list
                        connectedClients.Add((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint);
                        position = message.Message;

                        ICollection<IPEndPoint> withoutDuplicates = new HashSet<IPEndPoint>(connectedClients);
                        connectedClients = new List<IPEndPoint>(withoutDuplicates);
                        connected = true;
                    }
                }
            });

            // if we press that key, the server can receive it
            NetworkComms.AppendGlobalIncomingPacketHandler<int>("SecondaryIndexTrigger", (header, connection, message) =>
            {
                Console.WriteLine("SecondaryIndexTrigger");
                SendMyObject(); // here we ask for the creation of an object 
            });




        }

        /// <summary>
        /// client connected
        /// </summary>
        /// <param name="connection"></param>
        public void ClientConnected(Connection connection)
        {
            Console.WriteLine(" ");
            Console.WriteLine("Connection established: " + connection.ConnectionInfo.LocalEndPoint);
            
        }

        /// <summary>
        /// client disconnected
        /// </summary>
        /// <param name="connection"></param>
        public void ClientDisconnected(Connection connection)
        {
            Console.WriteLine(" ");
            Console.WriteLine("Connection is over: " + connection.ConnectionInfo.LocalEndPoint);

        }




        /// <summary>
        /// Performs whatever functions we might so desire when we receive an incoming ChatMessage
        /// </summary>
        /// <param name="header">The PacketHeader corresponding with the received object</param>
        /// <param name="connection">The Connection from which this object was received</param>
        /// <param name="incomingMessage">The incoming ChatMessage we are after</param>
        private void HandleIncomingChatMessage(PacketHeader header, Connection connection, Communication incomingMessage)
        {
            if (incomingMessage.SecretKey == 1234 && NetworkComms.NetworkIdentifier!=connection.ConnectionInfo.NetworkIdentifier)
            {
                position = incomingMessage.Message;
                Console.WriteLine("Message received from: " + connection.ConnectionInfo.LocalEndPoint + ", position updated: " + incomingMessage.Message + " " + incomingMessage.SecretKey);
                // for example purpose 
            }
        }

        /// <summary>
        /// we can send messages
        /// </summary>
        public void SendMessage() {
            
            message = new Communication(NetworkComms.NetworkIdentifier, position,1234);

            //Print out the IPs and ports we are now listening on
            Console.WriteLine();
            Console.WriteLine("[Communication] Server messaging these TCP connections:");

            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                Console.WriteLine("{0}:{1}", localEndPoint.Address, localEndPoint.Port);
                try
                {
                    TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Communication", message);

                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }

            Console.WriteLine("Server stops messaging");
            Console.WriteLine();
            

        }
        
        /// <summary>
        /// you can also send objects, here we create a cylinder, i mean we send a cylinder by tcp and then the client creates it after deserializing and stuff
        /// </summary>
        public void SendMyObject()
        {
            
            //Length c = new Length(1, UnitsNet.Units.LengthUnit.Meter);
            Cylinder c = new Cylinder(1, "Cylinder", "Element", 1, 1, new Length(1, UnitsNet.Units.LengthUnit.Meter), new Length(1, UnitsNet.Units.LengthUnit.Meter), new Length(1, UnitsNet.Units.LengthUnit.Meter));
            //Sphere c = new Sphere(1, "Sphere", "Element", 1, 1, true, new Length(1, UnitsNet.Units.LengthUnit.Meter), new Length(1, UnitsNet.Units.LengthUnit.Meter));

            JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
            _jsonSerializerSettings.Converters.Add(new UnitsNetJsonConverter());
            string json = JsonConvert.SerializeObject(c, _jsonSerializerSettings).Replace("\r\n", "\n");
            
             messageObject = new CommunicateElement(NetworkComms.NetworkIdentifier, json, 1234, c.Designation);
            
            //Print out the IPs and ports we are now listening on
            Console.WriteLine();
            Console.WriteLine("[Object] Server messaging these TCP connections:");

            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                Console.WriteLine("{0}:{1}", localEndPoint.Address, localEndPoint.Port);
                try
                {
                    TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Element", messageObject);
                   
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }

            Console.WriteLine("Server stops messaging");
            Console.WriteLine();
        }

        /// <summary>
        /// get the string between the two given strings
        /// </summary>
        /// <param name="strSource">source you are interested in extract something</param>
        /// <param name="strStart">first string</param>
        /// <param name="strEnd">second string</param>
        /// <returns></returns>
        public static string GetBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        } // credit https://stackoverflow.com/questions/10709821/find-text-in-string-with-c-sharp

        /// <summary>
        /// if we update an item in the interface, it tells the client and it updates
        /// </summary>
        /// <param name="text"></param>
        public void SendObjectUpdate(string text)
        {
            
            if (text.Length > 0)
            {
                // transform the text in a valid format ?
                string content = text;

                // get the ID
                string stringID = GetBetween(content, "<ID", "ID>");
                if (!Int32.TryParse(stringID, out int ID))
                {
                    ID = -1;
                }
                
                // transform text into CommunicateUpdate element
                CommunicateUpdate messageUpdate = new CommunicateUpdate(NetworkComms.NetworkIdentifier, content, 1234, ID);


                //Print out the IPs and ports we are now listening on
                Console.WriteLine();
                Console.WriteLine("[Update] Server messaging these TCP connections:");

                foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
                {
                    Console.WriteLine("{0}:{1}", localEndPoint.Address, localEndPoint.Port);
                    try
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Update", messageUpdate);

                    }
                    catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                    catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                    catch (Exception) { Console.WriteLine("Autre exception"); }
                }

                Console.WriteLine("Server stops messaging");
                Console.WriteLine();
            }
        }


        /// <summary>
        /// Toggle whether the local application is acting as a server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableServer_Toggle()
        {
            //Enable or disable the local server mode depending on the checkbox IsChecked value
            if (!serverEnabled)
                ToggleServerMode(true);
            else
                ToggleServerMode(false);
            serverEnabled = !serverEnabled;
            
        }

        /// Wrap the functionality required to enable/disable the local application server mode
        /// </summary>
        /// <param name="enableServer"></param>
        private void ToggleServerMode(bool enableServer)
        {
            if (enableServer)
            {
                //Start listening for new incoming TCP connections
                //Parameters ensure we listen across all adaptors using a random port
                TCPConnection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, PortServer));
            }
            
            else
            {
                ShutDown();
            }
        }

      
        /// <summary>
        /// closes the communication
        /// </summary>
        public void ShutDown() {
            //We have used NetworkComms so we should ensure that we correctly call shutdown
            NetworkComms.Shutdown();
        }


        /// <summary>
        /// for testing purpose
        /// </summary>
        /// <returns></returns>
        public int GetPosition() { return position; }


        // what goes next is more for example purpose, the client doesn't care about that type of message
        /// <summary>
        /// makes u go right
        /// </summary>
        /// <param name="val"></param>
        public void Right(int val)
        {
            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                try
                {
                    for (int i = 0; i < val; i++)
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Deplacement", "right");
                       
                    }
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }
        }
        
        /// <summary>
        /// makes u go bot
        /// </summary>
        /// <param name="val"></param>
        public void Bot(int val)
        {
            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                try
                {
                    for (int i = 0; i < val; i++)
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Deplacement", "bot");
                    }
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }
        }

        /// <summary>
        /// makes u go left
        /// </summary>
        /// <param name="val"></param>
        public void Left(int val)
        {
            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                try
                {
                    for (int i = 0; i < val; i++)
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Deplacement", "left");
                    }
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }
        }

        /// <summary>
        /// makes u go top
        /// </summary>
        /// <param name="val"></param>
        public void Top(int val)
        {
            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                try
                {
                    for (int i = 0; i < val; i++)
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Deplacement", "top");
                    }
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }
        }
    }


    
}
